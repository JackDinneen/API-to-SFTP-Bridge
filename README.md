# Obi Bridge

API-to-SFTP bridge that automates ESG data ingestion into the Obi platform.

## What it does

Obi's ESG platform ingests data exclusively via SFTP upload of CSV files conforming to a strict 7-column schema. Today, connecting a new data provider requires weeks of manual CSV wrangling and back-and-forth coordination.

Obi Bridge eliminates this by sitting between any external REST API and Obi's SFTP endpoint. It:

1. **Connects** to any REST API (with full auth flexibility: API Key, OAuth 2.0, Basic Auth, Custom Headers)
2. **Fetches** data using configurable pagination (offset, cursor, page number) with rate limiting and retry
3. **Transforms** the response using user-configured field mappings (with auto-detect heuristics) into Obi's 7-column CSV format
4. **Validates** (optionally) the output against Obi's known Asset IDs, Submeter Codes, and Utility Types
5. **Delivers** the CSV to Obi's SFTP server on a configurable schedule
6. **Logs** every action in an immutable audit trail and sends email notifications

## Obi CSV Schema

Every generated CSV must conform to this structure:

| Column | Type | Example |
|--------|------|---------|
| Asset ID | String | `EF001` |
| Asset name | String | `Building 1` |
| Submeter Code | String | `ELEC01` |
| Utility Type | Enum | `Electricity` |
| Year | Integer | `2024` |
| Month | Integer | `12` |
| Value | Numeric | `8000` |

**Accepted Utility Types**: Electricity (kWh), Gas (kWh), Water (m3), Waste (t), District Heating (kWh), District Cooling (kWh)

**File naming**: `[client]_[platform]_[DDMMYYYY].csv` (e.g., `blackrock_GARBE_08052025.csv`)

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Backend | C# .NET 10 Web API, clean architecture |
| Frontend | Vue 3 (Composition API, `<script setup>`) + Tailwind CSS |
| Database | SQL Server + Entity Framework Core |
| Auth | Azure AD via MSAL (SSO with Google/Microsoft) |
| Scheduler | Hangfire (SQL Server storage) |
| SFTP | Renci.SshNet (SSH.NET) |
| Secrets | Azure Key Vault |
| Storage | Azure Blob Storage (CSV retention, reference data) |
| Testing | xUnit + FluentAssertions + Moq (backend), Vitest + Vue Test Utils (frontend) |

## Project Structure

```
src/
  API/
    Controllers/              # API endpoints (Connections, Sync, Auth, Health, Audit, Validation, ReferenceData, Notifications)
    Core/
      Entities/               # EF Core entities (Connection, SyncRun, AuditLog, etc.)
      DTOs/                   # Request/response data transfer objects
      Interfaces/             # Service and repository contracts
      Models/                 # ApiResponse<T> wrapper, enums
    Application/
      Services/               # Business logic (ApiConnector, CredentialVault, TransformEngine, CsvGenerator, SftpDelivery, SyncOrchestrator, Scheduler, Validation, Notifications, Audit)
      Validators/             # FluentValidation validators
      Auth/                   # Authorization policies and handlers
    Infrastructure/
      Data/                   # ApplicationDbContext, EF Core configuration
      Repositories/           # Data access implementations
      Migrations/             # EF Core migrations
      External/               # Azure Key Vault, Blob Storage clients
  ClientApp/
    src/
      components/
        shared/               # StatusBadge, HealthIndicator, DataTable, JsonTreeViewer, LoadingSpinner, ConfirmDialog
        dashboard/            # ConnectionCard, SyncHistoryTable
        wizard/               # Step1-6 wizard components, FieldMappingRow
      views/                  # DashboardView, ConnectionDetailView, WizardView, LoginView
      composables/            # useApi (typed HTTP client)
      stores/                 # Pinia stores (connections, sync, wizard)
      types/                  # TypeScript interfaces and enums
tests/
  UnitTests/                  # 264 unit tests
  IntegrationTests/           # 12 integration tests (WebApplicationFactory)
```

## Key Features

### Connection Setup Wizard (6 steps)
1. **Source API** -- Enter base URL, select auth method, enter credentials, test connection
2. **Endpoint Discovery** -- Specify endpoint, fetch sample response, configure pagination
3. **Field Mapping** -- Map source fields to Obi's 7 columns with auto-detect suggestions, configure transforms (value mapping, unit conversion, date parsing, static values, concatenation, split)
4. **Aggregation** -- Configure monthly aggregation for sub-monthly data (sum, average, last, max)
5. **Output** -- Set client/platform names, SFTP target, schedule (daily/weekly/monthly with cron), reporting lag
6. **Test & Activate** -- Run test sync, preview CSV, review validation, activate

### Transform Engine
Supports 7 transformation types:
- **Direct Mapping** -- extract from JSON path (including nested and wildcard arrays)
- **Value Mapping** -- enum translation (e.g., `"electric"` -> `"Electricity"`)
- **Unit Conversion** -- 8 pre-loaded conversions (MWh->kWh, GJ->kWh, litres->m3, etc.) + custom factors
- **Date Parse** -- extract Year/Month from ISO 8601, custom formats, Unix timestamps
- **Static Value** -- inject fixed values
- **Concatenation** -- combine multiple fields
- **Split** -- split a field and extract parts

### Validation Engine (optional, non-blocking)
5 checks against uploaded reference data:
1. Asset ID exists in Obi's records
2. Submeter Code exists and matches Asset ID
3. Utility Type is in accepted list
4. Duplicate detection (Asset ID + Submeter Code + Year + Month)
5. Range check (flag values >10x historical average)

### Sync Pipeline
`Fetch API data` -> `Transform` -> `Generate CSV` -> `Validate (optional)` -> `Deliver via SFTP` -> `Audit log` -> `Notify`

Scheduled via Hangfire (cron) or triggered manually. Retries with exponential backoff on failure.

### User Roles
| Role | Access |
|------|--------|
| Admin | Full access: create/edit/delete connections, manage users, configure SFTP, view all audit logs |
| Operator | View/configure assigned connections, trigger syncs, view own sync history |
| Viewer | Read-only: view sync status, download CSVs |

## API Endpoints

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/api/health` | None | Health check |
| GET | `/api/auth/me` | Any | Current user profile |
| GET | `/api/connections` | Any | List connections (filtered by role) |
| POST | `/api/connections` | Admin | Create connection |
| PUT | `/api/connections/{id}` | Admin | Update connection |
| DELETE | `/api/connections/{id}` | Admin | Soft delete connection |
| POST | `/api/connections/{id}/sync` | Admin, Operator | Trigger manual sync |
| POST | `/api/connections/{id}/test-connection` | Admin, Operator | Test API connectivity |
| GET | `/api/sync/{connectionId}/history` | Any | Sync run history |
| GET | `/api/sync/{connectionId}/latest` | Any | Latest sync run |
| POST | `/api/reference-data/upload` | Admin | Upload reference data (CSV/JSON) |
| GET | `/api/reference-data` | Any | List reference data |
| POST | `/api/validation/run/{connectionId}` | Any | Run validation on sync data |
| GET | `/api/connections/{id}/notifications` | Any | Get notification config |
| PUT | `/api/connections/{id}/notifications` | Admin, Operator | Update notification config |
| GET | `/api/audit` | Admin | Audit logs (filterable) |
| GET | `/api/audit/export` | Admin | Export audit logs as CSV |

## Getting Started

### Prerequisites
- .NET 10 SDK
- Node.js 20+
- SQL Server (local or Azure)
- Azure AD tenant (for auth)
- Azure Key Vault (for credential storage)

### Backend
```bash
# Restore and build
dotnet restore
dotnet build

# Run tests
dotnet test

# Apply migrations (requires SQL Server connection string)
dotnet ef database update --project src/API

# Run the API
dotnet run --project src/API
```

### Frontend
```bash
cd src/ClientApp

# Install dependencies
npm install

# Development server
npm run dev

# Type check, lint, build
npm run type-check
npm run lint
npm run build

# Run tests
npm run test
```

### Environment Variables

See [Docs/env-vars.md](Docs/env-vars.md) for the full list. Key variables:

| Variable | Description |
|----------|-------------|
| `ConnectionStrings__DefaultConnection` | SQL Server connection string |
| `AzureAd__Instance` | Azure AD instance URL |
| `AzureAd__TenantId` | Azure AD tenant ID |
| `AzureAd__ClientId` | Azure AD app registration client ID |
| `KeyVault__Uri` | Azure Key Vault URI |
| `AzureBlobStorage__ConnectionString` | Blob Storage connection string |
| `VITE_API_BASE_URL` | API base URL for the Vue frontend |

## Tests

| Suite | Count | Framework |
|-------|-------|-----------|
| Backend Unit Tests | 264 | xUnit + FluentAssertions + Moq |
| Backend Integration Tests | 12 | WebApplicationFactory |
| Frontend Component Tests | 35 | Vitest + Vue Test Utils |
| **Total** | **311** | |

## Security

- All API endpoints require authentication (except `/api/health`)
- Role-based access control (Admin, Operator, Viewer)
- Credentials stored in Azure Key Vault, referenced by name -- never stored in DB or logged
- IP-based rate limiting (200 requests/minute)
- Global exception handler prevents internal detail leakage
- SFTP credentials never appear in logs or API responses
- CORS restricted to configured origins

## Architecture

See [Docs/architecture.md](Docs/architecture.md) for the full architecture overview.

## Documentation

- [PRD](Docs/PRD_API_to_SFTP_Bridge.md) -- Product Requirements Document
- [CSV Schema](Docs/csv-schema.md) -- Obi CSV output specification
- [Database Schema](Docs/schema-overview.md) -- Entity relationship documentation
- [Environment Variables](Docs/env-vars.md) -- Required configuration
- [Architecture](Docs/architecture.md) -- System design overview

## Claude Code Infrastructure

This project uses Claude Code with a full harness for AI-assisted development:

- **10 skills** in `.claude/skills/` -- `/new-endpoint`, `/new-component`, `/new-migration`, `/new-service`, `/run-tests`, `/code-review`, `/deploy-check`, `/security-audit`, `/new-integration-test`, `/csv-validate`
- **5 rules** in `.claude/rules/` -- path-scoped instructions for controllers, Vue components, migrations, services, SFTP code
- **3 agents** in `.claude/agents/` -- code-reviewer, test-writer, security-auditor
- **Hooks** in `.claude/settings.json` -- auto build checks, migration warnings, secret access blocking
