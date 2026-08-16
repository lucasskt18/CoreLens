using System.Management;
using System.Runtime.InteropServices;
using CoreLens.Contracts;
using CoreLens.Contracts.Dtos;

namespace CoreLens.Agent.Collectors;

public sealed class InventoryCollector
{
    public ComponentInventoryDto Motherboard { get; }

    public InventoryCollector()
    {
        Motherboard = ReadMotherboard();
    }

    public string Hostname => Environment.MachineName;

    public string OsVersion => $"{RuntimeInformation.OSDescription} ({RuntimeInformation.OSArchitecture})";

    private static ComponentInventoryDto ReadMotherboard()
    {
        var dto = new ComponentInventoryDto
        {
            StableKey = "mb:0",
            Type = HardwareTypes.Motherboard,
            Model = "Unknown"
        };

        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Manufacturer, Model FROM Win32_ComputerSystem");
            foreach (var item in searcher.Get())
            {
                dto.Manufacturer = item["Manufacturer"]?.ToString();
                dto.Model = item["Model"]?.ToString();
                break;
            }
        }
        catch
        {
            // Inventory is best-effort; the hot path does not depend on WMI.
        }

        return dto;
    }
}
