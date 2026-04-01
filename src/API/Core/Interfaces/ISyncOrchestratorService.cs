namespace API.Core.Interfaces;

using API.Core.Models;

public interface ISyncOrchestratorService
{
    Task<SyncResult> ExecuteSyncAsync(Guid connectionId, string triggeredBy, CancellationToken cancellationToken = default);
}

public class SyncResult
{
    public bool Success { get; set; }
    public Guid SyncRunId { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorStep { get; set; }
    public int RecordCount { get; set; }
    public string? FileName { get; set; }
}
