namespace API.Infrastructure.Repositories;

using API.Core.Entities;
using API.Core.Interfaces;
using API.Core.Models;
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
        // Attach the sync run if not already tracked
        var entry = _context.Entry(syncRun);
        if (entry.State == EntityState.Detached)
        {
            _context.SyncRuns.Attach(syncRun);
            entry.State = EntityState.Modified;
        }

        // Ensure new child records are marked as Added (not Modified)
        foreach (var record in syncRun.Records)
        {
            var recordEntry = _context.Entry(record);
            if (recordEntry.State == EntityState.Detached || recordEntry.State == EntityState.Modified)
            {
                if (record.Id == Guid.Empty || !await _context.Set<SyncRunRecord>().AnyAsync(r => r.Id == record.Id, cancellationToken))
                {
                    recordEntry.State = EntityState.Added;
                }
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return syncRun;
    }

    public async Task<Dictionary<Guid, SyncRun>> GetLatestByConnectionIdsAsync(IEnumerable<Guid> connectionIds, CancellationToken cancellationToken = default)
    {
        var ids = connectionIds.ToList();
        if (ids.Count == 0) return new Dictionary<Guid, SyncRun>();

        var latestRuns = await _context.SyncRuns
            .Where(s => ids.Contains(s.ConnectionId))
            .GroupBy(s => s.ConnectionId)
            .Select(g => g.OrderByDescending(s => s.CreatedAt).First())
            .ToListAsync(cancellationToken);

        return latestRuns.ToDictionary(s => s.ConnectionId);
    }

    public async Task<decimal?> GetSuccessRateAsync(Guid connectionId, int recentCount = 10, CancellationToken cancellationToken = default)
    {
        var rates = await GetSuccessRatesByConnectionIdsAsync(new[] { connectionId }, recentCount, cancellationToken);
        return rates.GetValueOrDefault(connectionId);
    }

    public async Task<Dictionary<Guid, decimal?>> GetSuccessRatesByConnectionIdsAsync(IEnumerable<Guid> connectionIds, int recentCount = 10, CancellationToken cancellationToken = default)
    {
        var ids = connectionIds.ToList();
        if (ids.Count == 0) return new Dictionary<Guid, decimal?>();

        // Fetch recent completed runs for all connections in a single query
        var recentRuns = await _context.SyncRuns
            .Where(s => ids.Contains(s.ConnectionId)
                && (s.Status == SyncRunStatus.Succeeded || s.Status == SyncRunStatus.Failed))
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);

        var result = new Dictionary<Guid, decimal?>();
        foreach (var id in ids)
        {
            var runsForConnection = recentRuns
                .Where(s => s.ConnectionId == id)
                .Take(recentCount)
                .ToList();

            if (runsForConnection.Count == 0)
            {
                result[id] = null;
            }
            else
            {
                var succeeded = runsForConnection.Count(s => s.Status == SyncRunStatus.Succeeded);
                result[id] = Math.Round((decimal)succeeded / runsForConnection.Count * 100, 1);
            }
        }

        return result;
    }
}
