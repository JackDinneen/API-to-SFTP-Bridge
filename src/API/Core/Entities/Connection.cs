namespace API.Core.Entities;

using API.Core.Models;

public class Connection : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public AuthType AuthType { get; set; }
    public ConnectionStatus Status { get; set; } = ConnectionStatus.Paused;
    public string? ScheduleCron { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string PlatformName { get; set; } = string.Empty;
    public string? SftpHost { get; set; }
    public int SftpPort { get; set; } = 22;
    public string? SftpPath { get; set; }
    public int ReportingLagDays { get; set; } = 5;
    public string? EndpointPath { get; set; }
    public string? PaginationStrategy { get; set; }
    public string? PaginationConfig { get; set; } // JSON
    public string? ResponseSampleJson { get; set; }
    public string? IterationEndpointPath { get; set; }
    public string? IterationJsonPath { get; set; }

    // Foreign keys
    public Guid CreatedById { get; set; }
    public UserProfile CreatedBy { get; set; } = null!;

    // Navigation
    public ICollection<ConnectionCredential> Credentials { get; set; } = new List<ConnectionCredential>();
    public ICollection<ConnectionMapping> Mappings { get; set; } = new List<ConnectionMapping>();
    public ICollection<SyncRun> SyncRuns { get; set; } = new List<SyncRun>();
    public NotificationConfig? NotificationConfig { get; set; }
}
