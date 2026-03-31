# Database Schema Overview

<!-- This document is updated automatically when EF Core migrations are created. -->
<!-- See: .claude/skills/new-migration/SKILL.md -->

## Tables

### UserProfiles
Primary identity table for users authenticated via Azure AD.

| Column | Type | Constraints |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Auto-generated GUID |
| AzureAdId | nvarchar(128) | Unique index |
| Email | nvarchar(256) | Required, unique index |
| DisplayName | nvarchar(256) | Required |
| Role | nvarchar(50) | Enum: Admin, Operator, Viewer |
| CreatedAt | datetime2 | Auto-set |
| UpdatedAt | datetime2 | Auto-set |
| IsDeleted | bit | Soft delete filter |

### Connections
Represents an API-to-SFTP bridge configuration for a client/platform pair.

| Column | Type | Constraints |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Auto-generated GUID |
| Name | nvarchar(256) | Required |
| BaseUrl | nvarchar(2048) | Required |
| AuthType | nvarchar(50) | Enum: ApiKey, OAuth2ClientCredentials, BasicAuth, CustomHeaders |
| Status | nvarchar(50) | Enum: Active, Paused, Error. Indexed |
| ScheduleCron | nvarchar(128) | Nullable |
| ClientName | nvarchar(256) | Required, indexed |
| PlatformName | nvarchar(256) | Required |
| SftpHost | nvarchar(512) | Nullable |
| SftpPort | int | Default 22 |
| SftpPath | nvarchar(1024) | Nullable |
| ReportingLagDays | int | Default 5 |
| EndpointPath | nvarchar(2048) | Nullable |
| PaginationStrategy | nvarchar(128) | Nullable |
| PaginationConfig | nvarchar(max) | JSON, nullable |
| ResponseSampleJson | nvarchar(max) | Nullable |
| CreatedById | uniqueidentifier (FK) | References UserProfiles.Id, indexed, Restrict delete |
| CreatedAt | datetime2 | Auto-set |
| UpdatedAt | datetime2 | Auto-set |
| IsDeleted | bit | Soft delete filter |

### ConnectionCredentials
Stores references to Azure Key Vault secrets for connection authentication. Never stores actual secret values.

| Column | Type | Constraints |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Auto-generated GUID |
| ConnectionId | uniqueidentifier (FK) | References Connections.Id, indexed, cascade delete |
| CredentialType | nvarchar(50) | Enum: AuthType values |
| KeyVaultSecretName | nvarchar(256) | Required |
| Label | nvarchar(128) | Nullable |
| CreatedAt | datetime2 | Auto-set |
| UpdatedAt | datetime2 | Auto-set |
| IsDeleted | bit | Soft delete filter |

### ConnectionMappings
Defines how API response JSON fields map to the standard CSV output columns.

| Column | Type | Constraints |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Auto-generated GUID |
| ConnectionId | uniqueidentifier (FK) | References Connections.Id, indexed, cascade delete |
| SourcePath | nvarchar(1024) | Required, JSON path |
| TargetColumn | nvarchar(128) | Required |
| TransformType | nvarchar(50) | Enum: DirectMapping, ValueMapping, UnitConversion, DateParse, StaticValue, Concatenation, Split |
| TransformConfig | nvarchar(max) | JSON, nullable |
| SortOrder | int | |
| CreatedAt | datetime2 | Auto-set |
| UpdatedAt | datetime2 | Auto-set |
| IsDeleted | bit | Soft delete filter |

### SyncRuns
Records each execution of a connection sync (API fetch, transform, SFTP upload).

| Column | Type | Constraints |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Auto-generated GUID |
| ConnectionId | uniqueidentifier (FK) | References Connections.Id, indexed, cascade delete |
| Status | nvarchar(50) | Enum: Pending, Running, Succeeded, Failed. Indexed |
| CompletedAt | datetime2 | Nullable |
| RecordCount | int | |
| FileSize | bigint | |
| FileName | nvarchar(512) | Nullable |
| ErrorMessage | nvarchar(max) | Nullable |
| TriggeredBy | nvarchar(256) | Required ("scheduled" or user email) |
| BlobStorageUrl | nvarchar(2048) | Nullable, Azure Blob URL |
| RetryCount | int | |
| CreatedAt | datetime2 | Indexed, auto-set |
| UpdatedAt | datetime2 | Auto-set |
| IsDeleted | bit | Soft delete filter |

### SyncRunRecords
Individual data rows extracted and transformed during a sync run.

| Column | Type | Constraints |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Auto-generated GUID |
| SyncRunId | uniqueidentifier (FK) | References SyncRuns.Id, indexed, cascade delete |
| AssetId | nvarchar(256) | Nullable |
| AssetName | nvarchar(512) | Nullable |
| SubmeterCode | nvarchar(256) | Nullable |
| UtilityType | nvarchar(128) | Nullable |
| Year | int | Nullable |
| Month | int | Nullable |
| Value | decimal(18,6) | Nullable |
| IsValid | bit | Indexed |
| ValidationMessage | nvarchar(max) | Nullable |
| CreatedAt | datetime2 | Auto-set |
| UpdatedAt | datetime2 | Auto-set |
| IsDeleted | bit | Soft delete filter |

### ReferenceData
Uploaded reference data used to validate sync run records against known assets/meters.

| Column | Type | Constraints |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Auto-generated GUID |
| AssetId | nvarchar(256) | Required, indexed |
| AssetName | nvarchar(512) | Required |
| SubmeterCode | nvarchar(256) | Required, indexed |
| UtilityType | nvarchar(128) | Required |
| UploadedById | uniqueidentifier (FK) | References UserProfiles.Id, indexed, Restrict delete |
| CreatedAt | datetime2 | Auto-set |
| UpdatedAt | datetime2 | Auto-set |
| IsDeleted | bit | Soft delete filter |

### AuditLogs
Immutable append-only log of user and system actions. No soft delete; no cascade delete.

| Column | Type | Constraints |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Auto-generated GUID |
| Action | nvarchar(256) | Required |
| EntityType | nvarchar(256) | Required, indexed |
| EntityId | uniqueidentifier | Nullable |
| UserId | uniqueidentifier (FK) | Nullable, references UserProfiles.Id, indexed, SetNull on delete |
| Details | nvarchar(max) | JSON, nullable |
| CreatedAt | datetime2 | Indexed |

### NotificationConfigs
One-to-one notification settings per connection.

| Column | Type | Constraints |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Auto-generated GUID |
| ConnectionId | uniqueidentifier (FK) | References Connections.Id, unique index, cascade delete |
| NotifyOnSuccess | bit | Default true |
| NotifyOnFailure | bit | Default true |
| NotifyOnValidationWarning | bit | Default true |
| NotifyOnNewMeter | bit | Default true |
| EmailRecipients | nvarchar(2048) | Nullable, comma-separated |
| WebhookUrl | nvarchar(2048) | Nullable |
| CreatedAt | datetime2 | Auto-set |
| UpdatedAt | datetime2 | Auto-set |
| IsDeleted | bit | Soft delete filter |

## Entity Relationship Overview

```
UserProfiles 1──* Connections        (CreatedBy, Restrict delete)
UserProfiles 1──* AuditLogs          (User, SetNull on delete)
UserProfiles 1──* ReferenceData      (UploadedBy, Restrict delete)

Connections  1──* ConnectionCredentials  (Cascade delete)
Connections  1──* ConnectionMappings     (Cascade delete)
Connections  1──* SyncRuns               (Cascade delete)
Connections  1──1 NotificationConfigs    (Cascade delete)

SyncRuns     1──* SyncRunRecords         (Cascade delete)
```

## Key Design Decisions

- **Soft delete**: All entities extending `BaseEntity` have an `IsDeleted` flag with a global query filter. `AuditLog` is excluded (immutable).
- **Enums stored as strings**: All enum columns use string conversion for readability and forward compatibility.
- **Timestamps auto-managed**: `CreatedAt` and `UpdatedAt` are set automatically via `SaveChanges`/`SaveChangesAsync` overrides in `ApplicationDbContext`.
- **Secrets never stored in DB**: `ConnectionCredential.KeyVaultSecretName` is a reference to Azure Key Vault, not the secret value itself.
- **Decimal precision**: `SyncRunRecord.Value` uses `decimal(18,6)` for ESG meter readings.

## Migration History

| Migration | Date | Description |
|-----------|------|-------------|
| InitialSchema | 2026-03-31 | Initial database schema with all 9 tables |
