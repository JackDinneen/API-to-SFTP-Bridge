using API.Core.Interfaces;
using Azure;
using Azure.Security.KeyVault.Secrets;

namespace API.Infrastructure;

/// <summary>
/// In-memory Key Vault client for local development without Azure.
/// Stores secrets in a simple dictionary.
/// </summary>
public class DevKeyVaultClient : IKeyVaultClient
{
    private readonly Dictionary<string, string> _secrets = new();

    public Task<Response<KeyVaultSecret>> GetSecretAsync(string name, CancellationToken cancellationToken = default)
    {
        if (_secrets.TryGetValue(name, out var value))
        {
            var secret = new KeyVaultSecret(name, value);
            return Task.FromResult<Response<KeyVaultSecret>>(Response.FromValue(secret, null!));
        }
        throw new RequestFailedException(404, $"Secret '{name}' not found");
    }

    public Task<KeyVaultSecret> SetSecretAsync(string name, string value, CancellationToken cancellationToken = default)
    {
        _secrets[name] = value;
        return Task.FromResult(new KeyVaultSecret(name, value));
    }

    public Task<DeleteSecretOperation> StartDeleteSecretAsync(string name, CancellationToken cancellationToken = default)
    {
        _secrets.Remove(name);
        // Return a mock operation — in dev this is fire-and-forget
        return Task.FromResult<DeleteSecretOperation>(null!);
    }
}
