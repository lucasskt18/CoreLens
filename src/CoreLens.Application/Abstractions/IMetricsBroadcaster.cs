using CoreLens.Contracts.Dtos;

namespace CoreLens.Application.Abstractions;

public interface IMetricsBroadcaster
{
    Task BroadcastMetricsAsync(MetricsBroadcastDto batch, CancellationToken cancellationToken);
    Task BroadcastAlertAsync(AlertEventDto alert, CancellationToken cancellationToken);
}
