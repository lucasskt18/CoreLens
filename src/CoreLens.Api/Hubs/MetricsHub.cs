using CoreLens.Application.Abstractions;
using CoreLens.Contracts;
using CoreLens.Contracts.Dtos;
using Microsoft.AspNetCore.SignalR;

namespace CoreLens.Api.Hubs;

public sealed class MetricsHub : Hub
{
    public async Task JoinComputer(string computerId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, computerId);
    }

    public async Task LeaveComputer(string computerId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, computerId);
    }
}

public sealed class SignalRMetricsBroadcaster : IMetricsBroadcaster
{
    private readonly IHubContext<MetricsHub> _hub;

    public SignalRMetricsBroadcaster(IHubContext<MetricsHub> hub)
    {
        _hub = hub;
    }

    public Task BroadcastMetricsAsync(MetricsBroadcastDto batch, CancellationToken cancellationToken) =>
        _hub.Clients.Group(batch.ComputerId.ToString())
            .SendAsync(SignalRContract.MetricsEvent, batch, cancellationToken);

    public Task BroadcastAlertAsync(AlertEventDto alert, CancellationToken cancellationToken) =>
        _hub.Clients.Group(alert.ComputerId.ToString())
            .SendAsync(SignalRContract.AlertEvent, alert, cancellationToken);
}
