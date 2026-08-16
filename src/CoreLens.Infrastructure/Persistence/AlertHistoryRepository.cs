using CoreLens.Application.Abstractions;
using CoreLens.Domain;
using Microsoft.EntityFrameworkCore;

namespace CoreLens.Infrastructure.Persistence;

public sealed class AlertHistoryRepository : IAlertHistoryRepository
{
    private readonly CoreLensDbContext _db;

    public AlertHistoryRepository(CoreLensDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(AlertHistory history, CancellationToken cancellationToken)
    {
        _db.AlertHistory.Add(history);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AlertHistory>> ListAsync(Guid computerId, int take, CancellationToken cancellationToken) =>
        await _db.AlertHistory.AsNoTracking()
            .Where(a => a.ComputerId == computerId)
            .OrderByDescending(a => a.Time)
            .Take(take)
            .ToListAsync(cancellationToken);
}
