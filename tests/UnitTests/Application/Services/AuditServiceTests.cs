namespace UnitTests.Application.Services;

using API.Application.Services;
using API.Core.Entities;
using API.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

public class AuditServiceTests
{
    private ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private AuditService CreateService(ApplicationDbContext context)
    {
        return new AuditService(context, Mock.Of<ILogger<AuditService>>());
    }

    // ---------------------------------------------------------------
    // LogAsync
    // ---------------------------------------------------------------

    [Fact]
    public async Task LogAsync_CreatesAuditLogEntry()
    {
        await using var context = CreateInMemoryContext();
        var service = CreateService(context);

        await service.LogAsync("TestAction", "TestEntity");

        var logs = await context.AuditLogs.ToListAsync();
        logs.Should().HaveCount(1);
    }

    [Fact]
    public async Task LogAsync_HasCorrectActionAndEntityType()
    {
        await using var context = CreateInMemoryContext();
        var service = CreateService(context);

        await service.LogAsync("ConnectionCreated", "Connection");

        var log = await context.AuditLogs.FirstAsync();
        log.Action.Should().Be("ConnectionCreated");
        log.EntityType.Should().Be("Connection");
    }

    [Fact]
    public async Task LogAsync_HasCorrectEntityIdAndUserId()
    {
        await using var context = CreateInMemoryContext();
        var service = CreateService(context);

        var entityId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await service.LogAsync("SyncStarted", "SyncRun", entityId, userId);

        var log = await context.AuditLogs.FirstAsync();
        log.EntityId.Should().Be(entityId);
        log.UserId.Should().Be(userId);
    }

    [Fact]
    public async Task LogAsync_HasDetailsJson()
    {
        await using var context = CreateInMemoryContext();
        var service = CreateService(context);

        var details = "{\"recordCount\": 42, \"fileName\": \"test.csv\"}";
        await service.LogAsync("SyncSucceeded", "SyncRun", Guid.NewGuid(), null, details);

        var log = await context.AuditLogs.FirstAsync();
        log.Details.Should().Be(details);
        log.Details.Should().Contain("recordCount");
    }

    [Fact]
    public async Task LogAsync_CreatedAtIsSetAutomatically()
    {
        await using var context = CreateInMemoryContext();
        var service = CreateService(context);

        var before = DateTime.UtcNow.AddSeconds(-1);
        await service.LogAsync("TestAction", "TestEntity");
        var after = DateTime.UtcNow.AddSeconds(1);

        var log = await context.AuditLogs.FirstAsync();
        log.CreatedAt.Should().BeAfter(before);
        log.CreatedAt.Should().BeBefore(after);
    }
}
