namespace API.Core.Interfaces;

using API.Core.Entities;

public interface INotificationConfigRepository
{
    Task<NotificationConfig?> GetByConnectionIdAsync(Guid connectionId, CancellationToken cancellationToken = default);
    Task<NotificationConfig> CreateOrUpdateAsync(NotificationConfig config, CancellationToken cancellationToken = default);
}
