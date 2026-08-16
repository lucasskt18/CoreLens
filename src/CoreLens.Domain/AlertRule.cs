namespace CoreLens.Domain;

public sealed class AlertRule
{
    public Guid Id { get; set; }
    public Guid? ComputerId { get; set; }
    public Guid? ComponentId { get; set; }
    public HardwareType? ComponentType { get; set; }
    public string MetricName { get; set; } = string.Empty;
    public AlertOperator Operator { get; set; }
    public double Threshold { get; set; }
    public int DurationSeconds { get; set; }
    public AlertSeverity Severity { get; set; }
    public bool IsEnabled { get; set; } = true;
}
