using System.Runtime.InteropServices;
using CoreLens.Contracts;
using CoreLens.Contracts.Dtos;

namespace CoreLens.Agent.Collectors;

public sealed class CpuRamCollector : IMetricCollector
{
    private long _prevIdle;
    private long _prevKernel;
    private long _prevUser;
    private bool _primed;

    public string Name => "cpu-ram";
    public TimeSpan Interval => TimeSpan.FromSeconds(1);

    public void Collect(CollectorSnapshot snapshot)
    {
        snapshot.Components.Add(new ComponentInventoryDto
        {
            StableKey = "cpu:0",
            Type = HardwareTypes.Cpu,
            Model = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER"),
            Specs = new Dictionary<string, string>
            {
                ["logicalProcessors"] = Environment.ProcessorCount.ToString()
            }
        });

        if (TryGetCpuLoad(out var cpu))
        {
            snapshot.Samples.Add(new MetricSampleDto
            {
                ComponentStableKey = "cpu:0",
                Name = MetricKeys.LoadPct,
                Value = cpu
            });
        }

        var status = new MemoryStatusEx { Length = (uint)Marshal.SizeOf<MemoryStatusEx>() };
        if (GlobalMemoryStatusEx(ref status))
        {
            snapshot.Components.Add(new ComponentInventoryDto
            {
                StableKey = "ram:0",
                Type = HardwareTypes.Ram,
                Model = "System memory",
                Specs = new Dictionary<string, string>
                {
                    ["totalBytes"] = status.TotalPhys.ToString()
                }
            });

            var used = (double)(status.TotalPhys - status.AvailPhys);
            snapshot.Samples.Add(new MetricSampleDto { ComponentStableKey = "ram:0", Name = MetricKeys.TotalBytes, Value = status.TotalPhys });
            snapshot.Samples.Add(new MetricSampleDto { ComponentStableKey = "ram:0", Name = MetricKeys.AvailableBytes, Value = status.AvailPhys });
            snapshot.Samples.Add(new MetricSampleDto { ComponentStableKey = "ram:0", Name = MetricKeys.UsedBytes, Value = used });
            snapshot.Samples.Add(new MetricSampleDto { ComponentStableKey = "ram:0", Name = MetricKeys.UsedPct, Value = status.MemoryLoad });
        }
    }

    private bool TryGetCpuLoad(out double load)
    {
        load = 0;
        if (!GetSystemTimes(out var idle, out var kernel, out var user))
        {
            return false;
        }

        var idleTime = ToLong(idle);
        var kernelTime = ToLong(kernel);
        var userTime = ToLong(user);

        if (!_primed)
        {
            _prevIdle = idleTime;
            _prevKernel = kernelTime;
            _prevUser = userTime;
            _primed = true;
            return false;
        }

        var idleDelta = idleTime - _prevIdle;
        var totalDelta = (kernelTime - _prevKernel) + (userTime - _prevUser);
        _prevIdle = idleTime;
        _prevKernel = kernelTime;
        _prevUser = userTime;

        if (totalDelta <= 0)
        {
            return false;
        }

        load = Math.Clamp((1.0 - (double)idleDelta / totalDelta) * 100.0, 0, 100);
        return true;
    }

    private static long ToLong(FileTime time) => ((long)time.High << 32) | time.Low;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetSystemTimes(out FileTime idle, out FileTime kernel, out FileTime user);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx buffer);

    [StructLayout(LayoutKind.Sequential)]
    private struct FileTime
    {
        public uint Low;
        public uint High;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MemoryStatusEx
    {
        public uint Length;
        public uint MemoryLoad;
        public ulong TotalPhys;
        public ulong AvailPhys;
        public ulong TotalPageFile;
        public ulong AvailPageFile;
        public ulong TotalVirtual;
        public ulong AvailVirtual;
        public ulong AvailExtendedVirtual;
    }
}
