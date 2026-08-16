namespace CoreLens.Contracts.Dtos;

public sealed class ComponentInventoryDto
{
    public string StableKey { get; set; } = string.Empty;
    public string Type { get; set; } = HardwareTypes.Other;
    public string? Manufacturer { get; set; }
    public string? Model { get; set; }
    public Dictionary<string, string>? Specs { get; set; }
}
