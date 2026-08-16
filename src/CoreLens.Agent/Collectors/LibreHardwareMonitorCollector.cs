using CoreLens.Contracts;
using CoreLens.Contracts.Dtos;
using LibreHardwareMonitor.Hardware;

namespace CoreLens.Agent.Collectors;

public sealed class LibreHardwareMonitorCollector : IMetricCollector, IDisposable
{
    private readonly Computer _computer;
    private readonly UpdateVisitor _visitor = new();
    private readonly ILogger<LibreHardwareMonitorCollector> _logger;
    private bool _opened;

    public LibreHardwareMonitorCollector(ILogger<LibreHardwareMonitorCollector> logger)
    {
        _logger = logger;
        _computer = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true,
            IsMemoryEnabled = true,
            IsMotherboardEnabled = true,
            IsControllerEnabled = true,
            IsStorageEnabled = true,
            IsNetworkEnabled = false
        };

        try
        {
            _computer.Open();
            _opened = true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "LibreHardwareMonitor could not open. Temperatures and GPU sensors will be unavailable.");
        }
    }

    public string Name => "sensors";
    public TimeSpan Interval => TimeSpan.FromSeconds(1);

    public void Collect(CollectorSnapshot snapshot)
    {
        if (!_opened)
        {
            return;
        }

        try
        {
            _computer.Accept(_visitor);
            foreach (var hardware in _computer.Hardware)
            {
                VisitHardware(hardware, snapshot);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "LibreHardwareMonitor update failed.");
        }
    }

    private static void VisitHardware(IHardware hardware, CollectorSnapshot snapshot)
    {
        hardware.Update();
        var (stableKey, type) = MapHardware(hardware);
        snapshot.Components.Add(new ComponentInventoryDto
        {
            StableKey = stableKey,
            Type = type,
            Manufacturer = hardware.Name,
            Model = hardware.Name
        });

        foreach (var sensor in hardware.Sensors)
        {
            if (sensor.Value is not float value)
            {
                continue;
            }

            var metric = MapSensor(sensor.SensorType);
            if (metric is null)
            {
                continue;
            }

            var componentKey = sensor.SensorType == SensorType.Fan
                ? $"fan:{KeyHelper.Sanitize(sensor.Name)}"
                : stableKey;

            if (componentKey.StartsWith("fan:", StringComparison.Ordinal))
            {
                snapshot.Components.Add(new ComponentInventoryDto
                {
                    StableKey = componentKey,
                    Type = HardwareTypes.Fan,
                    Model = sensor.Name
                });
            }

            snapshot.Samples.Add(new MetricSampleDto
            {
                ComponentStableKey = componentKey,
                Name = metric,
                Value = value
            });
        }

        foreach (var sub in hardware.SubHardware)
        {
            VisitHardware(sub, snapshot);
        }
    }

    private static (string Key, string Type) MapHardware(IHardware hardware)
    {
        var slug = KeyHelper.Sanitize(hardware.Identifier.ToString());
        return hardware.HardwareType switch
        {
            HardwareType.Cpu => ("cpu:0", HardwareTypes.Cpu),
            HardwareType.GpuNvidia or HardwareType.GpuAmd or HardwareType.GpuIntel => ($"gpu:{slug}", HardwareTypes.Gpu),
            HardwareType.Memory => ("ram:0", HardwareTypes.Ram),
            HardwareType.Storage => ($"disk:{slug}", HardwareTypes.Disk),
            HardwareType.Motherboard => ("mb:0", HardwareTypes.Motherboard),
            _ => ($"other:{slug}", HardwareTypes.Other)
        };
    }

    private static string? MapSensor(SensorType type) => type switch
    {
        SensorType.Load => MetricKeys.LoadPct,
        SensorType.Temperature => MetricKeys.TempC,
        SensorType.Clock => MetricKeys.ClockMhz,
        SensorType.Fan => MetricKeys.FanRpm,
        SensorType.Power => MetricKeys.PowerW,
        SensorType.Data => null,
        _ => null
    };

    public void Dispose()
    {
        if (_opened)
        {
            _computer.Close();
            _opened = false;
        }
    }

    private sealed class UpdateVisitor : IVisitor
    {
        public void VisitComputer(IComputer computer) => computer.Traverse(this);

        public void VisitHardware(IHardware hardware)
        {
            hardware.Update();
            foreach (var sub in hardware.SubHardware)
            {
                sub.Accept(this);
            }
        }

        public void VisitSensor(ISensor sensor)
        {
        }

        public void VisitParameter(IParameter parameter)
        {
        }
    }
}
