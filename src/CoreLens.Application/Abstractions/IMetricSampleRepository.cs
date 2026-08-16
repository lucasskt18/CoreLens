using CoreLens.Domain;

namespace CoreLens.Application.Abstractions;

public interface IMetricSampleRepository
{
    Task InsertBatchAsync(IReadOnlyList<MetricSample> samples, CancellationToken cancellationToken);

    Task<IReadOnlyList<(DateTimeOffset Time, Guid ComponentId, string Name, double Value)>> QueryAsync(
        Guid computerId,
        DateTimeOffset from,
        DateTimeOffset to,
        string bucket,
        string? metricName,
        string? componentStableKey,
        CancellationToken cancellationToken);
}
