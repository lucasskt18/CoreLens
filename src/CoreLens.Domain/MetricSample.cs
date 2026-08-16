namespace CoreLens.Domain;

public sealed class MetricSample
{
    public DateTimeOffset Time { get; set; }
    public Guid ComputerId { get; set; }
    public Guid ComponentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public double Value { get; set; }
}
