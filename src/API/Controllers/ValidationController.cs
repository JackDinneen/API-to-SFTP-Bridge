using API.Application.Auth;
using API.Core.Interfaces;
using API.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ValidationController : ControllerBase
{
    private readonly IValidationEngineService _validationEngine;
    private readonly ISyncRunRepository _syncRunRepository;
    private readonly IConnectionRepository _connectionRepository;

    public ValidationController(
        IValidationEngineService validationEngine,
        ISyncRunRepository syncRunRepository,
        IConnectionRepository connectionRepository)
    {
        _validationEngine = validationEngine;
        _syncRunRepository = syncRunRepository;
        _connectionRepository = connectionRepository;
    }

    [HttpPost("run/{connectionId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOrOperator)]
    [ProducesResponseType(typeof(ApiResponse<ValidationReport>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ValidationReport>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ValidationReport>>> RunValidation(
        Guid connectionId,
        CancellationToken cancellationToken)
    {
        var connection = await _connectionRepository.GetByIdAsync(connectionId, cancellationToken);
        if (connection == null)
        {
            return NotFound(ApiResponse<ValidationReport>.Fail($"Connection {connectionId} not found"));
        }

        // Get the latest sync run for this connection
        var syncRuns = await _syncRunRepository.GetByConnectionIdAsync(connectionId, 1, cancellationToken);
        var latestRun = syncRuns.FirstOrDefault();

        if (latestRun == null)
        {
            return NotFound(ApiResponse<ValidationReport>.Fail($"No sync runs found for connection {connectionId}"));
        }

        // Load the sync run with records via the repository
        var syncRunWithRecords = await _syncRunRepository.GetByIdWithRecordsAsync(latestRun.Id, cancellationToken);
        if (syncRunWithRecords == null || syncRunWithRecords.Records.Count == 0)
        {
            return NotFound(ApiResponse<ValidationReport>.Fail("No records found in the latest sync run"));
        }

        // Convert SyncRunRecords to dictionary rows for validation
        var rows = syncRunWithRecords.Records.Select(r => new Dictionary<string, object?>
        {
            ["AssetId"] = r.AssetId,
            ["Asset name"] = r.AssetName,
            ["SubmeterCode"] = r.SubmeterCode,
            ["UtilityType"] = r.UtilityType,
            ["Year"] = r.Year,
            ["Month"] = r.Month,
            ["Value"] = r.Value
        }).ToList();

        var report = await _validationEngine.ValidateAsync(rows, cancellationToken);

        return Ok(ApiResponse<ValidationReport>.Ok(report, "Validation complete"));
    }
}
