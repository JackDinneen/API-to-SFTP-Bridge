# Required Environment Variables

This document lists all environment variables required to run the Obi Bridge platform.

## Azure AD Authentication

| Variable | Description | Required |
|----------|-------------|----------|
| `AzureAd__TenantId` | Azure AD tenant ID | Yes (prod) |
| `AzureAd__ClientId` | Azure AD application client ID | Yes (prod) |
| `AzureAd__Instance` | Azure AD instance URL (e.g. `https://login.microsoftonline.com/`) | Yes (prod) |

When the `AzureAd` configuration section is absent, the API falls back to a development JWT bearer scheme. This fallback disables audience and issuer validation and should never be used in production.

## Azure Key Vault

| Variable | Description | Required |
|----------|-------------|----------|
| `KeyVault__VaultUri` | Azure Key Vault URI (e.g. `https://obi-bridge.vault.azure.net/`) | Yes |

All external API credentials and SFTP passwords are stored as Key Vault secrets. The app authenticates to Key Vault via `DefaultAzureCredential` (Managed Identity in Azure, CLI/VS credentials locally).

## Azure Blob Storage

| Variable | Description | Required |
|----------|-------------|----------|
| `BlobStorage__ConnectionString` | Azure Blob Storage connection string for CSV retention | Yes |

## Database

| Variable | Description | Required |
|----------|-------------|----------|
| `ConnectionStrings__DefaultConnection` | SQL Server connection string for EF Core | Yes |
| `ConnectionStrings__HangfireConnection` | SQL Server connection string for Hangfire job storage (can be the same as Default) | No (Hangfire commented out for now) |

## CORS

| Variable | Description | Required |
|----------|-------------|----------|
| `Cors__AllowedOrigins__0` | First allowed CORS origin (e.g. `https://app.obibridge.com`) | No |
| `Cors__AllowedOrigins__1` | Additional allowed origins (array indexed) | No |

When no origins are configured, the API defaults to `http://localhost:3000` (development only).

## Authentication Fallback (Development Only)

| Variable | Description | Required |
|----------|-------------|----------|
| `Auth__Authority` | JWT authority URL for development auth fallback | No |

## Logging

| Variable | Description | Required |
|----------|-------------|----------|
| `Logging__LogLevel__Default` | Default log level (`Information`, `Warning`, etc.) | No |
| `Logging__LogLevel__Microsoft.AspNetCore` | ASP.NET Core framework log level | No |

## Hangfire (when enabled)

| Variable | Description | Required |
|----------|-------------|----------|
| `Hangfire__DashboardEnabled` | Enable Hangfire dashboard (true/false) | No |

## Notes

- All secrets (API keys, SFTP passwords, OAuth client secrets) must be stored in Azure Key Vault, never as environment variables in production.
- Use `__` (double underscore) as section separator in environment variables, which maps to `:` in `appsettings.json`.
- In development, values can also be set in `appsettings.Development.json` or via .NET User Secrets.
