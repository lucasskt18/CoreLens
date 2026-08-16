using CoreLens.Application.Abstractions;
using CoreLens.Contracts.Dtos;

namespace CoreLens.Application.History;

public sealed class GetHistoryHandler
{
    private readonly IMetricSampleRepository _samples;
    private readonly IComponentRepository _components;

    public GetHistoryHandler(IMetricSampleRepository samples, IComponentRepository components)
    {
        _samples = samples;
        _components = components;
    }

    public async Task<HistoryResponseDto> GetAsync(
        Guid computerId,
        DateTimeOffset from,
        DateTimeOffset to,
        string? bucket,
        string? metricName,
        string? componentStableKey,
        CancellationToken cancellationToken)
    {
        var resolvedBucket = ResolveBucket(bucket, to - from);
        var rows = await _samples.QueryAsync(
            computerId, from, to, resolvedBucket, metricName, componentStableKey, cancellationToken);

        var map = await _components.GetMapAsync(computerId, cancellationToken);
        var byId = map.Values.ToDictionary(c => c.Id, c => c.StableKey);

        return new HistoryResponseDto
        {
            ComputerId = computerId,
            Bucket = resolvedBucket,
            Points = rows.Select(r => new HistoryPointDto
            {
                Time = r.Time,
                ComponentStableKey = byId.GetValueOrDefault(r.ComponentId, r.ComponentId.ToString()),
                Name = r.Name,
                Value = r.Value
            }).ToList()
        };
    }

    private static string ResolveBucket(string? requested, TimeSpan range)
    {
        if (!string.IsNullOrWhiteSpace(requested))
        {
            return requested;
        }

        if (range <= TimeSpan.FromHours(2))
        {
            return "1s";
        }

        if (range <= TimeSpan.FromDays(7))
        {
            return "1m";
        }

        return "1h";
    }
}
