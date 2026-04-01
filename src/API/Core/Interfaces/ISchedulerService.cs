namespace API.Core.Interfaces;

public interface ISchedulerService
{
    Task ScheduleConnectionAsync(Guid connectionId, string cronExpression, CancellationToken cancellationToken = default);
    Task UnscheduleConnectionAsync(Guid connectionId, CancellationToken cancellationToken = default);
    Task<string> TriggerManualSyncAsync(Guid connectionId, string triggeredBy, CancellationToken cancellationToken = default);
}
