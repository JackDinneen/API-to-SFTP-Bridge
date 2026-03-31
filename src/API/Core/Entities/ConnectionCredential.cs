namespace API.Core.Entities;

using API.Core.Models;

public class ConnectionCredential : BaseEntity
{
    public Guid ConnectionId { get; set; }
    public Connection Connection { get; set; } = null!;
    public AuthType CredentialType { get; set; }
    public string KeyVaultSecretName { get; set; } = string.Empty; // Reference to Azure Key Vault, NEVER the actual value
    public string? Label { get; set; } // e.g., "API Key", "Client ID", "Username"
}
