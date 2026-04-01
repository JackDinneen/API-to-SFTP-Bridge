namespace UnitTests.Infrastructure.Repositories;

using API.Core.Entities;
using API.Core.Models;
using API.Infrastructure.Data;
using API.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

public class ConnectionCredentialRepositoryTests
{
    private ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private async Task<(Guid userId, Guid connectionId)> SeedConnectionAsync(ApplicationDbContext context)
    {
        var user = new UserProfile
        {
            Id = Guid.NewGuid(),
            AzureAdId = "azure-" + Guid.NewGuid(),
            Email = $"test-{Guid.NewGuid()}@example.com",
            DisplayName = "Test User",
            Role = UserRole.Admin
        };
        context.UserProfiles.Add(user);

        var connection = new Connection
        {
            Id = Guid.NewGuid(),
            Name = "Test Connection",
            BaseUrl = "https://api.example.com",
            AuthType = AuthType.ApiKey,
            ClientName = "test-client",
            PlatformName = "test-platform",
            CreatedById = user.Id
        };
        context.Connections.Add(connection);
        await context.SaveChangesAsync();

        return (user.Id, connection.Id);
    }

    // ---------------------------------------------------------------
    // CreateAsync
    // ---------------------------------------------------------------

    [Fact]
    public async Task CreateAsync_StoresCredentialAndReturnsWithId()
    {
        await using var context = CreateInMemoryContext();
        var (_, connectionId) = await SeedConnectionAsync(context);
        var repo = new ConnectionCredentialRepository(context);

        var credential = new ConnectionCredential
        {
            ConnectionId = connectionId,
            CredentialType = AuthType.ApiKey,
            KeyVaultSecretName = $"obi-bridge-{connectionId}-api-key",
            Label = "api-key"
        };

        var result = await repo.CreateAsync(credential);

        result.Id.Should().NotBeEmpty();
        result.ConnectionId.Should().Be(connectionId);
        result.KeyVaultSecretName.Should().Contain("obi-bridge");
        result.Label.Should().Be("api-key");

        // Verify persisted
        var stored = await context.ConnectionCredentials.FindAsync(result.Id);
        stored.Should().NotBeNull();
    }

    // ---------------------------------------------------------------
    // GetByConnectionIdAsync
    // ---------------------------------------------------------------

    [Fact]
    public async Task GetByConnectionIdAsync_ReturnsOnlyCredentialsForGivenConnection()
    {
        await using var context = CreateInMemoryContext();
        var (userId, connectionId1) = await SeedConnectionAsync(context);

        // Create a second connection
        var connection2 = new Connection
        {
            Id = Guid.NewGuid(),
            Name = "Second Connection",
            BaseUrl = "https://api2.example.com",
            AuthType = AuthType.BasicAuth,
            ClientName = "client2",
            PlatformName = "platform2",
            CreatedById = userId
        };
        context.Connections.Add(connection2);
        await context.SaveChangesAsync();

        var repo = new ConnectionCredentialRepository(context);

        // Add credentials to both connections
        await repo.CreateAsync(new ConnectionCredential
        {
            ConnectionId = connectionId1,
            CredentialType = AuthType.ApiKey,
            KeyVaultSecretName = $"obi-bridge-{connectionId1}-api-key",
            Label = "api-key"
        });

        await repo.CreateAsync(new ConnectionCredential
        {
            ConnectionId = connectionId1,
            CredentialType = AuthType.ApiKey,
            KeyVaultSecretName = $"obi-bridge-{connectionId1}-secret",
            Label = "secret"
        });

        await repo.CreateAsync(new ConnectionCredential
        {
            ConnectionId = connection2.Id,
            CredentialType = AuthType.BasicAuth,
            KeyVaultSecretName = $"obi-bridge-{connection2.Id}-username",
            Label = "username"
        });

        var results = await repo.GetByConnectionIdAsync(connectionId1);

        results.Should().HaveCount(2);
        results.Should().OnlyContain(c => c.ConnectionId == connectionId1);
    }

    // ---------------------------------------------------------------
    // GetByIdAsync
    // ---------------------------------------------------------------

    [Fact]
    public async Task GetByIdAsync_ReturnsCredentialWhenFound()
    {
        await using var context = CreateInMemoryContext();
        var (_, connectionId) = await SeedConnectionAsync(context);
        var repo = new ConnectionCredentialRepository(context);

        var credential = await repo.CreateAsync(new ConnectionCredential
        {
            ConnectionId = connectionId,
            CredentialType = AuthType.ApiKey,
            KeyVaultSecretName = $"obi-bridge-{connectionId}-api-key",
            Label = "api-key"
        });

        var result = await repo.GetByIdAsync(credential.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(credential.Id);
        result.Label.Should().Be("api-key");
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNullWhenNotFound()
    {
        await using var context = CreateInMemoryContext();
        var repo = new ConnectionCredentialRepository(context);

        var result = await repo.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    // ---------------------------------------------------------------
    // DeleteAsync (soft delete)
    // ---------------------------------------------------------------

    [Fact]
    public async Task DeleteAsync_SoftDeletesCredential()
    {
        await using var context = CreateInMemoryContext();
        var (_, connectionId) = await SeedConnectionAsync(context);
        var repo = new ConnectionCredentialRepository(context);

        var credential = await repo.CreateAsync(new ConnectionCredential
        {
            ConnectionId = connectionId,
            CredentialType = AuthType.ApiKey,
            KeyVaultSecretName = $"obi-bridge-{connectionId}-api-key",
            Label = "api-key"
        });

        await repo.DeleteAsync(credential.Id);

        // Soft-deleted: the global query filter should exclude it
        var fromQuery = await repo.GetByConnectionIdAsync(connectionId);
        fromQuery.Should().BeEmpty();

        // But the record still exists in the database (ignoring query filters)
        var raw = await context.ConnectionCredentials
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == credential.Id);
        raw.Should().NotBeNull();
        raw!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_NonExistentId_DoesNotThrow()
    {
        await using var context = CreateInMemoryContext();
        var repo = new ConnectionCredentialRepository(context);

        var act = () => repo.DeleteAsync(Guid.NewGuid());

        await act.Should().NotThrowAsync();
    }
}
