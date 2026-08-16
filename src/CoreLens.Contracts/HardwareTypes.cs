namespace CoreLens.Contracts;

public static class HardwareTypes
{
    public const string Cpu = "cpu";
    public const string Gpu = "gpu";
    public const string Ram = "ram";
    public const string Disk = "disk";
    public const string Network = "network";
    public const string Motherboard = "motherboard";
    public const string Fan = "fan";
    public const string Other = "other";

    public static string FromStableKey(string stableKey)
    {
        var prefix = stableKey.Split(':', 2)[0];
        return prefix switch
        {
            "cpu" => Cpu,
            "gpu" => Gpu,
            "ram" => Ram,
            "disk" => Disk,
            "net" => Network,
            "mb" => Motherboard,
            "fan" => Fan,
            _ => Other
        };
    }
}
