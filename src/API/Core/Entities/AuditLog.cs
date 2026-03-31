namespace API.Core.Entities;

public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    public Guid? UserId { get; set; }
    public UserProfile? User { get; set; }
    public string? Details { get; set; } // JSON
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
