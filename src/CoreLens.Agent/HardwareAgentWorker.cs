using System.Reflection;
using CoreLens.Agent.Collectors;
using CoreLens.Agent.Identity;
using CoreLens.Agent.Transport;
using CoreLens.Contracts.Dtos;

namespace CoreLens.Agent;

public sealed class HardwareAgentWorker : BackgroundService
{
    private readonly ILogger<HardwareAgentWorker> _logger;
    private readonly ApiIngestClient _client;
    private readonly IReadOnlyList<IMetricCollector> _collectors;
    private readonly InventoryCollector _inventory;
    private readonly Guid _computerId;
    private readonly TimeSpan _sampleInterval;
    private readonly TimeSpan _inventoryInterval;
    private readonly string _agentVersion;

    public HardwareAgentWorker(
        ILogger<HardwareAgentWorker> logger,
        ApiIngestClient client,
        IEnumerable<IMetricCollector> collectors,
        InventoryCollector inventory,
        IConfiguration configuration)
    {
        _logger = logger;
        _client = client;
        _collectors = collectors.ToList();
        _inventory = inventory;
        _computerId = MachineIdentity.GetOrCreate();
        _sampleInterval = TimeSpan.FromMilliseconds(configuration.GetValue("Agent:SampleIntervalMs", 1000));
        _inventoryInterval = TimeSpan.FromSeconds(configuration.GetValue("Agent:InventoryIntervalSeconds", 300));
        _agentVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.1.0";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("CoreLens agent started for {ComputerId} ({Host})", _computerId, _inventory.Hostname);

        var lastRun = _collectors.ToDictionary(c => c.Name, _ => DateTimeOffset.MinValue);
        var lastInventory = DateTimeOffset.MinValue;

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTimeOffset.UtcNow;
            var snapshot = new CollectorSnapshot();
            var includeInventory = now - lastInventory >= _inventoryInterval;

            foreach (var collector in _collectors)
            {
                if (now - lastRun[collector.Name] < collector.Interval)
                {
                    continue;
                }

                try
                {
                    collector.Collect(snapshot);
                    lastRun[collector.Name] = now;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Collector {Name} failed", collector.Name);
                }
            }

            if (includeInventory)
            {
                snapshot.Components.Add(_inventory.Motherboard);
                lastInventory = now;
            }

            var components = includeInventory
                ? snapshot.Components
                    .GroupBy(c => c.StableKey, StringComparer.OrdinalIgnoreCase)
                    .Select(g => g.Last())
                    .ToList()
                : null;

            var samples = snapshot.Samples
                .GroupBy(s => (s.ComponentStableKey, s.Name))
                .Select(g => g.Last())
                .ToList();

            if (samples.Count == 0 && components is null)
            {
                await Task.Delay(_sampleInterval, stoppingToken);
                continue;
            }

            var request = new IngestRequest
            {
                ComputerId = _computerId,
                Hostname = _inventory.Hostname,
                OsVersion = _inventory.OsVersion,
                AgentVersion = _agentVersion,
                Timestamp = now,
                Components = components,
                Samples = samples
            };

            try
            {
                await _client.SendAsync(request, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to ingest metrics; will retry on the next tick.");
            }

            await Task.Delay(_sampleInterval, stoppingToken);
        }
    }
}
