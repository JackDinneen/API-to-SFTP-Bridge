using API.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace API.Infrastructure;

/// <summary>
/// Development scheduler that executes syncs inline (no Hangfire needed).
/// Scheduling is a no-op since there's no persistent job store.
/// </summary>
public class DevSchedulerService : ISchedulerService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DevSchedulerService> _logger;

    public DevSchedulerService(IServiceProvider serviceProvider, ILogger<DevSchedulerService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public Task ScheduleConnectionAsync(Guid connectionId, string cronExpression, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[Dev] Would schedule sync for connection {ConnectionId} with cron {Cron}", connectionId, cronExpression);
        return Task.CompletedTask;
    }

    public Task UnscheduleConnectionAsync(Guid connectionId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[Dev] Would unschedule sync for connection {ConnectionId}", connectionId);
        return Task.CompletedTask;
    }

    public async Task<string> TriggerManualSyncAsync(Guid connectionId, string triggeredBy, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[Dev] Executing sync inline for connection {ConnectionId} by {TriggeredBy}", connectionId, triggeredBy);

        // Execute sync inline using a new scope (since orchestrator is scoped)
        using var scope = _serviceProvider.CreateScope();
        var orchestrator = scope.ServiceProvider.GetRequiredService<ISyncOrchestratorService>();
        var result = await orchestrator.ExecuteSyncAsync(connectionId, triggeredBy, cancellationToken);

        if (result.Success)
        {
            _logger.LogInformation("[Dev] Sync succeeded for connection {ConnectionId}: {RecordCount} records, file {FileName}",
                connectionId, result.RecordCount, result.FileName);
        }
        else
        {
            _logger.LogWarning("[Dev] Sync failed for connection {ConnectionId} at step {Step}: {Error}",
                connectionId, result.ErrorStep, result.ErrorMessage);
        }

        return result.SyncRunId.ToString();
    }
}
