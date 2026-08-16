using CoreLens.Domain;

namespace CoreLens.Application.Abstractions;

public interface IComputerRepository
{
    Task UpsertAsync(Computer computer, CancellationToken cancellationToken);
    Task<Computer?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Computer>> ListAsync(CancellationToken cancellationToken);
}
