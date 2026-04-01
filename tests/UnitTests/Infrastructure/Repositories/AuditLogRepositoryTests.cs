namespace UnitTests.Infrastructure.Repositories;

using API.Core.Entities;
using API.Infrastructure.Data;
using API.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

public class AuditLogRepositoryTests
{
    private ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private async Task SeedLogsAsync(ApplicationDbContext context)
    {
        context.AuditLogs.AddRange(
            new AuditLog { Action = "Created", EntityType = "Connection", CreatedAt = new DateTime(2025, 1, 1) },
            new AuditLog { Action = "Synced", EntityType = "SyncRun", CreatedAt = new DateTime(2025, 2, 1) },
            new AuditLog { Action = "Deleted", EntityType = "Connection", CreatedAt = new DateTime(2025, 3, 1) },
            new AuditLog { Action = "Uploaded", EntityType = "ReferenceData", CreatedAt = new DateTime(2025, 4, 1) },
            new AuditLog { Action = "Synced", EntityType = "SyncRun", CreatedAt = new DateTime(2025, 5, 1) }
        );
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetFilteredAsync_NoFilters_ReturnsAll()
    {
        await using var context = CreateInMemoryContext();
        await SeedLogsAsync(context);
        var repo = new AuditLogRepository(context);

        var result = await repo.GetFilteredAsync();

        result.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetFilteredAsync_FilterByEntityType_ReturnsMatching()
    {
        await using var context = CreateInMemoryContext();
        await SeedLogsAsync(context);
        var repo = new AuditLogRepository(context);

        var result = await repo.GetFilteredAsync(entityType: "Connection");

        result.Should().HaveCount(2);
        result.Should().OnlyContain(l => l.EntityType == "Connection");
    }

    [Fact]
    public async Task GetFilteredAsync_FilterByDateRange_ReturnsMatching()
    {
        await using var context = CreateInMemoryContext();
        await SeedLogsAsync(context);
        var repo = new AuditLogRepository(context);

        var result = await repo.GetFilteredAsync(
            from: new DateTime(2025, 2, 1),
            to: new DateTime(2025, 4, 1));

        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetFilteredAsync_RespectsLimit()
    {
        await using var context = CreateInMemoryContext();
        await SeedLogsAsync(context);
        var repo = new AuditLogRepository(context);

        var result = await repo.GetFilteredAsync(limit: 2);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetFilteredAsync_OrdersByCreatedAtDescending()
    {
        await using var context = CreateInMemoryContext();
        await SeedLogsAsync(context);
        var repo = new AuditLogRepository(context);

        var result = await repo.GetFilteredAsync();

        result.Should().BeInDescendingOrder(l => l.CreatedAt);
    }

    [Fact]
    public async Task GetFilteredAsync_CombineFilters()
    {
        await using var context = CreateInMemoryContext();
        await SeedLogsAsync(context);
        var repo = new AuditLogRepository(context);

        var result = await repo.GetFilteredAsync(
            entityType: "SyncRun",
            from: new DateTime(2025, 1, 1),
            to: new DateTime(2025, 3, 1));

        result.Should().HaveCount(1);
        result[0].EntityType.Should().Be("SyncRun");
    }
}
