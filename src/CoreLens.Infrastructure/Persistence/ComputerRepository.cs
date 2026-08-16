using CoreLens.Application.Abstractions;
using CoreLens.Domain;
using Microsoft.EntityFrameworkCore;

namespace CoreLens.Infrastructure.Persistence;

public sealed class ComputerRepository : IComputerRepository
{
    private readonly CoreLensDbContext _db;

    public ComputerRepository(CoreLensDbContext db)
    {
        _db = db;
    }

    public async Task UpsertAsync(Computer computer, CancellationToken cancellationToken)
    {
        var existing = await _db.Computers.FirstOrDefaultAsync(c => c.Id == computer.Id, cancellationToken);
        if (existing is null)
        {
            _db.Computers.Add(computer);
        }
        else
        {
            existing.Hostname = computer.Hostname;
            existing.OsVersion = computer.OsVersion;
            existing.AgentVersion = computer.AgentVersion;
            existing.LastSeenAt = computer.LastSeenAt;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task<Computer?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        _db.Computers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Computer>> ListAsync(CancellationToken cancellationToken) =>
        await _db.Computers.AsNoTracking().OrderBy(c => c.Hostname).ToListAsync(cancellationToken);
}
