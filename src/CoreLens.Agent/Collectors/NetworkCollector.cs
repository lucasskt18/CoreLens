using System.Net.NetworkInformation;
using CoreLens.Contracts;
using CoreLens.Contracts.Dtos;

namespace CoreLens.Agent.Collectors;

public sealed class NetworkCollector : IMetricCollector
{
    private readonly Dictionary<string, (long Recv, long Sent, DateTimeOffset At)> _previous = new();

    public string Name => "network";
    public TimeSpan Interval => TimeSpan.FromSeconds(1);

    public void Collect(CollectorSnapshot snapshot)
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (nic.NetworkInterfaceType is NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel)
            {
                continue;
            }

            if (nic.OperationalStatus != OperationalStatus.Up)
            {
                continue;
            }

            var key = $"net:{KeyHelper.Sanitize(nic.Name)}";
            snapshot.Components.Add(new ComponentInventoryDto
            {
                StableKey = key,
                Type = HardwareTypes.Network,
                Model = nic.Description,
                Specs = new Dictionary<string, string>
                {
                    ["speedBps"] = nic.Speed.ToString(),
                    ["type"] = nic.NetworkInterfaceType.ToString()
                }
            });

            var stats = nic.GetIPStatistics();
            if (_previous.TryGetValue(key, out var prev))
            {
                var seconds = Math.Max((now - prev.At).TotalSeconds, 0.001);
                snapshot.Samples.Add(new MetricSampleDto
                {
                    ComponentStableKey = key,
                    Name = MetricKeys.BytesRecvPerS,
                    Value = Math.Max(0, (stats.BytesReceived - prev.Recv) / seconds)
                });
                snapshot.Samples.Add(new MetricSampleDto
                {
                    ComponentStableKey = key,
                    Name = MetricKeys.BytesSentPerS,
                    Value = Math.Max(0, (stats.BytesSent - prev.Sent) / seconds)
                });
            }

            _previous[key] = (stats.BytesReceived, stats.BytesSent, now);
        }
    }
}
