namespace UnitTests.Infrastructure.Repositories;

using API.Core.Entities;
using API.Core.Models;
using API.Infrastructure.Data;
using API.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

public class NotificationConfigRepositoryTests
{
    private ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private async Task<Guid> SeedConnectionAsync(ApplicationDbContext context)
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
        return connection.Id;
    }

    [Fact]
    public async Task GetByConnectionIdAsync_ReturnsConfigWhenExists()
    {
        await using var context = CreateInMemoryContext();
        var connectionId = await SeedConnectionAsync(context);
        var repo = new NotificationConfigRepository(context);

        context.NotificationConfigs.Add(new NotificationConfig
        {
            ConnectionId = connectionId,
            NotifyOnSuccess = true,
            EmailRecipients = "test@example.com"
        });
        await context.SaveChangesAsync();

        var result = await repo.GetByConnectionIdAsync(connectionId);

        result.Should().NotBeNull();
        result!.EmailRecipients.Should().Be("test@example.com");
    }

    [Fact]
    public async Task GetByConnectionIdAsync_ReturnsNullWhenNotExists()
    {
        await using var context = CreateInMemoryContext();
        var repo = new NotificationConfigRepository(context);

        var result = await repo.GetByConnectionIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateOrUpdateAsync_CreatesNewConfigWhenNoneExists()
    {
        await using var context = CreateInMemoryContext();
        var connectionId = await SeedConnectionAsync(context);
        var repo = new NotificationConfigRepository(context);

        var config = new NotificationConfig
        {
            ConnectionId = connectionId,
            NotifyOnSuccess = true,
            NotifyOnFailure = false,
            EmailRecipients = "admin@example.com"
        };

        var result = await repo.CreateOrUpdateAsync(config);

        result.Should().NotBeNull();
        result.EmailRecipients.Should().Be("admin@example.com");

        var stored = await context.NotificationConfigs.FirstOrDefaultAsync(n => n.ConnectionId == connectionId);
        stored.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateOrUpdateAsync_UpdatesExistingConfig()
    {
        await using var context = CreateInMemoryContext();
        var connectionId = await SeedConnectionAsync(context);
        var repo = new NotificationConfigRepository(context);

        // Create initial
        context.NotificationConfigs.Add(new NotificationConfig
        {
            ConnectionId = connectionId,
            NotifyOnSuccess = true,
            EmailRecipients = "old@example.com"
        });
        await context.SaveChangesAsync();

        // Update
        var updated = new NotificationConfig
        {
            ConnectionId = connectionId,
            NotifyOnSuccess = false,
            NotifyOnFailure = true,
            EmailRecipients = "new@example.com"
        };

        var result = await repo.CreateOrUpdateAsync(updated);

        result.NotifyOnSuccess.Should().BeFalse();
        result.EmailRecipients.Should().Be("new@example.com");

        var all = await context.NotificationConfigs
            .Where(n => n.ConnectionId == connectionId)
            .ToListAsync();
        all.Should().HaveCount(1);
    }
}
