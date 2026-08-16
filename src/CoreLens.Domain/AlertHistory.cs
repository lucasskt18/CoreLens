namespace CoreLens.Domain;

public sealed class AlertHistory
{
    public Guid Id { get; set; }
    public Guid AlertRuleId { get; set; }
    public Guid ComputerId { get; set; }
    public Guid ComponentId { get; set; }
    public DateTimeOffset Time { get; set; }
    public string Message { get; set; } = string.Empty;
    public AlertSeverity Severity { get; set; }
    public double Value { get; set; }
}
