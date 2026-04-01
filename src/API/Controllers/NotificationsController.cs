using API.Application.Auth;
using API.Core.Entities;
using API.Core.Interfaces;
using API.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/connections/{connectionId:guid}/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationConfigRepository _notificationConfigRepo;
    private readonly IConnectionRepository _connectionRepository;

    public NotificationsController(
        INotificationConfigRepository notificationConfigRepo,
        IConnectionRepository connectionRepository)
    {
        _notificationConfigRepo = notificationConfigRepo;
        _connectionRepository = connectionRepository;
    }

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.AllAuthenticated)]
    [ProducesResponseType(typeof(ApiResponse<NotificationConfigDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<NotificationConfigDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<NotificationConfigDto>>> Get(
        Guid connectionId,
        CancellationToken cancellationToken)
    {
        var connection = await _connectionRepository.GetByIdAsync(connectionId, cancellationToken);
        if (connection == null)
        {
            return NotFound(ApiResponse<NotificationConfigDto>.Fail($"Connection {connectionId} not found"));
        }

        var config = await _notificationConfigRepo.GetByConnectionIdAsync(connectionId, cancellationToken);
        if (config == null)
        {
            // Return defaults
            return Ok(ApiResponse<NotificationConfigDto>.Ok(new NotificationConfigDto
            {
                ConnectionId = connectionId,
                NotifyOnSuccess = true,
                NotifyOnFailure = true,
                NotifyOnValidationWarning = true,
                NotifyOnNewMeter = true
            }));
        }

        return Ok(ApiResponse<NotificationConfigDto>.Ok(MapToDto(config)));
    }

    [HttpPut]
    [Authorize(Policy = AuthorizationPolicies.AdminOrOperator)]
    [ProducesResponseType(typeof(ApiResponse<NotificationConfigDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<NotificationConfigDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<NotificationConfigDto>>> Update(
        Guid connectionId,
        [FromBody] UpdateNotificationConfigRequest request,
        CancellationToken cancellationToken)
    {
        var connection = await _connectionRepository.GetByIdAsync(connectionId, cancellationToken);
        if (connection == null)
        {
            return NotFound(ApiResponse<NotificationConfigDto>.Fail($"Connection {connectionId} not found"));
        }

        var config = new NotificationConfig
        {
            ConnectionId = connectionId,
            NotifyOnSuccess = request.NotifyOnSuccess,
            NotifyOnFailure = request.NotifyOnFailure,
            NotifyOnValidationWarning = request.NotifyOnValidationWarning,
            NotifyOnNewMeter = request.NotifyOnNewMeter,
            EmailRecipients = request.EmailRecipients,
            WebhookUrl = request.WebhookUrl
        };

        var result = await _notificationConfigRepo.CreateOrUpdateAsync(config, cancellationToken);

        return Ok(ApiResponse<NotificationConfigDto>.Ok(MapToDto(result), "Notification configuration updated"));
    }

    private static NotificationConfigDto MapToDto(NotificationConfig config) => new()
    {
        Id = config.Id,
        ConnectionId = config.ConnectionId,
        NotifyOnSuccess = config.NotifyOnSuccess,
        NotifyOnFailure = config.NotifyOnFailure,
        NotifyOnValidationWarning = config.NotifyOnValidationWarning,
        NotifyOnNewMeter = config.NotifyOnNewMeter,
        EmailRecipients = config.EmailRecipients,
        WebhookUrl = config.WebhookUrl
    };
}

public class NotificationConfigDto
{
    public Guid Id { get; set; }
    public Guid ConnectionId { get; set; }
    public bool NotifyOnSuccess { get; set; }
    public bool NotifyOnFailure { get; set; }
    public bool NotifyOnValidationWarning { get; set; }
    public bool NotifyOnNewMeter { get; set; }
    public string? EmailRecipients { get; set; }
    public string? WebhookUrl { get; set; }
}

public class UpdateNotificationConfigRequest
{
    public bool NotifyOnSuccess { get; set; } = true;
    public bool NotifyOnFailure { get; set; } = true;
    public bool NotifyOnValidationWarning { get; set; } = true;
    public bool NotifyOnNewMeter { get; set; } = true;
    public string? EmailRecipients { get; set; }
    public string? WebhookUrl { get; set; }
}
