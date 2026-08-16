using CoreLens.Domain;

namespace CoreLens.Application.Abstractions;

public interface IInsightProvider
{
    Task<IReadOnlyList<Insight>> GetInsightsAsync(Guid computerId, CancellationToken cancellationToken);
}
