using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CoreLens.Infrastructure.Persistence;

public sealed class TimescaleSetup
{
    private readonly CoreLensDbContext _db;
    private readonly ILogger<TimescaleSetup> _logger;

    public TimescaleSetup(CoreLensDbContext db, ILogger<TimescaleSetup> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task ApplyAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _db.Database.ExecuteSqlRawAsync("CREATE EXTENSION IF NOT EXISTS timescaledb;", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "TimescaleDB extension is not available. metric_samples stays as a regular table.");
            return;
        }

        await _db.Database.ExecuteSqlRawAsync(
            "SELECT create_hypertable('metric_samples', 'time', if_not_exists => TRUE, migrate_data => TRUE);",
            cancellationToken);

        await TryAsync(
            "SELECT add_retention_policy('metric_samples', INTERVAL '24 hours', if_not_exists => TRUE);",
            cancellationToken);

        await _db.Database.ExecuteSqlRawAsync("""
            CREATE MATERIALIZED VIEW IF NOT EXISTS metric_samples_1m
            WITH (timescaledb.continuous) AS
            SELECT time_bucket('1 minute', "time") AS bucket,
                   computer_id,
                   component_id,
                   name,
                   avg(value) AS avg_value,
                   min(value) AS min_value,
                   max(value) AS max_value
            FROM metric_samples
            GROUP BY bucket, computer_id, component_id, name
            WITH NO DATA;
            """, cancellationToken);

        await TryAsync("""
            SELECT add_continuous_aggregate_policy(
                'metric_samples_1m',
                start_offset => INTERVAL '2 hours',
                end_offset => INTERVAL '1 minute',
                schedule_interval => INTERVAL '1 minute',
                if_not_exists => TRUE);
            """, cancellationToken);

        await TryAsync(
            "SELECT add_retention_policy('metric_samples_1m', INTERVAL '30 days', if_not_exists => TRUE);",
            cancellationToken);

        await _db.Database.ExecuteSqlRawAsync("""
            CREATE MATERIALIZED VIEW IF NOT EXISTS metric_samples_1h
            WITH (timescaledb.continuous) AS
            SELECT time_bucket('1 hour', bucket) AS bucket,
                   computer_id,
                   component_id,
                   name,
                   avg(avg_value) AS avg_value,
                   min(min_value) AS min_value,
                   max(max_value) AS max_value
            FROM metric_samples_1m
            GROUP BY 1, 2, 3, 4
            WITH NO DATA;
            """, cancellationToken);

        await TryAsync("""
            SELECT add_continuous_aggregate_policy(
                'metric_samples_1h',
                start_offset => INTERVAL '3 days',
                end_offset => INTERVAL '1 hour',
                schedule_interval => INTERVAL '15 minutes',
                if_not_exists => TRUE);
            """, cancellationToken);

        await TryAsync(
            "SELECT add_retention_policy('metric_samples_1h', INTERVAL '90 days', if_not_exists => TRUE);",
            cancellationToken);

        _logger.LogInformation("TimescaleDB hypertables, continuous aggregates and retention policies are in place.");
    }

    private async Task TryAsync(string sql, CancellationToken cancellationToken)
    {
        try
        {
            await _db.Database.ExecuteSqlRawAsync(sql, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Timescale policy SQL skipped.");
        }
    }
}
