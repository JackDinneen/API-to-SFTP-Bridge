namespace UnitTests.Application.Services;

using System.Text;
using API.Application.Services;
using API.Core.DTOs;
using API.Core.Entities;
using API.Core.Interfaces;
using API.Core.Models;
using Azure;
using Azure.Security.KeyVault.Secrets;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

public class CredentialVaultServiceTests
{
    private readonly Mock<IKeyVaultClient> _keyVaultMock;
    private readonly Mock<IConnectionCredentialRepository> _repoMock;
    private readonly Mock<ILogger<CredentialVaultService>> _loggerMock;
    private readonly CredentialVaultService _service;

    public CredentialVaultServiceTests()
    {
        _keyVaultMock = new Mock<IKeyVaultClient>();
        _repoMock = new Mock<IConnectionCredentialRepository>();
        _loggerMock = new Mock<ILogger<CredentialVaultService>>();
        _service = new CredentialVaultService(_keyVaultMock.Object, _repoMock.Object, _loggerMock.Object);
    }

    private void SetupGetSecret(string secretName, string secretValue)
    {
        var kvSecret = new KeyVaultSecret(secretName, secretValue);
        var responseMock = new Mock<Response<KeyVaultSecret>>();
        responseMock.Setup(r => r.Value).Returns(kvSecret);

        _keyVaultMock.Setup(c => c.GetSecretAsync(secretName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseMock.Object);
    }

    private void SetupGetSecretNotFound(string secretName)
    {
        _keyVaultMock.Setup(c => c.GetSecretAsync(secretName, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RequestFailedException(404, "Secret not found"));
    }

    // ---------------------------------------------------------------
    // StoreCredentialAsync
    // ---------------------------------------------------------------

    [Fact]
    public async Task StoreCredentialAsync_GeneratesCorrectSecretNameFormat()
    {
        var connectionId = Guid.NewGuid();
        var request = new StoreCredentialRequest
        {
            ConnectionId = connectionId,
            CredentialType = AuthType.ApiKey,
            Label = "api-key",
            SecretValue = "secret-value-123"
        };

        _repoMock.Setup(r => r.CreateAsync(It.IsAny<ConnectionCredential>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConnectionCredential c, CancellationToken _) => c);

        var result = await _service.StoreCredentialAsync(request);

        var expectedSecretName = $"obi-bridge-{connectionId}-api-key";
        result.KeyVaultSecretName.Should().Be(expectedSecretName);

        _keyVaultMock.Verify(c => c.SetSecretAsync(
            expectedSecretName,
            "secret-value-123",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StoreCredentialAsync_SanitizesLabelInSecretName()
    {
        var connectionId = Guid.NewGuid();
        var request = new StoreCredentialRequest
        {
            ConnectionId = connectionId,
            CredentialType = AuthType.BasicAuth,
            Label = "Client Secret",
            SecretValue = "value"
        };

        _repoMock.Setup(r => r.CreateAsync(It.IsAny<ConnectionCredential>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConnectionCredential c, CancellationToken _) => c);

        var result = await _service.StoreCredentialAsync(request);

        result.KeyVaultSecretName.Should().Be($"obi-bridge-{connectionId}-client-secret");
    }

    // ---------------------------------------------------------------
    // GetCredentialAsync
    // ---------------------------------------------------------------

    [Fact]
    public async Task GetCredentialAsync_ReturnsValueFromKeyVault()
    {
        var connectionId = Guid.NewGuid();
        var secretName = $"obi-bridge-{connectionId}-api-key";
        var secretValue = "my-secret-value";

        SetupGetSecret(secretName, secretValue);

        var result = await _service.GetCredentialAsync(connectionId, "api-key");

        result.Should().Be(secretValue);
    }

    [Fact]
    public async Task GetCredentialAsync_ReturnsNullWhenSecretNotFound()
    {
        var connectionId = Guid.NewGuid();
        var secretName = $"obi-bridge-{connectionId}-api-key";

        SetupGetSecretNotFound(secretName);

        var result = await _service.GetCredentialAsync(connectionId, "api-key");

        result.Should().BeNull();
    }

    // ---------------------------------------------------------------
    // DeleteCredentialAsync
    // ---------------------------------------------------------------

    [Fact]
    public async Task DeleteCredentialAsync_CallsKeyVaultDelete()
    {
        var connectionId = Guid.NewGuid();
        var credentialId = Guid.NewGuid();
        var secretName = $"obi-bridge-{connectionId}-api-key";

        var credential = new ConnectionCredential
        {
            Id = credentialId,
            ConnectionId = connectionId,
            Label = "api-key",
            KeyVaultSecretName = secretName,
            CredentialType = AuthType.ApiKey
        };

        _repoMock.Setup(r => r.GetByConnectionIdAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ConnectionCredential> { credential });

        var deleteOpMock = new Mock<DeleteSecretOperation>();
        _keyVaultMock.Setup(c => c.StartDeleteSecretAsync(secretName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deleteOpMock.Object);

        await _service.DeleteCredentialAsync(connectionId, "api-key");

        _keyVaultMock.Verify(c => c.StartDeleteSecretAsync(secretName, It.IsAny<CancellationToken>()), Times.Once);
        _repoMock.Verify(r => r.DeleteAsync(credentialId, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---------------------------------------------------------------
    // BuildAuthHeadersAsync - ApiKey
    // ---------------------------------------------------------------

    [Fact]
    public async Task BuildAuthHeadersAsync_ApiKey_ReturnsBearerHeader()
    {
        var connectionId = Guid.NewGuid();
        var apiKeyValue = "test-api-key-12345";

        SetupGetSecret($"obi-bridge-{connectionId}-api-key", apiKeyValue);

        var result = await _service.BuildAuthHeadersAsync(connectionId, AuthType.ApiKey);

        result.Headers.Should().ContainKey("Authorization");
        result.Headers["Authorization"].Should().Be($"Bearer {apiKeyValue}");
    }

    // ---------------------------------------------------------------
    // BuildAuthHeadersAsync - BasicAuth
    // ---------------------------------------------------------------

    [Fact]
    public async Task BuildAuthHeadersAsync_BasicAuth_ReturnsBase64EncodedHeader()
    {
        var connectionId = Guid.NewGuid();
        var username = "testuser";
        var password = "testpass";

        SetupGetSecret($"obi-bridge-{connectionId}-username", username);
        SetupGetSecret($"obi-bridge-{connectionId}-password", password);

        var result = await _service.BuildAuthHeadersAsync(connectionId, AuthType.BasicAuth);

        var expectedEncoded = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
        result.Headers.Should().ContainKey("Authorization");
        result.Headers["Authorization"].Should().Be($"Basic {expectedEncoded}");
    }

    // ---------------------------------------------------------------
    // BuildAuthHeadersAsync - CustomHeaders
    // ---------------------------------------------------------------

    [Fact]
    public async Task BuildAuthHeadersAsync_CustomHeaders_ReturnsCustomHeaderDict()
    {
        var connectionId = Guid.NewGuid();

        var credentials = new List<ConnectionCredential>
        {
            new ConnectionCredential
            {
                ConnectionId = connectionId,
                CredentialType = AuthType.CustomHeaders,
                Label = "X-Api-Key",
                KeyVaultSecretName = $"obi-bridge-{connectionId}-x-api-key"
            },
            new ConnectionCredential
            {
                ConnectionId = connectionId,
                CredentialType = AuthType.CustomHeaders,
                Label = "X-Secret",
                KeyVaultSecretName = $"obi-bridge-{connectionId}-x-secret"
            }
        };

        _repoMock.Setup(r => r.GetByConnectionIdAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(credentials);

        SetupGetSecret($"obi-bridge-{connectionId}-x-api-key", "key-value-1");
        SetupGetSecret($"obi-bridge-{connectionId}-x-secret", "secret-value-2");

        var result = await _service.BuildAuthHeadersAsync(connectionId, AuthType.CustomHeaders);

        result.Headers.Should().ContainKey("X-Api-Key");
        result.Headers["X-Api-Key"].Should().Be("key-value-1");
        result.Headers.Should().ContainKey("X-Secret");
        result.Headers["X-Secret"].Should().Be("secret-value-2");
    }

    // ---------------------------------------------------------------
    // Credential values never in log output
    // ---------------------------------------------------------------

    [Fact]
    public async Task StoreCredentialAsync_DoesNotLogCredentialValues()
    {
        var connectionId = Guid.NewGuid();
        var secretValue = "super-secret-credential-value-xyz";
        var request = new StoreCredentialRequest
        {
            ConnectionId = connectionId,
            CredentialType = AuthType.ApiKey,
            Label = "api-key",
            SecretValue = secretValue
        };

        _repoMock.Setup(r => r.CreateAsync(It.IsAny<ConnectionCredential>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConnectionCredential c, CancellationToken _) => c);

        await _service.StoreCredentialAsync(request);

        // Verify that no log call contains the secret value
        _loggerMock.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains(secretValue)),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    // ---------------------------------------------------------------
    // CredentialExistsAsync
    // ---------------------------------------------------------------

    [Fact]
    public async Task CredentialExistsAsync_ReturnsTrueWhenSecretExists()
    {
        var connectionId = Guid.NewGuid();
        SetupGetSecret($"obi-bridge-{connectionId}-api-key", "value");

        var result = await _service.CredentialExistsAsync(connectionId, "api-key");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CredentialExistsAsync_ReturnsFalseWhenSecretNotFound()
    {
        var connectionId = Guid.NewGuid();
        SetupGetSecretNotFound($"obi-bridge-{connectionId}-api-key");

        var result = await _service.CredentialExistsAsync(connectionId, "api-key");

        result.Should().BeFalse();
    }

    // ---------------------------------------------------------------
    // Secret name format verified via StoreCredentialAsync
    // ---------------------------------------------------------------

    [Fact]
    public async Task StoreCredentialAsync_SecretNameMatchesExpectedFormat()
    {
        var connectionId = Guid.Parse("12345678-1234-1234-1234-123456789abc");
        var request = new StoreCredentialRequest
        {
            ConnectionId = connectionId,
            CredentialType = AuthType.ApiKey,
            Label = "api-key",
            SecretValue = "value"
        };

        _repoMock.Setup(r => r.CreateAsync(It.IsAny<ConnectionCredential>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConnectionCredential c, CancellationToken _) => c);

        var result = await _service.StoreCredentialAsync(request);

        result.KeyVaultSecretName.Should().Be("obi-bridge-12345678-1234-1234-1234-123456789abc-api-key");
    }
}
