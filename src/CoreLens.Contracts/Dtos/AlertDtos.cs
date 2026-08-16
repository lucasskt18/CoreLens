namespace CoreLens.Contracts.Dtos;

public sealed class AlertEventDto
{
    public Guid Id { get; set; }
    public Guid ComputerId { get; set; }
    public Guid ComponentId { get; set; }
    public string ComponentStableKey { get; set; } = string.Empty;
    public DateTimeOffset Time { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Severity { get; set; } = "warning";
    public double Value { get; set; }
    public string MetricName { get; set; } = string.Empty;
}

public sealed class AlertRuleDto
{
    public Guid Id { get; set; }
    public string? ComponentType { get; set; }
    public string MetricName { get; set; } = string.Empty;
    public string Operator { get; set; } = string.Empty;
    public double Threshold { get; set; }
    public int DurationSeconds { get; set; }
    public string Severity { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
}
