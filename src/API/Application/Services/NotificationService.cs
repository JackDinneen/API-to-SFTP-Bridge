namespace API.Application.Services;

using API.Core.Entities;
using API.Core.Interfaces;
using Microsoft.Extensions.Logging;

public class NotificationService : INotificationService
{
    private readonly INotificationConfigRepository _configRepo;
    private readonly IConnectionRepository _connectionRepo;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        INotificationConfigRepository configRepo,
        IConnectionRepository connectionRepo,
        ILogger<NotificationService> logger)
    {
        _configRepo = configRepo;
        _connectionRepo = connectionRepo;
        _logger = logger;
    }

    public async Task SendSyncSuccessAsync(Guid connectionId, string fileName, int recordCount, CancellationToken ct = default)
    {
        var config = await _configRepo.GetByConnectionIdAsync(connectionId, ct);
        if (config == null || !config.NotifyOnSuccess)
        {
            _logger.LogDebug("Sync success notification skipped for connection {ConnectionId} — not enabled", connectionId);
            return;
        }

        var connection = await _connectionRepo.GetByIdAsync(connectionId, ct);
        var connectionName = connection?.Name ?? connectionId.ToString();

        var subject = $"[Obi Bridge] Sync Succeeded — {connectionName}";
        var body = $"Sync completed successfully for connection '{connectionName}'.\n\n" +
                   $"File: {fileName}\n" +
                   $"Records: {recordCount}\n" +
                   $"Time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC";

        LogNotification(subject, body, config);
    }

    public async Task SendSyncFailureAsync(Guid connectionId, string errorMessage, CancellationToken ct = default)
    {
        var config = await _configRepo.GetByConnectionIdAsync(connectionId, ct);
        if (config == null || !config.NotifyOnFailure)
        {
            _logger.LogDebug("Sync failure notification skipped for connection {ConnectionId} — not enabled", connectionId);
            return;
        }

        var connection = await _connectionRepo.GetByIdAsync(connectionId, ct);
        var connectionName = connection?.Name ?? connectionId.ToString();

        var subject = $"[Obi Bridge] Sync FAILED — {connectionName}";
        var body = $"Sync failed for connection '{connectionName}'.\n\n" +
                   $"Error: {errorMessage}\n" +
                   $"Time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC\n\n" +
                   "Please review the sync logs and retry if appropriate.";

        LogNotification(subject, body, config);
    }

    public async Task SendValidationWarningAsync(Guid connectionId, ValidationReport report, CancellationToken ct = default)
    {
        var config = await _configRepo.GetByConnectionIdAsync(connectionId, ct);
        if (config == null || !config.NotifyOnValidationWarning)
        {
            _logger.LogDebug("Validation warning notification skipped for connection {ConnectionId} — not enabled", connectionId);
            return;
        }

        var connection = await _connectionRepo.GetByIdAsync(connectionId, ct);
        var connectionName = connection?.Name ?? connectionId.ToString();

        var subject = $"[Obi Bridge] Validation Warnings — {connectionName}";
        var body = $"Validation completed for connection '{connectionName}' with warnings.\n\n" +
                   $"Total Rows: {report.TotalRows}\n" +
                   $"Passed: {report.PassedRows}\n" +
                   $"Warnings: {report.WarningRows}\n" +
                   $"Errors: {report.ErrorRows}\n\n";

        var issueRows = report.Rows.Where(r => r.Status != "Passed").Take(20).ToList();
        if (issueRows.Count > 0)
        {
            body += "Issues:\n";
            foreach (var row in issueRows)
            {
                body += $"  Row {row.RowNumber} [{row.Status}]: {string.Join("; ", row.Messages)}\n";
            }
        }

        LogNotification(subject, body, config);
    }

    public async Task SendNewMeterDetectedAsync(Guid connectionId, List<string> newMeters, CancellationToken ct = default)
    {
        var config = await _configRepo.GetByConnectionIdAsync(connectionId, ct);
        if (config == null || !config.NotifyOnNewMeter)
        {
            _logger.LogDebug("New meter notification skipped for connection {ConnectionId} — not enabled", connectionId);
            return;
        }

        var connection = await _connectionRepo.GetByIdAsync(connectionId, ct);
        var connectionName = connection?.Name ?? connectionId.ToString();

        var subject = $"[Obi Bridge] New Meters Detected — {connectionName}";
        var body = $"New meters have been detected for connection '{connectionName}'.\n\n" +
                   $"Count: {newMeters.Count}\n" +
                   $"Meters:\n";

        foreach (var meter in newMeters.Take(50))
        {
            body += $"  - {meter}\n";
        }

        if (newMeters.Count > 50)
        {
            body += $"  ... and {newMeters.Count - 50} more\n";
        }

        LogNotification(subject, body, config);
    }

    private void LogNotification(string subject, string body, NotificationConfig config)
    {
        var recipients = config.EmailRecipients ?? "(no recipients configured)";
        _logger.LogInformation(
            "NOTIFICATION — To: {Recipients}, Subject: {Subject}\n{Body}",
            recipients, subject, body);

        if (!string.IsNullOrEmpty(config.WebhookUrl))
        {
            _logger.LogInformation(
                "WEBHOOK — URL: {WebhookUrl}, Subject: {Subject}",
                config.WebhookUrl, subject);
        }
    }
}
