namespace CoreLens.Domain;

public sealed class Component
{
    public Guid Id { get; set; }
    public Guid ComputerId { get; set; }
    public Computer? Computer { get; set; }
    public string StableKey { get; set; } = string.Empty;
    public HardwareType Type { get; set; }
    public string? Manufacturer { get; set; }
    public string? Model { get; set; }
    public string? SpecsJson { get; set; }
}
