using CoreLens.Domain;

namespace CoreLens.Application.Abstractions;

public interface IAlertRuleRepository
{
    Task<IReadOnlyList<AlertRule>> ListEnabledAsync(CancellationToken cancellationToken);
    Task EnsureDefaultsAsync(CancellationToken cancellationToken);
}
