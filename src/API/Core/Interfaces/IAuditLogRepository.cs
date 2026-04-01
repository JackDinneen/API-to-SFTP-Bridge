namespace API.Core.Interfaces;

using API.Core.Entities;

public interface IAuditLogRepository
{
    Task<IReadOnlyList<AuditLog>> GetFilteredAsync(
        string? entityType = null,
        DateTime? from = null,
        DateTime? to = null,
        int limit = 100,
        CancellationToken cancellationToken = default);
}
