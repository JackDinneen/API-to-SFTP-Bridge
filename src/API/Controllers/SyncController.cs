using API.Application.Auth;
using API.Core.DTOs;
using API.Core.Interfaces;
using API.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SyncController : ControllerBase
{
    private readonly ISyncRunRepository _syncRunRepository;
    private readonly IConnectionRepository _connectionRepository;

    public SyncController(
        ISyncRunRepository syncRunRepository,
        IConnectionRepository connectionRepository)
    {
        _syncRunRepository = syncRunRepository;
        _connectionRepository = connectionRepository;
    }

    [HttpGet("{connectionId:guid}/history")]
    [Authorize(Policy = AuthorizationPolicies.AllAuthenticated)]
    [ProducesResponseType(typeof(ApiResponse<List<SyncRunDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<SyncRunDto>>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<List<SyncRunDto>>>> GetHistory(
        Guid connectionId,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var connection = await _connectionRepository.GetByIdAsync(connectionId, cancellationToken);
        if (connection == null)
        {
            return NotFound(ApiResponse<List<SyncRunDto>>.Fail($"Connection {connectionId} not found"));
        }

        var syncRuns = await _syncRunRepository.GetByConnectionIdAsync(connectionId, limit, cancellationToken);
        var dtos = syncRuns.Select(MapToDto).ToList();

        return Ok(ApiResponse<List<SyncRunDto>>.Ok(dtos));
    }

    [HttpGet("{connectionId:guid}/latest")]
    [Authorize(Policy = AuthorizationPolicies.AllAuthenticated)]
    [ProducesResponseType(typeof(ApiResponse<SyncRunDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SyncRunDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<SyncRunDto>>> GetLatest(
        Guid connectionId,
        CancellationToken cancellationToken = default)
    {
        var connection = await _connectionRepository.GetByIdAsync(connectionId, cancellationToken);
        if (connection == null)
        {
            return NotFound(ApiResponse<SyncRunDto>.Fail($"Connection {connectionId} not found"));
        }

        var syncRuns = await _syncRunRepository.GetByConnectionIdAsync(connectionId, 1, cancellationToken);
        var latest = syncRuns.FirstOrDefault();

        if (latest == null)
        {
            return NotFound(ApiResponse<SyncRunDto>.Fail($"No sync runs found for connection {connectionId}"));
        }

        return Ok(ApiResponse<SyncRunDto>.Ok(MapToDto(latest)));
    }

    private static SyncRunDto MapToDto(Core.Entities.SyncRun syncRun)
    {
        return new SyncRunDto
        {
            Id = syncRun.Id,
            ConnectionId = syncRun.ConnectionId,
            Status = syncRun.Status,
            CompletedAt = syncRun.CompletedAt,
            RecordCount = syncRun.RecordCount,
            FileSize = syncRun.FileSize,
            FileName = syncRun.FileName,
            ErrorMessage = syncRun.ErrorMessage,
            TriggeredBy = syncRun.TriggeredBy,
            RetryCount = syncRun.RetryCount,
            CreatedAt = syncRun.CreatedAt
        };
    }
}
