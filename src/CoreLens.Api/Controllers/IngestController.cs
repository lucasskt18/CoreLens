using CoreLens.Application.Ingest;
using CoreLens.Contracts.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace CoreLens.Api.Controllers;

[ApiController]
[Route("internal")]
public sealed class IngestController : ControllerBase
{
    private readonly IngestMetricsHandler _handler;

    public IngestController(IngestMetricsHandler handler)
    {
        _handler = handler;
    }

    [HttpPost("ingest")]
    public async Task<IActionResult> Ingest([FromBody] IngestRequest request, CancellationToken cancellationToken)
    {
        if (request.ComputerId == Guid.Empty)
        {
            return BadRequest("computerId is required.");
        }

        await _handler.HandleAsync(request, cancellationToken);
        return Accepted();
    }
}
