namespace UnitTests.Infrastructure.Repositories;

using API.Core.Entities;
using API.Core.Models;
using API.Infrastructure.Data;
using API.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

public class ConnectionRepositoryTests
{
    private ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private async Task<Guid> SeedUserAsync(ApplicationDbContext context)
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
        await context.SaveChangesAsync();
        return user.Id;
    }

    private Connection CreateConnection(Guid userId, ConnectionStatus status = ConnectionStatus.Active, string name = "Test Connection")
    {
        return new Connection
        {
            Id = Guid.NewGuid(),
            Name = name,
            BaseUrl = "https://api.example.com",
            AuthType = AuthType.ApiKey,
            Status = status,
            ClientName = "test-client",
            PlatformName = "test-platform",
            CreatedById = userId
        };
    }

    // ---------------------------------------------------------------
    // CreateAsync
    // ---------------------------------------------------------------

    [Fact]
    public async Task CreateAsync_StoresConnectionWithId()
    {
        await using var context = CreateInMemoryContext();
        var userId = await SeedUserAsync(context);
        var repo = new ConnectionRepository(context);

        var connection = CreateConnection(userId);
        var result = await repo.CreateAsync(connection);

        result.Id.Should().NotBeEmpty();
        result.Name.Should().Be("Test Connection");

        var stored = await context.Connections.FindAsync(result.Id);
        stored.Should().NotBeNull();
    }

    // ---------------------------------------------------------------
    // GetByIdAsync
    // ---------------------------------------------------------------

    [Fact]
    public async Task GetByIdAsync_ReturnsConnectionWhenFound()
    {
        await using var context = CreateInMemoryContext();
        var userId = await SeedUserAsync(context);
        var repo = new ConnectionRepository(context);

        var connection = CreateConnection(userId);
        await repo.CreateAsync(connection);

        var result = await repo.GetByIdAsync(connection.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(connection.Id);
        result.Name.Should().Be("Test Connection");
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNullWhenNotFound()
    {
        await using var context = CreateInMemoryContext();
        var repo = new ConnectionRepository(context);

        var result = await repo.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    // ---------------------------------------------------------------
    // GetByIdWithDetailsAsync
    // ---------------------------------------------------------------

    [Fact]
    public async Task GetByIdWithDetailsAsync_IncludesMappingsAndCredentials()
    {
        await using var context = CreateInMemoryContext();
        var userId = await SeedUserAsync(context);
        var repo = new ConnectionRepository(context);

        var connection = CreateConnection(userId);
        await repo.CreateAsync(connection);

        // Add mapping
        context.ConnectionMappings.Add(new ConnectionMapping
        {
            ConnectionId = connection.Id,
            SourcePath = "data.value",
            TargetColumn = "Value",
            TransformType = TransformType.DirectMapping,
            SortOrder = 1
        });

        // Add credential
        context.ConnectionCredentials.Add(new ConnectionCredential
        {
            ConnectionId = connection.Id,
            CredentialType = AuthType.ApiKey,
            KeyVaultSecretName = "secret-name",
            Label = "api-key"
        });

        await context.SaveChangesAsync();

        var result = await repo.GetByIdWithDetailsAsync(connection.Id);

        result.Should().NotBeNull();
        result!.Mappings.Should().HaveCount(1);
        result.Mappings.First().SourcePath.Should().Be("data.value");
        result.Credentials.Should().HaveCount(1);
        result.Credentials.First().Label.Should().Be("api-key");
    }

    // ---------------------------------------------------------------
    // GetAllAsync
    // ---------------------------------------------------------------

    [Fact]
    public async Task GetAllAsync_ReturnsAllNonDeletedConnections()
    {
        await using var context = CreateInMemoryContext();
        var userId = await SeedUserAsync(context);
        var repo = new ConnectionRepository(context);

        await repo.CreateAsync(CreateConnection(userId, name: "Conn 1"));
        await repo.CreateAsync(CreateConnection(userId, name: "Conn 2"));
        await repo.CreateAsync(CreateConnection(userId, name: "Conn 3"));

        var result = await repo.GetAllAsync();

        result.Should().HaveCount(3);
    }

    // ---------------------------------------------------------------
    // GetActiveAsync
    // ---------------------------------------------------------------

    [Fact]
    public async Task GetActiveAsync_ReturnsOnlyActiveConnections()
    {
        await using var context = CreateInMemoryContext();
        var userId = await SeedUserAsync(context);
        var repo = new ConnectionRepository(context);

        await repo.CreateAsync(CreateConnection(userId, ConnectionStatus.Active, "Active 1"));
        await repo.CreateAsync(CreateConnection(userId, ConnectionStatus.Active, "Active 2"));
        await repo.CreateAsync(CreateConnection(userId, ConnectionStatus.Paused, "Paused"));
        await repo.CreateAsync(CreateConnection(userId, ConnectionStatus.Error, "Error"));

        var result = await repo.GetActiveAsync();

        result.Should().HaveCount(2);
        result.Should().OnlyContain(c => c.Status == ConnectionStatus.Active);
    }

    // ---------------------------------------------------------------
    // DeleteAsync (soft delete)
    // ---------------------------------------------------------------

    [Fact]
    public async Task DeleteAsync_SoftDeletesSetsIsDeletedTrue()
    {
        await using var context = CreateInMemoryContext();
        var userId = await SeedUserAsync(context);
        var repo = new ConnectionRepository(context);

        var connection = CreateConnection(userId);
        await repo.CreateAsync(connection);

        await repo.DeleteAsync(connection.Id);

        // Should not appear via normal query (query filter)
        var fromQuery = await repo.GetByIdAsync(connection.Id);
        fromQuery.Should().BeNull();

        // But still exists in database
        var raw = await context.Connections
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == connection.Id);
        raw.Should().NotBeNull();
        raw!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task SoftDeletedConnections_NotReturnedByGetAllAsync()
    {
        await using var context = CreateInMemoryContext();
        var userId = await SeedUserAsync(context);
        var repo = new ConnectionRepository(context);

        var conn1 = CreateConnection(userId, name: "Keep");
        var conn2 = CreateConnection(userId, name: "Delete");
        await repo.CreateAsync(conn1);
        await repo.CreateAsync(conn2);

        await repo.DeleteAsync(conn2.Id);

        var result = await repo.GetAllAsync();

        result.Should().HaveCount(1);
        result.First().Name.Should().Be("Keep");
    }
}
