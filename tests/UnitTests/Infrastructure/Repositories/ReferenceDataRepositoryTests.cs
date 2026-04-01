namespace UnitTests.Infrastructure.Repositories;

using API.Core.Entities;
using API.Core.Models;
using API.Infrastructure.Data;
using API.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

public class ReferenceDataRepositoryTests
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

    private ReferenceData MakeRefData(Guid userId, string assetId = "AST-001", string submeterCode = "SM-EL-001")
    {
        return new ReferenceData
        {
            AssetId = assetId,
            AssetName = "Tower A",
            SubmeterCode = submeterCode,
            UtilityType = "Electricity",
            UploadedById = userId
        };
    }

    // ---------------------------------------------------------------
    // CreateAsync
    // ---------------------------------------------------------------

    [Fact]
    public async Task CreateAsync_StoresReferenceData()
    {
        await using var context = CreateInMemoryContext();
        var userId = await SeedUserAsync(context);
        var repo = new ReferenceDataRepository(context);

        var item = MakeRefData(userId);
        var result = await repo.CreateAsync(item);

        result.Id.Should().NotBeEmpty();
        result.AssetId.Should().Be("AST-001");

        var stored = await context.ReferenceData.FindAsync(result.Id);
        stored.Should().NotBeNull();
    }

    // ---------------------------------------------------------------
    // GetByIdAsync
    // ---------------------------------------------------------------

    [Fact]
    public async Task GetByIdAsync_ReturnsItemWhenFound()
    {
        await using var context = CreateInMemoryContext();
        var userId = await SeedUserAsync(context);
        var repo = new ReferenceDataRepository(context);

        var item = MakeRefData(userId);
        await repo.CreateAsync(item);

        var result = await repo.GetByIdAsync(item.Id);

        result.Should().NotBeNull();
        result!.AssetId.Should().Be("AST-001");
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNullWhenNotFound()
    {
        await using var context = CreateInMemoryContext();
        var repo = new ReferenceDataRepository(context);

        var result = await repo.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    // ---------------------------------------------------------------
    // GetAllAsync
    // ---------------------------------------------------------------

    [Fact]
    public async Task GetAllAsync_ReturnsAllItems()
    {
        await using var context = CreateInMemoryContext();
        var userId = await SeedUserAsync(context);
        var repo = new ReferenceDataRepository(context);

        await repo.CreateAsync(MakeRefData(userId, "AST-001"));
        await repo.CreateAsync(MakeRefData(userId, "AST-002"));

        var result = await repo.GetAllAsync();

        result.Should().HaveCount(2);
    }

    // ---------------------------------------------------------------
    // GetByAssetIdAsync
    // ---------------------------------------------------------------

    [Fact]
    public async Task GetByAssetIdAsync_ReturnsMatchingItems()
    {
        await using var context = CreateInMemoryContext();
        var userId = await SeedUserAsync(context);
        var repo = new ReferenceDataRepository(context);

        await repo.CreateAsync(MakeRefData(userId, "AST-001", "SM-EL-001"));
        await repo.CreateAsync(MakeRefData(userId, "AST-001", "SM-GS-001"));
        await repo.CreateAsync(MakeRefData(userId, "AST-002", "SM-EL-002"));

        var result = await repo.GetByAssetIdAsync("AST-001");

        result.Should().HaveCount(2);
        result.Should().OnlyContain(r => r.AssetId == "AST-001");
    }

    // ---------------------------------------------------------------
    // BulkImportAsync
    // ---------------------------------------------------------------

    [Fact]
    public async Task BulkImportAsync_ImportsMultipleItems()
    {
        await using var context = CreateInMemoryContext();
        var userId = await SeedUserAsync(context);
        var repo = new ReferenceDataRepository(context);

        var items = new List<ReferenceData>
        {
            MakeRefData(userId, "AST-001"),
            MakeRefData(userId, "AST-002"),
            MakeRefData(userId, "AST-003")
        };

        await repo.BulkImportAsync(items);

        var all = await repo.GetAllAsync();
        all.Should().HaveCount(3);
    }

    // ---------------------------------------------------------------
    // DeleteAsync (soft delete)
    // ---------------------------------------------------------------

    [Fact]
    public async Task DeleteAsync_SoftDeletesItem()
    {
        await using var context = CreateInMemoryContext();
        var userId = await SeedUserAsync(context);
        var repo = new ReferenceDataRepository(context);

        var item = MakeRefData(userId);
        await repo.CreateAsync(item);

        await repo.DeleteAsync(item.Id);

        var fromQuery = await repo.GetByIdAsync(item.Id);
        fromQuery.Should().BeNull();

        var raw = await context.ReferenceData
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.Id == item.Id);
        raw.Should().NotBeNull();
        raw!.IsDeleted.Should().BeTrue();
    }
}
