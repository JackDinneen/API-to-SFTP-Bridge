using API.Application.Auth;
using API.Core.Entities;
using API.Core.Interfaces;
using API.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace API.Controllers;

[ApiController]
[Route("api/reference-data")]
[Authorize]
public class ReferenceDataController : ControllerBase
{
    private readonly IReferenceDataRepository _referenceDataRepo;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _auditService;

    public ReferenceDataController(
        IReferenceDataRepository referenceDataRepo,
        ICurrentUserService currentUser,
        IAuditService auditService)
    {
        _referenceDataRepo = referenceDataRepo;
        _currentUser = currentUser;
        _auditService = auditService;
    }

    [HttpPost("upload")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<int>>> Upload(IFormFile file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(ApiResponse<int>.Fail("No file provided"));
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension != ".csv" && extension != ".json")
        {
            return BadRequest(ApiResponse<int>.Fail("Only CSV and JSON files are supported"));
        }

        if (!Guid.TryParse(_currentUser.UserId, out var userId))
        {
            return BadRequest(ApiResponse<int>.Fail("Cannot determine current user"));
        }

        using var reader = new StreamReader(file.OpenReadStream());
        var content = await reader.ReadToEndAsync(cancellationToken);

        List<ReferenceData> items;
        try
        {
            items = extension == ".csv"
                ? ParseCsv(content, userId)
                : ParseJson(content, userId);
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<int>.Fail($"Failed to parse file: {ex.Message}"));
        }

        if (items.Count == 0)
        {
            return BadRequest(ApiResponse<int>.Fail("No valid records found in file"));
        }

        await _referenceDataRepo.BulkImportAsync(items, cancellationToken);

        await _auditService.LogAsync(
            "ReferenceDataUploaded",
            "ReferenceData",
            userId: userId,
            details: JsonSerializer.Serialize(new { FileName = file.FileName, RecordCount = items.Count }),
            cancellationToken: cancellationToken);

        return Ok(ApiResponse<int>.Ok(items.Count, $"{items.Count} reference data records imported"));
    }

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.AllAuthenticated)]
    [ProducesResponseType(typeof(ApiResponse<List<ReferenceDataDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<ReferenceDataDto>>>> GetAll(CancellationToken cancellationToken)
    {
        var items = await _referenceDataRepo.GetAllAsync(cancellationToken);
        var dtos = items.Select(MapToDto).ToList();
        return Ok(ApiResponse<List<ReferenceDataDto>>.Ok(dtos));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id, CancellationToken cancellationToken)
    {
        var item = await _referenceDataRepo.GetByIdAsync(id, cancellationToken);
        if (item == null)
        {
            return NotFound(ApiResponse<bool>.Fail($"Reference data {id} not found"));
        }

        await _referenceDataRepo.DeleteAsync(id, cancellationToken);

        if (Guid.TryParse(_currentUser.UserId, out var userId))
        {
            await _auditService.LogAsync(
                "ReferenceDataDeleted",
                "ReferenceData",
                entityId: id,
                userId: userId,
                cancellationToken: cancellationToken);
        }

        return Ok(ApiResponse<bool>.Ok(true, "Reference data record deleted"));
    }

    private static List<ReferenceData> ParseCsv(string content, Guid userId)
    {
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length < 2) return new List<ReferenceData>();

        var headers = lines[0].Split(',').Select(h => h.Trim().Trim('"')).ToArray();
        var assetIdIdx = Array.FindIndex(headers, h => h.Equals("Asset ID", StringComparison.OrdinalIgnoreCase) || h.Equals("AssetId", StringComparison.OrdinalIgnoreCase));
        var assetNameIdx = Array.FindIndex(headers, h => h.Equals("Asset name", StringComparison.OrdinalIgnoreCase) || h.Equals("AssetName", StringComparison.OrdinalIgnoreCase));
        var submeterIdx = Array.FindIndex(headers, h => h.Equals("Submeter Code", StringComparison.OrdinalIgnoreCase) || h.Equals("SubmeterCode", StringComparison.OrdinalIgnoreCase));
        var utilityIdx = Array.FindIndex(headers, h => h.Equals("Utility Type", StringComparison.OrdinalIgnoreCase) || h.Equals("UtilityType", StringComparison.OrdinalIgnoreCase));

        if (assetIdIdx < 0 || assetNameIdx < 0 || submeterIdx < 0 || utilityIdx < 0)
        {
            throw new FormatException("CSV must contain columns: Asset ID, Asset name, Submeter Code, Utility Type");
        }

        var items = new List<ReferenceData>();
        for (var i = 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            var fields = line.Split(',').Select(f => f.Trim().Trim('"')).ToArray();
            if (fields.Length <= Math.Max(Math.Max(assetIdIdx, assetNameIdx), Math.Max(submeterIdx, utilityIdx)))
                continue;

            items.Add(new ReferenceData
            {
                AssetId = fields[assetIdIdx],
                AssetName = fields[assetNameIdx],
                SubmeterCode = fields[submeterIdx],
                UtilityType = fields[utilityIdx],
                UploadedById = userId
            });
        }

        return items;
    }

    private static List<ReferenceData> ParseJson(string content, Guid userId)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var records = JsonSerializer.Deserialize<List<ReferenceDataImportDto>>(content, options);
        if (records == null) return new List<ReferenceData>();

        return records.Select(r => new ReferenceData
        {
            AssetId = r.AssetId ?? "",
            AssetName = r.AssetName ?? "",
            SubmeterCode = r.SubmeterCode ?? "",
            UtilityType = r.UtilityType ?? "",
            UploadedById = userId
        }).ToList();
    }

    private static ReferenceDataDto MapToDto(ReferenceData item) => new()
    {
        Id = item.Id,
        AssetId = item.AssetId,
        AssetName = item.AssetName,
        SubmeterCode = item.SubmeterCode,
        UtilityType = item.UtilityType,
        CreatedAt = item.CreatedAt
    };
}

public class ReferenceDataDto
{
    public Guid Id { get; set; }
    public string AssetId { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;
    public string SubmeterCode { get; set; } = string.Empty;
    public string UtilityType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class ReferenceDataImportDto
{
    public string? AssetId { get; set; }
    public string? AssetName { get; set; }
    public string? SubmeterCode { get; set; }
    public string? UtilityType { get; set; }
}
