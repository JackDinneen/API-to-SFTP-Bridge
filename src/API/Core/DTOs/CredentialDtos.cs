namespace API.Core.DTOs;

using API.Core.Models;

public class StoreCredentialRequest
{
    public Guid ConnectionId { get; set; }
    public AuthType CredentialType { get; set; }
    public string Label { get; set; } = string.Empty;
    public string SecretValue { get; set; } = string.Empty;
}

public class CredentialReference
{
    public Guid Id { get; set; }
    public Guid ConnectionId { get; set; }
    public AuthType CredentialType { get; set; }
    public string Label { get; set; } = string.Empty;
    public string KeyVaultSecretName { get; set; } = string.Empty;
}

public class AuthHeaders
{
    public Dictionary<string, string> Headers { get; set; } = new();
}
