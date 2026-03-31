## Tech stack

Backend: C# .NET 8 Web API, clean architecture (Controllers > Services > Repos)
Database: SQL Server — EF Core for ORM, Dapper for complex reporting queries
Auth: Azure AD via MSAL — do not implement custom auth flows
Frontend: Vue 3 (Composition API, <script setup>) + Tailwind CSS
Storage: Azure Blob Storage for CSV retention and reference data uploads
Secrets: Azure Key Vault — never store credentials in appsettings or code
Scheduler: Hangfire with SQL Server storage for sync job scheduling
SFTP: Renci.SshNet for CSV delivery to Obi's SFTP server

## Domain context

ESG data integration platform (Obi Bridge). Core concepts:
- Connections: configured links between an external REST API and Obi's SFTP endpoint
- Mappings: field-level transformations from source API schema to Obi's 7-column CSV format (Asset ID, Asset name, Submeter Code, Utility Type, Year, Month, Value)
- Sync runs: scheduled or manual executions that fetch, transform, validate, and deliver data
- Reference data: Obi's known Asset IDs, Submeter Codes, and Utility Types for optional validation
- Audit logs: immutable record of every platform action

## Conventions

C#: PascalCase for classes/methods, _camelCase for private fields
All API responses use ApiResponse<T> wrapper in Core/Models/ApiResponse.cs
Validation uses FluentValidation — never inline in controllers
Vue components use <script setup> — not the Options API
All DB changes through EF Core migrations — never modify schema directly
Async all the way down — no .Result or .Wait() on async calls
All controller actions must have [Authorize] attribute

## Do not

Bypass the repository pattern — controllers must not access DbContext directly
Use synchronous DB calls — everything must be async/await
Add NuGet or npm packages without flagging it first
Write raw ADO.NET except in designated Dapper repositories
Hardcode connection strings, Azure keys, or any credentials
Modify EF Core migrations once applied to any environment
Store API keys, SFTP passwords, or OAuth tokens in appsettings files
Log sensitive credential values

## Current focus

[Update at session start — e.g.: "Working on Phase 2: Database & Auth. Phase 1 foundation is complete."]

## Key reference docs

PRD: @Docs/PRD_API_to_SFTP_Bridge.md
CSV schema: @docs/csv-schema.md
Database schema: @docs/schema-overview.md
