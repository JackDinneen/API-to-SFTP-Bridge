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
    public bool Activate { get; set; }
    public List<CreateMappingDto> Mappings { get; set; } = new();
    public CreateCredentialDto? Credentials { get; set; }
}

public class CreateMappingDto
{
    public string SourcePath { get; set; } = string.Empty;
    public string TargetColumn { get; set; } = string.Empty;
    public string TransformType { get; set; } = "DirectMapping";
    public string? TransformConfig { get; set; }
    public int SortOrder { get; set; }
}

public class CreateCredentialDto
{
    public string? ApiKey { get; set; }
    public string? ApiKeyHeader { get; set; }
    public string? BasicUsername { get; set; }
    public string? BasicPassword { get; set; }
    public string? OAuthClientId { get; set; }
    public string? OAuthClientSecret { get; set; }
    public string? OAuthTokenUrl { get; set; }
    public string? SftpUsername { get; set; }
    public string? SftpPassword { get; set; }
    public List<CustomHeaderDto>? CustomHeaders { get; set; }
}

public class CustomHeaderDto
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
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
    public DateTime? LastSyncAt { get; set; }
    public int? LastSyncRecordCount { get; set; }
    public DateTime? NextSyncAt { get; set; }
    public decimal? SuccessRate { get; set; }
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
    public List<SyncRunRecordDto>? Records { get; set; }
}

public class SyncRunRecordDto
{
    public string? AssetId { get; set; }
    public string? AssetName { get; set; }
    public string? SubmeterCode { get; set; }
    public string? UtilityType { get; set; }
    public int? Year { get; set; }
    public int? Month { get; set; }
    public decimal? Value { get; set; }
    public bool IsValid { get; set; }
    public string? ValidationMessage { get; set; }
}

public class TriggerSyncRequest
{
    // Intentionally empty; triggeredBy is set from the current user
}

public class TestConnectionRequest
{
    // Intentionally empty; connection details come from the stored connection
}

public class TestConnectionPreviewRequest
{
    public string BaseUrl { get; set; } = string.Empty;
    public AuthType AuthType { get; set; }
    public CreateCredentialDto? Credentials { get; set; }
}

public class FetchSampleRequest
{
    public string BaseUrl { get; set; } = string.Empty;
    public string EndpointPath { get; set; } = string.Empty;
    public AuthType AuthType { get; set; }
    public CreateCredentialDto? Credentials { get; set; }
}
