namespace API.Infrastructure.Repositories;

using API.Core.Entities;
using API.Core.Interfaces;
using API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

public class NotificationConfigRepository : INotificationConfigRepository
{
    private readonly ApplicationDbContext _context;

    public NotificationConfigRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<NotificationConfig?> GetByConnectionIdAsync(Guid connectionId, CancellationToken cancellationToken = default)
    {
        return await _context.NotificationConfigs
            .FirstOrDefaultAsync(n => n.ConnectionId == connectionId, cancellationToken);
    }

    public async Task<NotificationConfig> CreateOrUpdateAsync(NotificationConfig config, CancellationToken cancellationToken = default)
    {
        var existing = await _context.NotificationConfigs
            .FirstOrDefaultAsync(n => n.ConnectionId == config.ConnectionId, cancellationToken);

        if (existing == null)
        {
            _context.NotificationConfigs.Add(config);
        }
        else
        {
            existing.NotifyOnSuccess = config.NotifyOnSuccess;
            existing.NotifyOnFailure = config.NotifyOnFailure;
            existing.NotifyOnValidationWarning = config.NotifyOnValidationWarning;
            existing.NotifyOnNewMeter = config.NotifyOnNewMeter;
            existing.EmailRecipients = config.EmailRecipients;
            existing.WebhookUrl = config.WebhookUrl;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return existing ?? config;
    }
}
