using CoreLens.Application.Alerts;
using CoreLens.Application.History;
using CoreLens.Application.Insights;
using CoreLens.Application.Inventory;
using Microsoft.AspNetCore.Mvc;

namespace CoreLens.Api.Controllers;

[ApiController]
[Route("api/computers")]
public sealed class ComputersController : ControllerBase
{
    private readonly GetInventoryHandler _inventory;
    private readonly GetHistoryHandler _history;
    private readonly GetAlertsHandler _alerts;
    private readonly GetInsightsHandler _insights;

    public ComputersController(
        GetInventoryHandler inventory,
        GetHistoryHandler history,
        GetAlertsHandler alerts,
        GetInsightsHandler insights)
    {
        _inventory = inventory;
        _history = history;
        _alerts = alerts;
        _insights = insights;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken) =>
        Ok(await _inventory.ListComputersAsync(cancellationToken));

    [HttpGet("{computerId:guid}")]
    public async Task<IActionResult> Get(Guid computerId, CancellationToken cancellationToken)
    {
        var inventory = await _inventory.GetAsync(computerId, cancellationToken);
        return inventory is null ? NotFound() : Ok(inventory);
    }

    [HttpGet("{computerId:guid}/history")]
    public async Task<IActionResult> History(
        Guid computerId,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] string? bucket,
        [FromQuery] string? name,
        [FromQuery] string? componentKey,
        CancellationToken cancellationToken)
    {
        var end = to ?? DateTimeOffset.UtcNow;
        var start = from ?? end.AddHours(-1);
        var result = await _history.GetAsync(computerId, start, end, bucket, name, componentKey, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{computerId:guid}/alerts")]
    public async Task<IActionResult> Alerts(Guid computerId, [FromQuery] int take = 50, CancellationToken cancellationToken = default) =>
        Ok(await _alerts.ListHistoryAsync(computerId, Math.Clamp(take, 1, 200), cancellationToken));

    [HttpGet("{computerId:guid}/insights")]
    public async Task<IActionResult> Insights(Guid computerId, CancellationToken cancellationToken) =>
        Ok(await _insights.GetAsync(computerId, cancellationToken));
}
