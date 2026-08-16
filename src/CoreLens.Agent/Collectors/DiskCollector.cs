using CoreLens.Contracts;
using CoreLens.Contracts.Dtos;

namespace CoreLens.Agent.Collectors;

public sealed class DiskCollector : IMetricCollector
{
    public string Name => "disk";
    public TimeSpan Interval => TimeSpan.FromSeconds(5);

    public void Collect(CollectorSnapshot snapshot)
    {
        foreach (var drive in DriveInfo.GetDrives())
        {
            if (!drive.IsReady || drive.DriveType is DriveType.CDRom or DriveType.Network or DriveType.Removable)
            {
                continue;
            }

            var key = $"disk:{KeyHelper.Sanitize(drive.Name.TrimEnd('\\', '/'))}";
            snapshot.Components.Add(new ComponentInventoryDto
            {
                StableKey = key,
                Type = HardwareTypes.Disk,
                Model = string.IsNullOrWhiteSpace(drive.VolumeLabel) ? drive.Name : drive.VolumeLabel,
                Specs = new Dictionary<string, string>
                {
                    ["format"] = drive.DriveFormat,
                    ["totalBytes"] = drive.TotalSize.ToString()
                }
            });

            var used = drive.TotalSize - drive.AvailableFreeSpace;
            var usedPct = drive.TotalSize <= 0 ? 0 : used * 100.0 / drive.TotalSize;
            snapshot.Samples.Add(new MetricSampleDto { ComponentStableKey = key, Name = MetricKeys.TotalBytes, Value = drive.TotalSize });
            snapshot.Samples.Add(new MetricSampleDto { ComponentStableKey = key, Name = MetricKeys.AvailableBytes, Value = drive.AvailableFreeSpace });
            snapshot.Samples.Add(new MetricSampleDto { ComponentStableKey = key, Name = MetricKeys.UsedBytes, Value = used });
            snapshot.Samples.Add(new MetricSampleDto { ComponentStableKey = key, Name = MetricKeys.UsedPct, Value = usedPct });
        }
    }
}
