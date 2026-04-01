namespace API.Infrastructure.Repositories;

using API.Core.Entities;
using API.Core.Interfaces;
using API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

public class SyncRunRepository : ISyncRunRepository
{
    private readonly ApplicationDbContext _context;

    public SyncRunRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SyncRun> CreateAsync(SyncRun syncRun, CancellationToken cancellationToken = default)
    {
        _context.SyncRuns.Add(syncRun);
        await _context.SaveChangesAsync(cancellationToken);
        return syncRun;
    }

    public async Task<SyncRun?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.SyncRuns.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<IReadOnlyList<SyncRun>> GetByConnectionIdAsync(Guid connectionId, int limit = 50, CancellationToken cancellationToken = default)
    {
        return await _context.SyncRuns
            .Where(s => s.ConnectionId == connectionId)
            .OrderByDescending(s => s.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<SyncRun?> GetByIdWithRecordsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.SyncRuns
            .Include(s => s.Records)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<SyncRun> UpdateAsync(SyncRun syncRun, CancellationToken cancellationToken = default)
    {
        _context.SyncRuns.Update(syncRun);
        await _context.SaveChangesAsync(cancellationToken);
        return syncRun;
    }
}
