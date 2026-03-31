# Required Environment Variables

This document lists all environment variables required to run the Obi Bridge platform.

## Azure

| Variable | Description | Required |
|----------|-------------|----------|
| `AzureAd__TenantId` | Azure AD tenant ID | Yes |
| `AzureAd__ClientId` | Azure AD application client ID | Yes |
| `AzureAd__ClientSecret` | Azure AD application client secret | Yes |
| `KeyVault__VaultUri` | Azure Key Vault URI | Yes |
| `BlobStorage__ConnectionString` | Azure Blob Storage connection string | Yes |

## Database

| Variable | Description | Required |
|----------|-------------|----------|
| `ConnectionStrings__DefaultConnection` | SQL Server connection string | Yes |

## SFTP

| Variable | Description | Required |
|----------|-------------|----------|
| `Sftp__Host` | Obi SFTP server hostname | Yes |
| `Sftp__Port` | SFTP port (default: 22) | No |
| `Sftp__Username` | SFTP username | Yes |
| `Sftp__Password` | SFTP password (or use key-based auth) | Conditional |
| `Sftp__PrivateKeyPath` | Path to SSH private key file | Conditional |

## Application

| Variable | Description | Required |
|----------|-------------|----------|
| `Hangfire__DashboardEnabled` | Enable Hangfire dashboard (true/false) | No |
| `Logging__LogLevel__Default` | Default log level | No |

_Note: All secrets should be stored in Azure Key Vault, not as environment variables in production._
