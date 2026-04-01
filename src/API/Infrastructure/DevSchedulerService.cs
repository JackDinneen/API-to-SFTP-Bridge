using API.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace API.Infrastructure;

/// <summary>
/// No-op scheduler for local development without Hangfire/SQL Server.
/// Logs actions but does not actually schedule jobs.
/// </summary>
public class DevSchedulerService : ISchedulerService
{
    private readonly ILogger<DevSchedulerService> _logger;

    public DevSchedulerService(ILogger<DevSchedulerService> logger)
    {
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

    public Task<string> TriggerManualSyncAsync(Guid connectionId, string triggeredBy, CancellationToken cancellationToken = default)
    {
        var jobId = Guid.NewGuid().ToString();
        _logger.LogInformation("[Dev] Would trigger manual sync for connection {ConnectionId} by {TriggeredBy}, mock job {JobId}", connectionId, triggeredBy, jobId);
        return Task.FromResult(jobId);
    }
}
