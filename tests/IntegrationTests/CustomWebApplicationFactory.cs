using API.Core.Interfaces;
using API.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // ---------------------------------------------------------------
            // Replace SQL Server with InMemory database
            // Remove ALL DbContext-related descriptors to avoid dual-provider error
            // ---------------------------------------------------------------
            var dbDescriptors = services
                .Where(d => d.ServiceType.FullName?.Contains("EntityFrameworkCore") == true
                         || d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>)
                         || d.ServiceType == typeof(DbContextOptions)
                         || d.ImplementationType?.FullName?.Contains("SqlServer") == true)
                .ToList();
            foreach (var d in dbDescriptors)
            {
                services.Remove(d);
            }

            services.RemoveAll<ApplicationDbContext>();

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}"));

            // ---------------------------------------------------------------
            // Replace Azure AD / JWT auth with test auth handler
            // ---------------------------------------------------------------
            services.RemoveAll<Microsoft.AspNetCore.Authentication.IAuthenticationService>();
            services.RemoveAll<Microsoft.AspNetCore.Authentication.IAuthenticationHandlerProvider>();
            services.RemoveAll<Microsoft.AspNetCore.Authentication.IAuthenticationSchemeProvider>();

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                options.DefaultScheme = TestAuthHandler.SchemeName;
            })
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                TestAuthHandler.SchemeName, _ => { });

            // ---------------------------------------------------------------
            // Register test doubles for services not yet wired up
            // ---------------------------------------------------------------
            services.RemoveAll<IConnectionRepository>();
            services.RemoveAll<IConnectionCredentialRepository>();
            services.RemoveAll<ISyncRunRepository>();
            services.RemoveAll<ISchedulerService>();
            services.RemoveAll<IApiConnectorService>();
            services.RemoveAll<ICredentialVaultService>();
            services.RemoveAll<ISyncOrchestratorService>();
            services.RemoveAll<ISftpDeliveryService>();
            services.RemoveAll<ICsvGeneratorService>();
            services.RemoveAll<ITransformEngineService>();
            services.RemoveAll<IKeyVaultClient>();

            // Use InMemory-backed repository implementations that work with EF Core
            services.AddScoped<IConnectionRepository, InMemoryConnectionRepository>();
            services.AddScoped<ISyncRunRepository, InMemorySyncRunRepository>();

            // Mock services that depend on external systems
            var schedulerMock = new Mock<ISchedulerService>();
            schedulerMock.Setup(s => s.ScheduleConnectionAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            schedulerMock.Setup(s => s.UnscheduleConnectionAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            schedulerMock.Setup(s => s.TriggerManualSyncAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("job-id");
            services.AddSingleton(schedulerMock.Object);

            var apiConnectorMock = new Mock<IApiConnectorService>();
            apiConnectorMock.Setup(s => s.TestConnectionAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            services.AddSingleton(apiConnectorMock.Object);

            var credentialVaultMock = new Mock<ICredentialVaultService>();
            services.AddSingleton(credentialVaultMock.Object);
        });
    }
}

/// <summary>
/// Simple EF Core-backed connection repository for integration tests.
/// </summary>
internal class InMemoryConnectionRepository : IConnectionRepository
{
    private readonly ApplicationDbContext _db;

    public InMemoryConnectionRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<API.Core.Entities.Connection> CreateAsync(API.Core.Entities.Connection connection, CancellationToken cancellationToken = default)
    {
        _db.Connections.Add(connection);
        await _db.SaveChangesAsync(cancellationToken);
        return connection;
    }

    public async Task<API.Core.Entities.Connection?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.Connections.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<API.Core.Entities.Connection?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.Connections
            .Include(c => c.Credentials)
            .Include(c => c.Mappings)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<API.Core.Entities.Connection>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Connections.ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<API.Core.Entities.Connection>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Connections
            .Where(c => c.Status == API.Core.Models.ConnectionStatus.Active)
            .ToListAsync(cancellationToken);
    }

    public async Task<API.Core.Entities.Connection> UpdateAsync(API.Core.Entities.Connection connection, CancellationToken cancellationToken = default)
    {
        _db.Connections.Update(connection);
        await _db.SaveChangesAsync(cancellationToken);
        return connection;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Connections.FindAsync(new object[] { id }, cancellationToken);
        if (entity != null)
        {
            entity.IsDeleted = true;
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}

/// <summary>
/// Simple EF Core-backed sync run repository for integration tests.
/// </summary>
internal class InMemorySyncRunRepository : ISyncRunRepository
{
    private readonly ApplicationDbContext _db;

    public InMemorySyncRunRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<API.Core.Entities.SyncRun> CreateAsync(API.Core.Entities.SyncRun syncRun, CancellationToken cancellationToken = default)
    {
        _db.SyncRuns.Add(syncRun);
        await _db.SaveChangesAsync(cancellationToken);
        return syncRun;
    }

    public async Task<API.Core.Entities.SyncRun?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.SyncRuns.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<IReadOnlyList<API.Core.Entities.SyncRun>> GetByConnectionIdAsync(Guid connectionId, int limit = 50, CancellationToken cancellationToken = default)
    {
        return await _db.SyncRuns
            .Where(s => s.ConnectionId == connectionId)
            .OrderByDescending(s => s.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<API.Core.Entities.SyncRun?> GetByIdWithRecordsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.SyncRuns
            .Include(s => s.Records)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<API.Core.Entities.SyncRun> UpdateAsync(API.Core.Entities.SyncRun syncRun, CancellationToken cancellationToken = default)
    {
        _db.SyncRuns.Update(syncRun);
        await _db.SaveChangesAsync(cancellationToken);
        return syncRun;
    }

    public async Task<Dictionary<Guid, API.Core.Entities.SyncRun>> GetLatestByConnectionIdsAsync(IEnumerable<Guid> connectionIds, CancellationToken cancellationToken = default)
    {
        var ids = connectionIds.ToList();
        if (ids.Count == 0) return new Dictionary<Guid, API.Core.Entities.SyncRun>();

        var latestRuns = await _db.SyncRuns
            .Where(s => ids.Contains(s.ConnectionId))
            .GroupBy(s => s.ConnectionId)
            .Select(g => g.OrderByDescending(s => s.CreatedAt).First())
            .ToListAsync(cancellationToken);

        return latestRuns.ToDictionary(s => s.ConnectionId);
    }

    public async Task<decimal?> GetSuccessRateAsync(Guid connectionId, int recentCount = 10, CancellationToken cancellationToken = default)
    {
        var rates = await GetSuccessRatesByConnectionIdsAsync(new[] { connectionId }, recentCount, cancellationToken);
        return rates.GetValueOrDefault(connectionId);
    }

    public async Task<Dictionary<Guid, decimal?>> GetSuccessRatesByConnectionIdsAsync(IEnumerable<Guid> connectionIds, int recentCount = 10, CancellationToken cancellationToken = default)
    {
        var ids = connectionIds.ToList();
        if (ids.Count == 0) return new Dictionary<Guid, decimal?>();

        var recentRuns = await _db.SyncRuns
            .Where(s => ids.Contains(s.ConnectionId)
                && (s.Status == API.Core.Models.SyncRunStatus.Succeeded || s.Status == API.Core.Models.SyncRunStatus.Failed))
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);

        var result = new Dictionary<Guid, decimal?>();
        foreach (var id in ids)
        {
            var runsForConnection = recentRuns
                .Where(s => s.ConnectionId == id)
                .Take(recentCount)
                .ToList();

            if (runsForConnection.Count == 0)
            {
                result[id] = null;
            }
            else
            {
                var succeeded = runsForConnection.Count(s => s.Status == API.Core.Models.SyncRunStatus.Succeeded);
                result[id] = Math.Round((decimal)succeeded / runsForConnection.Count * 100, 1);
            }
        }

        return result;
    }
}
