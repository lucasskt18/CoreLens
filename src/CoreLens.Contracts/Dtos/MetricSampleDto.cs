namespace CoreLens.Contracts.Dtos;

public sealed class MetricSampleDto
{
    public string ComponentStableKey { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public double Value { get; set; }
}
