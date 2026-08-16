using CoreLens.Application.Abstractions;
using CoreLens.Domain;
using Microsoft.EntityFrameworkCore;

namespace CoreLens.Infrastructure.Persistence;

public sealed class ComponentRepository : IComponentRepository
{
    private readonly CoreLensDbContext _db;

    public ComponentRepository(CoreLensDbContext db)
    {
        _db = db;
    }

    public async Task UpsertRangeAsync(Guid computerId, IReadOnlyList<Component> components, CancellationToken cancellationToken)
    {
        var existing = await _db.Components
            .Where(c => c.ComputerId == computerId)
            .ToListAsync(cancellationToken);

        var byKey = existing.ToDictionary(c => c.StableKey, StringComparer.OrdinalIgnoreCase);

        foreach (var incoming in components)
        {
            if (byKey.TryGetValue(incoming.StableKey, out var current))
            {
                current.Type = incoming.Type;
                current.Manufacturer = incoming.Manufacturer ?? current.Manufacturer;
                current.Model = incoming.Model ?? current.Model;
                current.SpecsJson = incoming.SpecsJson ?? current.SpecsJson;
            }
            else
            {
                incoming.ComputerId = computerId;
                if (incoming.Id == Guid.Empty)
                {
                    incoming.Id = Guid.NewGuid();
                }

                _db.Components.Add(incoming);
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Component>> ListByComputerAsync(Guid computerId, CancellationToken cancellationToken) =>
        await _db.Components.AsNoTracking()
            .Where(c => c.ComputerId == computerId)
            .OrderBy(c => c.Type)
            .ThenBy(c => c.StableKey)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<string, Component>> GetMapAsync(Guid computerId, CancellationToken cancellationToken)
    {
        var list = await ListByComputerAsync(computerId, cancellationToken);
        return list.ToDictionary(c => c.StableKey, StringComparer.OrdinalIgnoreCase);
    }
}
