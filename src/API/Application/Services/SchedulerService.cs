namespace API.Application.Services;

using API.Core.Interfaces;
using Hangfire;
using Microsoft.Extensions.Logging;

public class SchedulerService : ISchedulerService
{
    private readonly IRecurringJobManager _recurringJobManager;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ILogger<SchedulerService> _logger;

    public SchedulerService(
        IRecurringJobManager recurringJobManager,
        IBackgroundJobClient backgroundJobClient,
        ILogger<SchedulerService> logger)
    {
        _recurringJobManager = recurringJobManager;
        _backgroundJobClient = backgroundJobClient;
        _logger = logger;
    }

    public Task ScheduleConnectionAsync(Guid connectionId, string cronExpression, CancellationToken cancellationToken = default)
    {
        var jobId = $"sync-{connectionId}";
        _recurringJobManager.AddOrUpdate<ISyncOrchestratorService>(
            jobId,
            service => service.ExecuteSyncAsync(connectionId, "scheduled", CancellationToken.None),
            cronExpression);

        _logger.LogInformation("Scheduled recurring sync for connection {ConnectionId} with cron {Cron}", connectionId, cronExpression);
        return Task.CompletedTask;
    }

    public Task UnscheduleConnectionAsync(Guid connectionId, CancellationToken cancellationToken = default)
    {
        var jobId = $"sync-{connectionId}";
        _recurringJobManager.RemoveIfExists(jobId);

        _logger.LogInformation("Unscheduled recurring sync for connection {ConnectionId}", connectionId);
        return Task.CompletedTask;
    }

    public Task<string> TriggerManualSyncAsync(Guid connectionId, string triggeredBy, CancellationToken cancellationToken = default)
    {
        var jobId = _backgroundJobClient.Enqueue<ISyncOrchestratorService>(
            service => service.ExecuteSyncAsync(connectionId, triggeredBy, CancellationToken.None));

        _logger.LogInformation("Manual sync triggered for connection {ConnectionId} by {TriggeredBy}, job {JobId}", connectionId, triggeredBy, jobId);
        return Task.FromResult(jobId);
    }
}
