namespace API.Infrastructure;

using API.Core.Interfaces;
using Azure;
using Azure.Security.KeyVault.Secrets;

/// <summary>
/// Wraps Azure SecretClient behind IKeyVaultClient for testability.
/// </summary>
public class KeyVaultClientWrapper : IKeyVaultClient
{
    private readonly SecretClient _secretClient;

    public KeyVaultClientWrapper(SecretClient secretClient)
    {
        _secretClient = secretClient;
    }

    public async Task<Response<KeyVaultSecret>> GetSecretAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _secretClient.GetSecretAsync(name, cancellationToken: cancellationToken);
    }

    public async Task<KeyVaultSecret> SetSecretAsync(string name, string value, CancellationToken cancellationToken = default)
    {
        return await _secretClient.SetSecretAsync(name, value, cancellationToken);
    }

    public async Task<DeleteSecretOperation> StartDeleteSecretAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _secretClient.StartDeleteSecretAsync(name, cancellationToken);
    }
}
