namespace API.Core.Entities;

using API.Core.Models;

public class UserProfile : BaseEntity
{
    public string AzureAdId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Viewer;

    // Navigation
    public ICollection<Connection> CreatedConnections { get; set; } = new List<Connection>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}
