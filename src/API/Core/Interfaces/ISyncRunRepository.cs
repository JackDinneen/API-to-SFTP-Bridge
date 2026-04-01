namespace API.Core.Interfaces;

using API.Core.Entities;

public interface ISyncRunRepository
{
    Task<SyncRun> CreateAsync(SyncRun syncRun, CancellationToken cancellationToken = default);
    Task<SyncRun?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SyncRun>> GetByConnectionIdAsync(Guid connectionId, int limit = 50, CancellationToken cancellationToken = default);
    Task<SyncRun?> GetByIdWithRecordsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SyncRun> UpdateAsync(SyncRun syncRun, CancellationToken cancellationToken = default);
    Task<Dictionary<Guid, SyncRun>> GetLatestByConnectionIdsAsync(IEnumerable<Guid> connectionIds, CancellationToken cancellationToken = default);
    Task<decimal?> GetSuccessRateAsync(Guid connectionId, int recentCount = 10, CancellationToken cancellationToken = default);
    Task<Dictionary<Guid, decimal?>> GetSuccessRatesByConnectionIdsAsync(IEnumerable<Guid> connectionIds, int recentCount = 10, CancellationToken cancellationToken = default);
}
