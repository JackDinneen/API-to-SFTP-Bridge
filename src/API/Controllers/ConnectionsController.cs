using API.Application.Auth;
using API.Core.DTOs;
using API.Core.Entities;
using API.Core.Interfaces;
using API.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ConnectionsController : ControllerBase
{
    private readonly IConnectionRepository _connectionRepository;
    private readonly ISchedulerService _schedulerService;
    private readonly IApiConnectorService _apiConnectorService;
    private readonly ICredentialVaultService _credentialVaultService;
    private readonly ICurrentUserService _currentUser;

    public ConnectionsController(
        IConnectionRepository connectionRepository,
        ISchedulerService schedulerService,
        IApiConnectorService apiConnectorService,
        ICredentialVaultService credentialVaultService,
        ICurrentUserService currentUser)
    {
        _connectionRepository = connectionRepository;
        _schedulerService = schedulerService;
        _apiConnectorService = apiConnectorService;
        _credentialVaultService = credentialVaultService;
        _currentUser = currentUser;
    }

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.AllAuthenticated)]
    [ProducesResponseType(typeof(ApiResponse<List<ConnectionDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<ConnectionDto>>>> GetAll(CancellationToken cancellationToken)
    {
        var connections = await _connectionRepository.GetAllAsync(cancellationToken);
        var dtos = connections.Select(MapToDto).ToList();
        return Ok(ApiResponse<List<ConnectionDto>>.Ok(dtos));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.AllAuthenticated)]
    [ProducesResponseType(typeof(ApiResponse<ConnectionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ConnectionDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ConnectionDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var connection = await _connectionRepository.GetByIdAsync(id, cancellationToken);
        if (connection == null)
        {
            return NotFound(ApiResponse<ConnectionDto>.Fail($"Connection {id} not found"));
        }

        return Ok(ApiResponse<ConnectionDto>.Ok(MapToDto(connection)));
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [ProducesResponseType(typeof(ApiResponse<ConnectionDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<ConnectionDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<ConnectionDto>>> Create(
        [FromBody] CreateConnectionRequest request,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(_currentUser.UserId, out var userId))
        {
            return BadRequest(ApiResponse<ConnectionDto>.Fail("Cannot determine current user"));
        }

        var connection = new Connection
        {
            Name = request.Name,
            BaseUrl = request.BaseUrl,
            AuthType = request.AuthType,
            Status = ConnectionStatus.Paused,
            ScheduleCron = request.ScheduleCron,
            ClientName = request.ClientName,
            PlatformName = request.PlatformName,
            SftpHost = request.SftpHost,
            SftpPort = request.SftpPort,
            SftpPath = request.SftpPath,
            ReportingLagDays = request.ReportingLagDays,
            EndpointPath = request.EndpointPath,
            PaginationStrategy = request.PaginationStrategy,
            PaginationConfig = request.PaginationConfig,
            CreatedById = userId
        };

        var created = await _connectionRepository.CreateAsync(connection, cancellationToken);

        // Schedule if a cron expression was provided
        if (!string.IsNullOrEmpty(request.ScheduleCron))
        {
            await _schedulerService.ScheduleConnectionAsync(created.Id, request.ScheduleCron, cancellationToken);
        }

        return CreatedAtAction(
            nameof(GetById),
            new { id = created.Id },
            ApiResponse<ConnectionDto>.Ok(MapToDto(created)));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [ProducesResponseType(typeof(ApiResponse<ConnectionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ConnectionDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ConnectionDto>>> Update(
        Guid id,
        [FromBody] UpdateConnectionRequest request,
        CancellationToken cancellationToken)
    {
        var connection = await _connectionRepository.GetByIdAsync(id, cancellationToken);
        if (connection == null)
        {
            return NotFound(ApiResponse<ConnectionDto>.Fail($"Connection {id} not found"));
        }

        connection.Name = request.Name;
        connection.BaseUrl = request.BaseUrl;
        connection.AuthType = request.AuthType;
        connection.Status = request.Status;
        connection.ScheduleCron = request.ScheduleCron;
        connection.ClientName = request.ClientName;
        connection.PlatformName = request.PlatformName;
        connection.SftpHost = request.SftpHost;
        connection.SftpPort = request.SftpPort;
        connection.SftpPath = request.SftpPath;
        connection.ReportingLagDays = request.ReportingLagDays;
        connection.EndpointPath = request.EndpointPath;
        connection.PaginationStrategy = request.PaginationStrategy;
        connection.PaginationConfig = request.PaginationConfig;

        await _connectionRepository.UpdateAsync(connection, cancellationToken);

        // Update schedule
        if (!string.IsNullOrEmpty(request.ScheduleCron))
        {
            await _schedulerService.ScheduleConnectionAsync(id, request.ScheduleCron, cancellationToken);
        }
        else
        {
            await _schedulerService.UnscheduleConnectionAsync(id, cancellationToken);
        }

        return Ok(ApiResponse<ConnectionDto>.Ok(MapToDto(connection)));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id, CancellationToken cancellationToken)
    {
        var connection = await _connectionRepository.GetByIdAsync(id, cancellationToken);
        if (connection == null)
        {
            return NotFound(ApiResponse<bool>.Fail($"Connection {id} not found"));
        }

        // Unschedule before deleting
        await _schedulerService.UnscheduleConnectionAsync(id, cancellationToken);
        await _connectionRepository.DeleteAsync(id, cancellationToken);

        return Ok(ApiResponse<bool>.Ok(true, "Connection deleted"));
    }

    [HttpPost("{id:guid}/sync")]
    [Authorize(Policy = AuthorizationPolicies.AdminOrOperator)]
    [ProducesResponseType(typeof(ApiResponse<SyncResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SyncResult>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<SyncResult>>> TriggerSync(Guid id, CancellationToken cancellationToken)
    {
        var connection = await _connectionRepository.GetByIdAsync(id, cancellationToken);
        if (connection == null)
        {
            return NotFound(ApiResponse<SyncResult>.Fail($"Connection {id} not found"));
        }

        var triggeredBy = _currentUser.Email ?? "unknown";
        await _schedulerService.TriggerManualSyncAsync(id, triggeredBy, cancellationToken);

        return Ok(ApiResponse<SyncResult>.Ok(
            new SyncResult { Success = true },
            "Sync job enqueued"));
    }

    [HttpPost("{id:guid}/test-connection")]
    [Authorize(Policy = AuthorizationPolicies.AdminOrOperator)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<bool>>> TestConnection(Guid id, CancellationToken cancellationToken)
    {
        var connection = await _connectionRepository.GetByIdWithDetailsAsync(id, cancellationToken);
        if (connection == null)
        {
            return NotFound(ApiResponse<bool>.Fail($"Connection {id} not found"));
        }

        var authHeaders = await _credentialVaultService.BuildAuthHeadersAsync(id, connection.AuthType, cancellationToken);

        var apiUrl = connection.BaseUrl.TrimEnd('/');
        if (!string.IsNullOrEmpty(connection.EndpointPath))
        {
            apiUrl = $"{apiUrl}/{connection.EndpointPath.TrimStart('/')}";
        }

        var result = await _apiConnectorService.TestConnectionAsync(apiUrl, authHeaders.Headers, cancellationToken);

        if (result)
        {
            return Ok(ApiResponse<bool>.Ok(true, "API connection successful"));
        }

        return Ok(ApiResponse<bool>.Fail("API connection failed"));
    }

    private static ConnectionDto MapToDto(Connection connection)
    {
        return new ConnectionDto
        {
            Id = connection.Id,
            Name = connection.Name,
            BaseUrl = connection.BaseUrl,
            AuthType = connection.AuthType,
            Status = connection.Status,
            ScheduleCron = connection.ScheduleCron,
            ClientName = connection.ClientName,
            PlatformName = connection.PlatformName,
            SftpHost = connection.SftpHost,
            SftpPort = connection.SftpPort,
            SftpPath = connection.SftpPath,
            ReportingLagDays = connection.ReportingLagDays,
            EndpointPath = connection.EndpointPath,
            PaginationStrategy = connection.PaginationStrategy,
            CreatedAt = connection.CreatedAt,
            UpdatedAt = connection.UpdatedAt
        };
    }
}
