using System.Net.Http.Json;
using System.Text.Json;
using CoreLens.Contracts.Dtos;

namespace CoreLens.Agent.Transport;

public sealed class ApiIngestClient
{
    private readonly HttpClient _http;
    private readonly ILogger<ApiIngestClient> _logger;

    public ApiIngestClient(HttpClient http, ILogger<ApiIngestClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task SendAsync(IngestRequest request, CancellationToken cancellationToken)
    {
        using var response = await _http.PostAsJsonAsync(
            "internal/ingest",
            request,
            new JsonSerializerOptions(JsonSerializerDefaults.Web),
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Ingest failed ({Status}): {Body}", (int)response.StatusCode, body);
            response.EnsureSuccessStatusCode();
        }
    }
}
