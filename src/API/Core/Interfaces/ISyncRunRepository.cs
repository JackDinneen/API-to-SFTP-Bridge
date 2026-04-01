namespace API.Core.Interfaces;

using API.Core.Entities;

public interface ISyncRunRepository
{
    Task<SyncRun> CreateAsync(SyncRun syncRun, CancellationToken cancellationToken = default);
    Task<SyncRun?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SyncRun>> GetByConnectionIdAsync(Guid connectionId, int limit = 50, CancellationToken cancellationToken = default);
    Task<SyncRun> UpdateAsync(SyncRun syncRun, CancellationToken cancellationToken = default);
}
