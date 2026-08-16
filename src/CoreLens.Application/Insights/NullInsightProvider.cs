using CoreLens.Application.Abstractions;
using CoreLens.Contracts.Dtos;
using CoreLens.Domain;

namespace CoreLens.Application.Insights;

public sealed class NullInsightProvider : IInsightProvider
{
    public Task<IReadOnlyList<Insight>> GetInsightsAsync(Guid computerId, CancellationToken cancellationToken)
    {
        IReadOnlyList<Insight> insights =
        [
            new Insight
            {
                Title = "AI insights not configured",
                Summary = "Plug an LLM or local model into IInsightProvider. Ingestion never calls this path.",
                Provider = "none"
            }
        ];

        return Task.FromResult(insights);
    }
}

public sealed class GetInsightsHandler
{
    private readonly IInsightProvider _provider;

    public GetInsightsHandler(IInsightProvider provider)
    {
        _provider = provider;
    }

    public async Task<IReadOnlyList<InsightDto>> GetAsync(Guid computerId, CancellationToken cancellationToken)
    {
        var insights = await _provider.GetInsightsAsync(computerId, cancellationToken);
        return insights.Select(i => new InsightDto
        {
            Title = i.Title,
            Summary = i.Summary,
            Provider = i.Provider
        }).ToList();
    }
}
