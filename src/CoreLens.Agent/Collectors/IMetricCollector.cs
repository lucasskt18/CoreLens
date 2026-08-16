using CoreLens.Contracts.Dtos;

namespace CoreLens.Agent.Collectors;

public sealed class CollectorSnapshot
{
    public List<ComponentInventoryDto> Components { get; } = [];
    public List<MetricSampleDto> Samples { get; } = [];
}

public interface IMetricCollector
{
    string Name { get; }
    TimeSpan Interval { get; }
    void Collect(CollectorSnapshot snapshot);
}
