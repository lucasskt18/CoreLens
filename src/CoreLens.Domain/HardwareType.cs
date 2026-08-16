namespace CoreLens.Domain;

public enum HardwareType
{
    Cpu = 1,
    Gpu = 2,
    Ram = 3,
    Disk = 4,
    Network = 5,
    Motherboard = 6,
    Fan = 7,
    Other = 99
}

public static class HardwareTypeExtensions
{
    public static string ToContract(this HardwareType type) => type switch
    {
        HardwareType.Cpu => "cpu",
        HardwareType.Gpu => "gpu",
        HardwareType.Ram => "ram",
        HardwareType.Disk => "disk",
        HardwareType.Network => "network",
        HardwareType.Motherboard => "motherboard",
        HardwareType.Fan => "fan",
        _ => "other"
    };

    public static HardwareType FromContract(string? value) => value?.ToLowerInvariant() switch
    {
        "cpu" => HardwareType.Cpu,
        "gpu" => HardwareType.Gpu,
        "ram" => HardwareType.Ram,
        "disk" => HardwareType.Disk,
        "network" or "net" => HardwareType.Network,
        "motherboard" or "mb" => HardwareType.Motherboard,
        "fan" => HardwareType.Fan,
        _ => HardwareType.Other
    };
}
