namespace CoreLens.Contracts.Dtos;

public sealed class MetricsBroadcastDto
{
    public Guid ComputerId { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public IReadOnlyList<MetricSampleDto> Samples { get; set; } = [];
}
