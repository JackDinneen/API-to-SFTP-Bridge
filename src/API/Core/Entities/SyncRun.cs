namespace API.Core.Entities;

using API.Core.Models;

public class SyncRun : BaseEntity
{
    public Guid ConnectionId { get; set; }
    public Connection Connection { get; set; } = null!;
    public SyncRunStatus Status { get; set; } = SyncRunStatus.Pending;
    public DateTime? CompletedAt { get; set; }
    public int RecordCount { get; set; }
    public long FileSize { get; set; }
    public string? FileName { get; set; }
    public string? ErrorMessage { get; set; }
    public string TriggeredBy { get; set; } = string.Empty; // "scheduled" or user email
    public string? BlobStorageUrl { get; set; } // Azure Blob URL for the generated CSV
    public int RetryCount { get; set; }

    // Navigation
    public ICollection<SyncRunRecord> Records { get; set; } = new List<SyncRunRecord>();
}
