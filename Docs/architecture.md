# Architecture Overview

## High-Level Architecture

Obi Bridge is an ESG data integration platform that sits between external REST APIs and Obi's SFTP ingestion endpoint. It fetches data from source APIs, transforms it via user-configured mappings into Obi's 7-column CSV schema, validates it against reference data, and delivers the CSV to Obi's SFTP server on a configurable schedule.

```
External REST APIs
        |
        v
  +-----------+     +-----------------+     +------------+
  |  API      |---->| Transform &     |---->| CSV        |
  |  Connector|     | Map Engine      |     | Generator  |
  +-----------+     +-----------------+     +------------+
        |                                        |
  +-----------+                            +------------+
  |  Auth     |                            | SFTP       |
  |  Vault    |                            | Delivery   |
  +-----------+                            +------------+
        |                                        |
  +-----------+     +-----------------+          v
  | Scheduler |     | Validation &    |     Obi SFTP Server
  | (Hangfire)|     | Reconciliation  |
  +-----------+     +-----------------+
        |                 |
  +-----------+     +-----------------+
  | Audit Log |     | Notifications   |
  +-----------+     +-----------------+
```

## Technology Stack

| Layer | Technology |
|-------|-----------|
| Backend | C# .NET 10 Web API |
| ORM | Entity Framework Core (SQL Server) |
| Reporting queries | Dapper (when needed) |
| Authentication | Azure AD via MSAL (`Microsoft.Identity.Web`) |
| Frontend | Vue 3 (Composition API) + Tailwind CSS |
| Scheduler | Hangfire with SQL Server storage |
| SFTP client | Renci.SshNet (`SSH.NET`) |
| Secrets | Azure Key Vault |
| File storage | Azure Blob Storage |
| Rate limiting | `Microsoft.AspNetCore.RateLimiting` (fixed window) |

## Project Structure

```
src/
  API/
    Controllers/        # API endpoints (8 controllers)
    Core/
      Entities/         # Domain models (9 entity types)
      DTOs/             # Request/response objects
      Interfaces/       # Service and repository contracts
      Models/           # Shared models (ApiResponse<T>, enums)
    Application/
      Auth/             # Authorization policies and handlers
      Services/         # Business logic implementations
      Validators/       # FluentValidation validators
    Infrastructure/
      Data/             # ApplicationDbContext, EF Core configuration
      Repositories/     # Data access implementations (6 repositories)
    Migrations/         # EF Core migrations
    Program.cs          # DI registration, middleware pipeline
tests/
  UnitTests/            # xUnit + Moq unit tests (264 tests)
  IntegrationTests/     # WebApplicationFactory integration tests (12 tests)
Docs/                   # Project documentation
```

## Key Design Decisions

- **Clean architecture**: Controllers delegate to services, services use repositories. Controllers never access `DbContext` directly.
- **ApiResponse<T> wrapper**: All API responses use a consistent envelope with `success`, `data`, `message`, and `errors` fields.
- **Soft delete**: All entities extending `BaseEntity` have an `IsDeleted` flag filtered by a global EF Core query filter. `AuditLog` is excluded (immutable, append-only).
- **Enums stored as strings**: All enum columns use string conversion for readability and forward compatibility.
- **Secrets never in DB**: `ConnectionCredential.KeyVaultSecretName` is a reference to Azure Key Vault, not the secret value itself.
- **Role-based access**: Three roles (Admin, Operator, Viewer) enforced via ASP.NET authorization policies.
- **Rate limiting**: Global IP-based fixed-window limiter (200 requests/minute) prevents abuse.
- **Global exception handler**: Catches unhandled exceptions and returns a generic error, preventing internal details from leaking.

## Authentication and Authorization

Authentication is handled by Azure AD via `Microsoft.Identity.Web`. In development, a fallback JWT bearer scheme is available when no `AzureAd` configuration section is present.

Three authorization policies govern access:

| Policy | Allowed Roles |
|--------|--------------|
| `AdminOnly` | Admin |
| `AdminOrOperator` | Admin, Operator |
| `AllAuthenticated` | Any authenticated user |

## Database

SQL Server with EF Core. Nine tables: `UserProfiles`, `Connections`, `ConnectionCredentials`, `ConnectionMappings`, `SyncRuns`, `SyncRunRecords`, `ReferenceData`, `AuditLogs`, `NotificationConfigs`. See `Docs/schema-overview.md` for full schema details.

## Middleware Pipeline

1. Global exception handler (returns generic 500 without internal details)
2. Swagger (development only)
3. HTTPS redirection
4. CORS (`AllowFrontend` policy)
5. Rate limiter
6. Authentication
7. Authorization
8. Controller routing
