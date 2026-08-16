using CoreLens.Domain;

namespace CoreLens.Application.Abstractions;

public interface IAlertHistoryRepository
{
    Task AddAsync(AlertHistory history, CancellationToken cancellationToken);
    Task<IReadOnlyList<AlertHistory>> ListAsync(Guid computerId, int take, CancellationToken cancellationToken);
}
