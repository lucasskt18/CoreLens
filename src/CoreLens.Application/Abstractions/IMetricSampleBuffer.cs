using CoreLens.Domain;

namespace CoreLens.Application.Abstractions;

public interface IMetricSampleBuffer
{
    ValueTask EnqueueAsync(IReadOnlyList<MetricSample> samples, CancellationToken cancellationToken);
}
