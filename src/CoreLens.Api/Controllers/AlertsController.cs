using CoreLens.Application.Alerts;
using Microsoft.AspNetCore.Mvc;

namespace CoreLens.Api.Controllers;

[ApiController]
[Route("api/alerts")]
public sealed class AlertsController : ControllerBase
{
    private readonly GetAlertsHandler _alerts;

    public AlertsController(GetAlertsHandler alerts)
    {
        _alerts = alerts;
    }

    [HttpGet("rules")]
    public async Task<IActionResult> Rules(CancellationToken cancellationToken) =>
        Ok(await _alerts.ListRulesAsync(cancellationToken));
}
