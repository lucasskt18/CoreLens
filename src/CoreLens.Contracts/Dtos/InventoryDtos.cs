namespace CoreLens.Contracts.Dtos;

public sealed class ComputerSummaryDto
{
    public Guid Id { get; set; }
    public string Hostname { get; set; } = string.Empty;
    public string OsVersion { get; set; } = string.Empty;
    public string AgentVersion { get; set; } = string.Empty;
    public DateTimeOffset LastSeenAt { get; set; }
}

public sealed class InventoryDto
{
    public ComputerSummaryDto Computer { get; set; } = new();
    public IReadOnlyList<ComponentDto> Components { get; set; } = [];
}

public sealed class ComponentDto
{
    public Guid Id { get; set; }
    public string StableKey { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Manufacturer { get; set; }
    public string? Model { get; set; }
    public Dictionary<string, string>? Specs { get; set; }
}
