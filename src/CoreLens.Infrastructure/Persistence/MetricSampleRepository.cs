using CoreLens.Application.Abstractions;
using CoreLens.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CoreLens.Infrastructure.Persistence;

public sealed class MetricSampleRepository : IMetricSampleRepository
{
    private readonly CoreLensDbContext _db;

    public MetricSampleRepository(CoreLensDbContext db)
    {
        _db = db;
    }

    public async Task InsertBatchAsync(IReadOnlyList<MetricSample> samples, CancellationToken cancellationToken)
    {
        if (samples.Count == 0)
        {
            return;
        }

        var connection = (NpgsqlConnection)_db.Database.GetDbConnection();
        var shouldClose = connection.State != System.Data.ConnectionState.Open;
        if (shouldClose)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using var writer = await connection.BeginBinaryImportAsync(
                "COPY metric_samples (time, computer_id, component_id, name, value) FROM STDIN (FORMAT BINARY)",
                cancellationToken);

            foreach (var sample in samples)
            {
                await writer.StartRowAsync(cancellationToken);
                await writer.WriteAsync(sample.Time, NpgsqlTypes.NpgsqlDbType.TimestampTz, cancellationToken);
                await writer.WriteAsync(sample.ComputerId, NpgsqlTypes.NpgsqlDbType.Uuid, cancellationToken);
                await writer.WriteAsync(sample.ComponentId, NpgsqlTypes.NpgsqlDbType.Uuid, cancellationToken);
                await writer.WriteAsync(sample.Name, NpgsqlTypes.NpgsqlDbType.Text, cancellationToken);
                await writer.WriteAsync(sample.Value, NpgsqlTypes.NpgsqlDbType.Double, cancellationToken);
            }

            await writer.CompleteAsync(cancellationToken);
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }

    public async Task<IReadOnlyList<(DateTimeOffset Time, Guid ComponentId, string Name, double Value)>> QueryAsync(
        Guid computerId,
        DateTimeOffset from,
        DateTimeOffset to,
        string bucket,
        string? metricName,
        string? componentStableKey,
        CancellationToken cancellationToken)
    {
        Guid? componentId = null;
        if (!string.IsNullOrWhiteSpace(componentStableKey))
        {
            componentId = await _db.Components.AsNoTracking()
                .Where(c => c.ComputerId == computerId && c.StableKey == componentStableKey)
                .Select(c => (Guid?)c.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var sql = bucket switch
        {
            "1m" => """
                SELECT bucket AS time, component_id, name, avg_value AS value
                FROM metric_samples_1m
                WHERE computer_id = {0} AND bucket >= {1} AND bucket <= {2}
                """,
            "1h" => """
                SELECT bucket AS time, component_id, name, avg_value AS value
                FROM metric_samples_1h
                WHERE computer_id = {0} AND bucket >= {1} AND bucket <= {2}
                """,
            _ => """
                SELECT time, component_id, name, value
                FROM metric_samples
                WHERE computer_id = {0} AND time >= {1} AND time <= {2}
                """
        };

        if (!string.IsNullOrWhiteSpace(metricName))
        {
            sql += " AND name = {3}";
        }

        if (componentId is not null)
        {
            sql += string.IsNullOrWhiteSpace(metricName)
                ? " AND component_id = {3}"
                : " AND component_id = {4}";
        }

        sql += " ORDER BY time";

        try
        {
            var rows = await QueryRowsAsync(sql, computerId, from, to, metricName, componentId, cancellationToken);
            return rows;
        }
        catch (PostgresException) when (bucket is "1m" or "1h")
        {
            return await QueryAsync(computerId, from, to, "1s", metricName, componentStableKey, cancellationToken);
        }
    }

    private async Task<IReadOnlyList<(DateTimeOffset Time, Guid ComponentId, string Name, double Value)>> QueryRowsAsync(
        string sql,
        Guid computerId,
        DateTimeOffset from,
        DateTimeOffset to,
        string? metricName,
        Guid? componentId,
        CancellationToken cancellationToken)
    {
        var result = new List<(DateTimeOffset, Guid, string, double)>();
        var connection = (NpgsqlConnection)_db.Database.GetDbConnection();
        var shouldClose = connection.State != System.Data.ConnectionState.Open;
        if (shouldClose)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using var cmd = new NpgsqlCommand(ToNpgsql(sql), connection);
            cmd.Parameters.AddWithValue(computerId);
            cmd.Parameters.AddWithValue(from);
            cmd.Parameters.AddWithValue(to);
            if (!string.IsNullOrWhiteSpace(metricName))
            {
                cmd.Parameters.AddWithValue(metricName);
            }

            if (componentId is Guid id)
            {
                cmd.Parameters.AddWithValue(id);
            }

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                result.Add((
                    reader.GetFieldValue<DateTimeOffset>(0),
                    reader.GetGuid(1),
                    reader.GetString(2),
                    reader.GetDouble(3)));
            }
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }

        return result;
    }

    private static string ToNpgsql(string efStyle)
    {
        return efStyle
            .Replace("{0}", "$1")
            .Replace("{1}", "$2")
            .Replace("{2}", "$3")
            .Replace("{3}", "$4")
            .Replace("{4}", "$5");
    }
}
