namespace API.Core.Interfaces;

public interface INotificationService
{
    Task SendSyncSuccessAsync(Guid connectionId, string fileName, int recordCount, CancellationToken ct = default);
    Task SendSyncFailureAsync(Guid connectionId, string errorMessage, CancellationToken ct = default);
    Task SendValidationWarningAsync(Guid connectionId, ValidationReport report, CancellationToken ct = default);
    Task SendNewMeterDetectedAsync(Guid connectionId, List<string> newMeters, CancellationToken ct = default);
}
