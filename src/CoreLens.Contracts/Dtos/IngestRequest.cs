namespace CoreLens.Contracts.Dtos;

public sealed class IngestRequest
{
    public Guid ComputerId { get; set; }
    public string Hostname { get; set; } = string.Empty;
    public string OsVersion { get; set; } = string.Empty;
    public string AgentVersion { get; set; } = "0.1.0";
    public DateTimeOffset Timestamp { get; set; }
    public IReadOnlyList<ComponentInventoryDto>? Components { get; set; }
    public IReadOnlyList<MetricSampleDto> Samples { get; set; } = [];
}
