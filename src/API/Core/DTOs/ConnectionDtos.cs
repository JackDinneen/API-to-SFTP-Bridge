namespace API.Core.DTOs;

using API.Core.Models;

public class CreateConnectionRequest
{
    public string Name { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public AuthType AuthType { get; set; }
    public string? ScheduleCron { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string PlatformName { get; set; } = string.Empty;
    public string? SftpHost { get; set; }
    public int SftpPort { get; set; } = 22;
    public string? SftpPath { get; set; }
    public int ReportingLagDays { get; set; } = 5;
    public string? EndpointPath { get; set; }
    public string? PaginationStrategy { get; set; }
    public string? PaginationConfig { get; set; }
}

public class UpdateConnectionRequest
{
    public string Name { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public AuthType AuthType { get; set; }
    public ConnectionStatus Status { get; set; }
    public string? ScheduleCron { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string PlatformName { get; set; } = string.Empty;
    public string? SftpHost { get; set; }
    public int SftpPort { get; set; } = 22;
    public string? SftpPath { get; set; }
    public int ReportingLagDays { get; set; } = 5;
    public string? EndpointPath { get; set; }
    public string? PaginationStrategy { get; set; }
    public string? PaginationConfig { get; set; }
}

public class ConnectionDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public AuthType AuthType { get; set; }
    public ConnectionStatus Status { get; set; }
    public string? ScheduleCron { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string PlatformName { get; set; } = string.Empty;
    public string? SftpHost { get; set; }
    public int SftpPort { get; set; }
    public string? SftpPath { get; set; }
    public int ReportingLagDays { get; set; }
    public string? EndpointPath { get; set; }
    public string? PaginationStrategy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class SyncRunDto
{
    public Guid Id { get; set; }
    public Guid ConnectionId { get; set; }
    public SyncRunStatus Status { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int RecordCount { get; set; }
    public long FileSize { get; set; }
    public string? FileName { get; set; }
    public string? ErrorMessage { get; set; }
    public string TriggeredBy { get; set; } = string.Empty;
    public int RetryCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class TriggerSyncRequest
{
    // Intentionally empty; triggeredBy is set from the current user
}

public class TestConnectionRequest
{
    // Intentionally empty; connection details come from the stored connection
}
