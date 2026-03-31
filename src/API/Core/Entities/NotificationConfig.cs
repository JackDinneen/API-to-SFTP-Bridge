namespace API.Core.Entities;

public class NotificationConfig : BaseEntity
{
    public Guid ConnectionId { get; set; }
    public Connection Connection { get; set; } = null!;
    public bool NotifyOnSuccess { get; set; } = true;
    public bool NotifyOnFailure { get; set; } = true;
    public bool NotifyOnValidationWarning { get; set; } = true;
    public bool NotifyOnNewMeter { get; set; } = true;
    public string? EmailRecipients { get; set; } // Comma-separated emails
    public string? WebhookUrl { get; set; }
}
