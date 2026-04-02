namespace API.Application.Services;

using System.Text.Json;
using API.Core.DTOs;
using API.Core.Entities;
using API.Core.Interfaces;
using API.Core.Models;
using Microsoft.Extensions.Logging;

public class SyncOrchestratorService : ISyncOrchestratorService
{
    private readonly IConnectionRepository _connectionRepository;
    private readonly IApiConnectorService _apiConnector;
    private readonly ICredentialVaultService _credentialVault;
    private readonly ITransformEngineService _transformEngine;
    private readonly ICsvGeneratorService _csvGenerator;
    private readonly ISftpDeliveryService _sftpDelivery;
    private readonly IAuditService _auditService;
    private readonly ISyncRunRepository _syncRunRepository;
    private readonly ILogger<SyncOrchestratorService> _logger;

    public SyncOrchestratorService(
        IConnectionRepository connectionRepository,
        IApiConnectorService apiConnector,
        ICredentialVaultService credentialVault,
        ITransformEngineService transformEngine,
        ICsvGeneratorService csvGenerator,
        ISftpDeliveryService sftpDelivery,
        IAuditService auditService,
        ISyncRunRepository syncRunRepository,
        ILogger<SyncOrchestratorService> logger)
    {
        _connectionRepository = connectionRepository;
        _apiConnector = apiConnector;
        _credentialVault = credentialVault;
        _transformEngine = transformEngine;
        _csvGenerator = csvGenerator;
        _sftpDelivery = sftpDelivery;
        _auditService = auditService;
        _syncRunRepository = syncRunRepository;
        _logger = logger;
    }

    public async Task<SyncResult> ExecuteSyncAsync(Guid connectionId, string triggeredBy, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionRepository.GetByIdWithDetailsAsync(connectionId, cancellationToken);
        if (connection == null)
        {
            return new SyncResult
            {
                Success = false,
                ErrorMessage = $"Connection {connectionId} not found",
                ErrorStep = "init"
            };
        }

        var syncRun = new SyncRun
        {
            ConnectionId = connectionId,
            Status = SyncRunStatus.Running,
            TriggeredBy = triggeredBy
        };
        await _syncRunRepository.CreateAsync(syncRun, cancellationToken);

        await _auditService.LogAsync("SyncStarted", "SyncRun", syncRun.Id, null,
            JsonSerializer.Serialize(new { connectionId, triggeredBy }), cancellationToken);

        try
        {
            // Step 1: Fetch data from API
            var authHeaders = await _credentialVault.BuildAuthHeadersAsync(connectionId, connection.AuthType, cancellationToken);
            var endpointPath = ReplaceDatePlaceholders(connection.EndpointPath, connection.ReportingLagDays);
            var apiUrl = connection.BaseUrl.TrimEnd('/');
            if (!string.IsNullOrEmpty(endpointPath))
            {
                apiUrl = $"{apiUrl}/{endpointPath.TrimStart('/')}";
            }
            var apiConfig = new ApiRequestConfig
            {
                Url = apiUrl,
                Headers = authHeaders.Headers
            };

            var apiResponse = await _apiConnector.SendRequestAsync(apiConfig, cancellationToken);
            if (!apiResponse.IsSuccess || string.IsNullOrEmpty(apiResponse.Body))
            {
                var errorDetail = !string.IsNullOrEmpty(apiResponse.Body)
                    ? $"External API returned {apiResponse.StatusCode}: {apiResponse.Body}"
                    : $"External API returned {apiResponse.StatusCode}: {apiResponse.ErrorMessage ?? "No response body"}";
                return await FailSyncAsync(syncRun, "fetch", errorDetail, cancellationToken);
            }

            // Step 2: Transform data
            List<Dictionary<string, string?>> transformedRows;
            try
            {
                var jsonDoc = JsonDocument.Parse(apiResponse.Body);
                transformedRows = _transformEngine.Transform(jsonDoc.RootElement, connection.Mappings.ToList());
            }
            catch (Exception ex)
            {
                return await FailSyncAsync(syncRun, "transform", ex.Message, cancellationToken);
            }

            // Step 3: Generate CSV
            var (csvBytes, _) = _csvGenerator.GenerateCsv(transformedRows);
            var fileName = _csvGenerator.GenerateFileName(connection.ClientName, connection.PlatformName);

            // Step 4: Deliver via SFTP
            var deliveryResult = await _sftpDelivery.DeliverFileAsync(new SftpDeliveryRequest
            {
                Host = connection.SftpHost ?? "",
                Port = connection.SftpPort,
                RemotePath = connection.SftpPath ?? "/",
                FileName = fileName,
                FileContent = csvBytes,
                ConnectionId = connectionId
            }, cancellationToken);

            if (!deliveryResult.Success)
            {
                return await FailSyncAsync(syncRun, "deliver", deliveryResult.ErrorMessage ?? "SFTP delivery failed", cancellationToken);
            }

            // Success
            syncRun.Status = SyncRunStatus.Succeeded;
            syncRun.CompletedAt = DateTime.UtcNow;
            syncRun.RecordCount = transformedRows.Count;
            syncRun.FileName = fileName;
            syncRun.FileSize = csvBytes.Length;
            await _syncRunRepository.UpdateAsync(syncRun, cancellationToken);

            await _auditService.LogAsync("SyncSucceeded", "SyncRun", syncRun.Id, null,
                JsonSerializer.Serialize(new { recordCount = transformedRows.Count, fileName }), cancellationToken);

            return new SyncResult
            {
                Success = true,
                SyncRunId = syncRun.Id,
                RecordCount = transformedRows.Count,
                FileName = fileName
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during sync for connection {ConnectionId}", connectionId);
            return await FailSyncAsync(syncRun, "unknown", ex.Message, cancellationToken);
        }
    }

    private async Task<SyncResult> FailSyncAsync(SyncRun syncRun, string errorStep, string errorMessage, CancellationToken cancellationToken)
    {
        syncRun.Status = SyncRunStatus.Failed;
        syncRun.CompletedAt = DateTime.UtcNow;
        syncRun.ErrorMessage = errorMessage;
        await _syncRunRepository.UpdateAsync(syncRun, cancellationToken);

        await _auditService.LogAsync("SyncFailed", "SyncRun", syncRun.Id, null,
            JsonSerializer.Serialize(new { errorStep, errorMessage }), cancellationToken);

        return new SyncResult
        {
            Success = false,
            SyncRunId = syncRun.Id,
            ErrorMessage = errorMessage,
            ErrorStep = errorStep
        };
    }

    /// <summary>
    /// Replaces date placeholders in the endpoint path with calculated values
    /// based on the reporting lag. Supported placeholders:
    /// {start_date} — first day of target month (yyyy-MM-dd)
    /// {end_date}   — last day of target month (yyyy-MM-dd)
    /// </summary>
    internal static string? ReplaceDatePlaceholders(string? endpointPath, int reportingLagDays)
    {
        if (string.IsNullOrEmpty(endpointPath) || !endpointPath.Contains('{'))
            return endpointPath;

        var targetDate = DateTime.UtcNow.AddDays(-reportingLagDays);
        var startOfMonth = new DateTime(targetDate.Year, targetDate.Month, 1);
        var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

        return endpointPath
            .Replace("{start_date}", startOfMonth.ToString("yyyy-MM-dd"))
            .Replace("{end_date}", endOfMonth.ToString("yyyy-MM-dd"));
    }
}
