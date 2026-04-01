namespace API.Core.Interfaces;

using Azure;
using Azure.Security.KeyVault.Secrets;

/// <summary>
/// Wrapper interface for Azure Key Vault SecretClient to enable easy unit testing.
/// </summary>
public interface IKeyVaultClient
{
    Task<Response<KeyVaultSecret>> GetSecretAsync(string name, CancellationToken cancellationToken = default);
    Task<KeyVaultSecret> SetSecretAsync(string name, string value, CancellationToken cancellationToken = default);
    Task<DeleteSecretOperation> StartDeleteSecretAsync(string name, CancellationToken cancellationToken = default);
}
