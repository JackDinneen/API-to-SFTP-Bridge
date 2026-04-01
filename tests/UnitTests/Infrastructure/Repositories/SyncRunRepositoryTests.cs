namespace UnitTests.Infrastructure.Repositories;

using API.Core.Entities;
using API.Core.Models;
using API.Infrastructure.Data;
using API.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

public class SyncRunRepositoryTests
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
    public async Task CreateAsync_StoresSyncRun()
    {
        await using var context = CreateInMemoryContext();
        var (_, connectionId) = await SeedConnectionAsync(context);
        var repo = new SyncRunRepository(context);

        var syncRun = new SyncRun
        {
            ConnectionId = connectionId,
            Status = SyncRunStatus.Pending,
            TriggeredBy = "test@example.com"
        };

        var result = await repo.CreateAsync(syncRun);

        result.Id.Should().NotBeEmpty();
        result.ConnectionId.Should().Be(connectionId);
        result.TriggeredBy.Should().Be("test@example.com");

        var stored = await context.SyncRuns.FindAsync(result.Id);
        stored.Should().NotBeNull();
    }

    // ---------------------------------------------------------------
    // GetByIdAsync
    // ---------------------------------------------------------------

    [Fact]
    public async Task GetByIdAsync_ReturnsSyncRun()
    {
        await using var context = CreateInMemoryContext();
        var (_, connectionId) = await SeedConnectionAsync(context);
        var repo = new SyncRunRepository(context);

        var syncRun = new SyncRun
        {
            ConnectionId = connectionId,
            Status = SyncRunStatus.Running,
            TriggeredBy = "scheduled"
        };
        await repo.CreateAsync(syncRun);

        var result = await repo.GetByIdAsync(syncRun.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(syncRun.Id);
        result.Status.Should().Be(SyncRunStatus.Running);
    }

    // ---------------------------------------------------------------
    // GetByConnectionIdAsync
    // ---------------------------------------------------------------

    [Fact]
    public async Task GetByConnectionIdAsync_ReturnsRunsForConnection_OrderedByCreatedAtDesc()
    {
        await using var context = CreateInMemoryContext();
        var (_, connectionId) = await SeedConnectionAsync(context);
        var repo = new SyncRunRepository(context);

        // Create runs with different timestamps
        var run1 = new SyncRun
        {
            ConnectionId = connectionId,
            Status = SyncRunStatus.Succeeded,
            TriggeredBy = "scheduled",
            RecordCount = 10
        };
        await repo.CreateAsync(run1);

        // Small delay to ensure different CreatedAt
        await Task.Delay(10);

        var run2 = new SyncRun
        {
            ConnectionId = connectionId,
            Status = SyncRunStatus.Failed,
            TriggeredBy = "manual@example.com",
            RecordCount = 0
        };
        await repo.CreateAsync(run2);

        await Task.Delay(10);

        var run3 = new SyncRun
        {
            ConnectionId = connectionId,
            Status = SyncRunStatus.Succeeded,
            TriggeredBy = "scheduled",
            RecordCount = 25
        };
        await repo.CreateAsync(run3);

        var result = await repo.GetByConnectionIdAsync(connectionId);

        result.Should().HaveCount(3);
        // Most recent first
        result[0].Id.Should().Be(run3.Id);
        result[1].Id.Should().Be(run2.Id);
        result[2].Id.Should().Be(run1.Id);
    }

    [Fact]
    public async Task GetByConnectionIdAsync_RespectsLimitParameter()
    {
        await using var context = CreateInMemoryContext();
        var (_, connectionId) = await SeedConnectionAsync(context);
        var repo = new SyncRunRepository(context);

        for (int i = 0; i < 5; i++)
        {
            await repo.CreateAsync(new SyncRun
            {
                ConnectionId = connectionId,
                Status = SyncRunStatus.Succeeded,
                TriggeredBy = "scheduled"
            });
            await Task.Delay(10);
        }

        var result = await repo.GetByConnectionIdAsync(connectionId, limit: 2);

        result.Should().HaveCount(2);
    }

    // ---------------------------------------------------------------
    // UpdateAsync
    // ---------------------------------------------------------------

    [Fact]
    public async Task UpdateAsync_UpdatesStatusAndCompletionTime()
    {
        await using var context = CreateInMemoryContext();
        var (_, connectionId) = await SeedConnectionAsync(context);
        var repo = new SyncRunRepository(context);

        var syncRun = new SyncRun
        {
            ConnectionId = connectionId,
            Status = SyncRunStatus.Running,
            TriggeredBy = "scheduled"
        };
        await repo.CreateAsync(syncRun);

        syncRun.Status = SyncRunStatus.Succeeded;
        syncRun.CompletedAt = DateTime.UtcNow;
        syncRun.RecordCount = 42;
        syncRun.FileName = "test_platform_31032026.csv";

        await repo.UpdateAsync(syncRun);

        var updated = await repo.GetByIdAsync(syncRun.Id);
        updated.Should().NotBeNull();
        updated!.Status.Should().Be(SyncRunStatus.Succeeded);
        updated.CompletedAt.Should().NotBeNull();
        updated.RecordCount.Should().Be(42);
        updated.FileName.Should().Be("test_platform_31032026.csv");
    }
}
