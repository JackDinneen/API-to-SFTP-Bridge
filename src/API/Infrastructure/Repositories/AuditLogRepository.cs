namespace API.Infrastructure.Repositories;

using API.Core.Entities;
using API.Core.Interfaces;
using API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly ApplicationDbContext _context;

    public AuditLogRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<AuditLog>> GetFilteredAsync(
        string? entityType = null,
        DateTime? from = null,
        DateTime? to = null,
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        var query = _context.AuditLogs.AsQueryable();

        if (!string.IsNullOrEmpty(entityType))
        {
            query = query.Where(a => a.EntityType == entityType);
        }

        if (from.HasValue)
        {
            query = query.Where(a => a.CreatedAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(a => a.CreatedAt <= to.Value);
        }

        return await query
            .OrderByDescending(a => a.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }
}
