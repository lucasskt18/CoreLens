using CoreLens.Domain;

namespace CoreLens.Application.Abstractions;

public interface IComponentRepository
{
    Task UpsertRangeAsync(Guid computerId, IReadOnlyList<Component> components, CancellationToken cancellationToken);
    Task<IReadOnlyList<Component>> ListByComputerAsync(Guid computerId, CancellationToken cancellationToken);
    Task<IReadOnlyDictionary<string, Component>> GetMapAsync(Guid computerId, CancellationToken cancellationToken);
}
