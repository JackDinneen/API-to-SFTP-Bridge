namespace API.Application.Services;

using System.Text;
using API.Core.DTOs;
using API.Core.Entities;
using API.Core.Interfaces;
using API.Core.Models;
using Azure;
using Microsoft.Extensions.Logging;

public class CredentialVaultService : ICredentialVaultService
{
    private readonly IKeyVaultClient _keyVaultClient;
    private readonly IConnectionCredentialRepository _credentialRepository;
    private readonly ILogger<CredentialVaultService> _logger;

    public CredentialVaultService(
        IKeyVaultClient keyVaultClient,
        IConnectionCredentialRepository credentialRepository,
        ILogger<CredentialVaultService> logger)
    {
        _keyVaultClient = keyVaultClient;
        _credentialRepository = credentialRepository;
        _logger = logger;
    }

    public async Task<CredentialReference> StoreCredentialAsync(StoreCredentialRequest request, CancellationToken cancellationToken = default)
    {
        var secretName = GenerateSecretName(request.ConnectionId, request.Label);
        _logger.LogInformation("Storing credential for connection {ConnectionId} with label {Label}", request.ConnectionId, request.Label);

        await _keyVaultClient.SetSecretAsync(secretName, request.SecretValue, cancellationToken);

        var credential = new ConnectionCredential
        {
            ConnectionId = request.ConnectionId,
            CredentialType = request.CredentialType,
            KeyVaultSecretName = secretName,
            Label = request.Label
        };

        var stored = await _credentialRepository.CreateAsync(credential, cancellationToken);

        return new CredentialReference
        {
            Id = stored.Id,
            ConnectionId = stored.ConnectionId,
            CredentialType = stored.CredentialType,
            Label = stored.Label ?? string.Empty,
            KeyVaultSecretName = stored.KeyVaultSecretName
        };
    }

    public async Task<string?> GetCredentialAsync(Guid connectionId, string label, CancellationToken cancellationToken = default)
    {
        var secretName = GenerateSecretName(connectionId, label);
        _logger.LogInformation("Retrieving credential for connection {ConnectionId} with label {Label}", connectionId, label);

        try
        {
            var secret = await _keyVaultClient.GetSecretAsync(secretName, cancellationToken);
            return secret.Value.Value;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogWarning("Credential not found for connection {ConnectionId} with label {Label}", connectionId, label);
            return null;
        }
    }

    public async Task DeleteCredentialAsync(Guid connectionId, string label, CancellationToken cancellationToken = default)
    {
        var secretName = GenerateSecretName(connectionId, label);
        _logger.LogInformation("Deleting credential for connection {ConnectionId} with label {Label}", connectionId, label);

        await _keyVaultClient.StartDeleteSecretAsync(secretName, cancellationToken);

        var credentials = await _credentialRepository.GetByConnectionIdAsync(connectionId, cancellationToken);
        var credential = credentials.FirstOrDefault(c => c.Label == label);
        if (credential != null)
        {
            await _credentialRepository.DeleteAsync(credential.Id, cancellationToken);
        }
    }

    public async Task<bool> CredentialExistsAsync(Guid connectionId, string label, CancellationToken cancellationToken = default)
    {
        var secretName = GenerateSecretName(connectionId, label);

        try
        {
            await _keyVaultClient.GetSecretAsync(secretName, cancellationToken);
            return true;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return false;
        }
    }

    public async Task<AuthHeaders> BuildAuthHeadersAsync(Guid connectionId, AuthType authType, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Building auth headers for connection {ConnectionId} with type {AuthType}", connectionId, authType);

        var headers = new Dictionary<string, string>();

        switch (authType)
        {
            case AuthType.ApiKey:
                var apiKey = await GetCredentialAsync(connectionId, "api-key", cancellationToken);
                var apiKeyHeader = await GetCredentialAsync(connectionId, "api-key-header", cancellationToken);
                if (apiKey != null)
                {
                    var headerName = !string.IsNullOrEmpty(apiKeyHeader) ? apiKeyHeader : "Authorization";
                    if (headerName.Equals("Authorization", StringComparison.OrdinalIgnoreCase))
                    {
                        headers["Authorization"] = $"Bearer {apiKey}";
                    }
                    else
                    {
                        headers[headerName] = apiKey;
                    }
                }
                break;

            case AuthType.BasicAuth:
                var username = await GetCredentialAsync(connectionId, "username", cancellationToken);
                var password = await GetCredentialAsync(connectionId, "password", cancellationToken);
                if (username != null && password != null)
                {
                    var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
                    headers["Authorization"] = $"Basic {encoded}";
                }
                break;

            case AuthType.CustomHeaders:
                var credentials = await _credentialRepository.GetByConnectionIdAsync(connectionId, cancellationToken);
                foreach (var cred in credentials.Where(c => c.CredentialType == AuthType.CustomHeaders))
                {
                    var value = await GetCredentialAsync(connectionId, cred.Label ?? string.Empty, cancellationToken);
                    if (value != null && cred.Label != null)
                    {
                        headers[cred.Label] = value;
                    }
                }
                break;
        }

        return new AuthHeaders { Headers = headers };
    }

    internal static string GenerateSecretName(Guid connectionId, string label)
    {
        var sanitizedLabel = label.ToLowerInvariant().Replace(" ", "-");
        return $"obi-bridge-{connectionId}-{sanitizedLabel}";
    }
}
