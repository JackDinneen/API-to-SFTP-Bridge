using API.Application.Auth;
using API.Core.Interfaces;
using API.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AuditController : ControllerBase
{
    private readonly IAuditLogRepository _auditLogRepo;

    public AuditController(IAuditLogRepository auditLogRepo)
    {
        _auditLogRepo = auditLogRepo;
    }

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [ProducesResponseType(typeof(ApiResponse<List<AuditLogDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<AuditLogDto>>>> GetAll(
        [FromQuery] string? entityType = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int limit = 100,
        CancellationToken cancellationToken = default)
    {
        var logs = await _auditLogRepo.GetFilteredAsync(entityType, from, to, limit, cancellationToken);
        var dtos = logs.Select(MapToDto).ToList();
        return Ok(ApiResponse<List<AuditLogDto>>.Ok(dtos));
    }

    [HttpGet("export")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> Export(
        [FromQuery] string? entityType = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        var logs = await _auditLogRepo.GetFilteredAsync(entityType, from, to, limit: 10000, cancellationToken);

        var sb = new StringBuilder();
        sb.AppendLine("Id,Action,EntityType,EntityId,UserId,Details,CreatedAt");

        foreach (var log in logs)
        {
            var details = EscapeCsvField(log.Details ?? "");
            sb.AppendLine($"{log.Id},{EscapeCsvField(log.Action)},{EscapeCsvField(log.EntityType)},{log.EntityId},{log.UserId},{details},{log.CreatedAt:yyyy-MM-dd HH:mm:ss}");
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/csv", $"audit_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv");
    }

    private static string EscapeCsvField(string field)
    {
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }
        return field;
    }

    private static AuditLogDto MapToDto(Core.Entities.AuditLog log) => new()
    {
        Id = log.Id,
        Action = log.Action,
        EntityType = log.EntityType,
        EntityId = log.EntityId,
        UserId = log.UserId,
        Details = log.Details,
        CreatedAt = log.CreatedAt
    };
}

public class AuditLogDto
{
    public Guid Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    public Guid? UserId { get; set; }
    public string? Details { get; set; }
    public DateTime CreatedAt { get; set; }
}
