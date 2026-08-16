namespace CoreLens.Domain;

public sealed class Computer
{
    public Guid Id { get; set; }
    public string Hostname { get; set; } = string.Empty;
    public string OsVersion { get; set; } = string.Empty;
    public string AgentVersion { get; set; } = string.Empty;
    public DateTimeOffset LastSeenAt { get; set; }
    public ICollection<Component> Components { get; set; } = new List<Component>();
}
