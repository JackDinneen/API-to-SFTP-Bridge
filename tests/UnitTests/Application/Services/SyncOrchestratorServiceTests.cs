namespace UnitTests.Application.Services;

using System.Text.Json;
using API.Core.DTOs;
using API.Core.Entities;
using API.Core.Interfaces;
using API.Core.Models;
using API.Application.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

public class SyncOrchestratorServiceTests
{
    private readonly Mock<IConnectionRepository> _connectionRepo;
    private readonly Mock<IApiConnectorService> _apiConnector;
    private readonly Mock<ICredentialVaultService> _credentialVault;
    private readonly Mock<ITransformEngineService> _transformEngine;
    private readonly Mock<ICsvGeneratorService> _csvGenerator;
    private readonly Mock<ISftpDeliveryService> _sftpDelivery;
    private readonly Mock<IAuditService> _auditService;
    private readonly Mock<ISyncRunRepository> _syncRunRepo;
    private readonly SyncOrchestratorService _sut;

    public SyncOrchestratorServiceTests()
    {
        _connectionRepo = new Mock<IConnectionRepository>();
        _apiConnector = new Mock<IApiConnectorService>();
        _credentialVault = new Mock<ICredentialVaultService>();
        _transformEngine = new Mock<ITransformEngineService>();
        _csvGenerator = new Mock<ICsvGeneratorService>();
        _sftpDelivery = new Mock<ISftpDeliveryService>();
        _auditService = new Mock<IAuditService>();
        _syncRunRepo = new Mock<ISyncRunRepository>();

        _sut = new SyncOrchestratorService(
            _connectionRepo.Object,
            _apiConnector.Object,
            _credentialVault.Object,
            _transformEngine.Object,
            _csvGenerator.Object,
            _sftpDelivery.Object,
            _auditService.Object,
            _syncRunRepo.Object,
            Mock.Of<ILogger<SyncOrchestratorService>>());
    }

    private Connection CreateTestConnection(Guid? id = null)
    {
        return new Connection
        {
            Id = id ?? Guid.NewGuid(),
            Name = "Test Connection",
            BaseUrl = "https://api.example.com",
            EndpointPath = "/data",
            AuthType = AuthType.ApiKey,
            Status = ConnectionStatus.Active,
            ClientName = "testclient",
            PlatformName = "testplatform",
            SftpHost = "sftp.example.com",
            SftpPort = 22,
            SftpPath = "/uploads",
            CreatedById = Guid.NewGuid(),
            Mappings = new List<ConnectionMapping>
            {
                new() { SourcePath = "data.value", TargetColumn = "Value", TransformType = TransformType.DirectMapping }
            },
            Credentials = new List<ConnectionCredential>()
        };
    }

    private void SetupSuccessfulPipeline(Connection connection)
    {
        _connectionRepo.Setup(r => r.GetByIdWithDetailsAsync(connection.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(connection);

        _syncRunRepo.Setup(r => r.CreateAsync(It.IsAny<SyncRun>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SyncRun s, CancellationToken _) => s);

        _syncRunRepo.Setup(r => r.UpdateAsync(It.IsAny<SyncRun>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SyncRun s, CancellationToken _) => s);

        _credentialVault.Setup(v => v.BuildAuthHeadersAsync(connection.Id, connection.AuthType, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthHeaders { Headers = new Dictionary<string, string> { { "Authorization", "Bearer token" } } });

        _apiConnector.Setup(a => a.SendRequestAsync(It.IsAny<ApiRequestConfig>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse { IsSuccess = true, StatusCode = 200, Body = "{\"data\": [{\"value\": 100}]}" });

        var rows = new List<Dictionary<string, string?>>
        {
            new() { { "Value", "100" }, { "Asset ID", "AST-001" } }
        };
        _transformEngine.Setup(t => t.Transform(It.IsAny<JsonElement>(), It.IsAny<List<ConnectionMapping>>()))
            .Returns(rows);

        _csvGenerator.Setup(g => g.GenerateCsv(rows))
            .Returns((new byte[] { 1, 2, 3 }, "sha256hash"));

        _csvGenerator.Setup(g => g.GenerateFileName("testclient", "testplatform", It.IsAny<DateTime?>()))
            .Returns("testclient_testplatform_31032026.csv");

        _sftpDelivery.Setup(s => s.DeliverFileAsync(It.IsAny<SftpDeliveryRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SftpDeliveryResult { Success = true, RemoteFilePath = "/uploads/test.csv", BytesTransferred = 3 });
    }

    // ---------------------------------------------------------------
    // Successful sync
    // ---------------------------------------------------------------

    [Fact]
    public async Task ExecuteSyncAsync_SuccessfulSync_ReturnsSucceededResult()
    {
        var connection = CreateTestConnection();
        SetupSuccessfulPipeline(connection);

        var result = await _sut.ExecuteSyncAsync(connection.Id, "test@example.com");

        result.Success.Should().BeTrue();
        result.RecordCount.Should().Be(1);
        result.FileName.Should().Be("testclient_testplatform_31032026.csv");
    }

    [Fact]
    public async Task ExecuteSyncAsync_SuccessfulSync_CreatesSyncRunRecord()
    {
        var connection = CreateTestConnection();
        SetupSuccessfulPipeline(connection);

        await _sut.ExecuteSyncAsync(connection.Id, "scheduled");

        _syncRunRepo.Verify(r => r.CreateAsync(It.Is<SyncRun>(s =>
            s.ConnectionId == connection.Id &&
            s.TriggeredBy == "scheduled"
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteSyncAsync_SuccessfulSync_UpdatesSyncRunWithRecordCountAndFileName()
    {
        var connection = CreateTestConnection();
        SetupSuccessfulPipeline(connection);

        await _sut.ExecuteSyncAsync(connection.Id, "scheduled");

        _syncRunRepo.Verify(r => r.UpdateAsync(It.Is<SyncRun>(s =>
            s.Status == SyncRunStatus.Succeeded &&
            s.RecordCount == 1 &&
            s.FileName == "testclient_testplatform_31032026.csv" &&
            s.CompletedAt != null
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteSyncAsync_SuccessfulSync_LogsAuditEntries()
    {
        var connection = CreateTestConnection();
        SetupSuccessfulPipeline(connection);

        await _sut.ExecuteSyncAsync(connection.Id, "test@example.com");

        _auditService.Verify(a => a.LogAsync("SyncStarted", "SyncRun", It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
        _auditService.Verify(a => a.LogAsync("SyncSucceeded", "SyncRun", It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---------------------------------------------------------------
    // Connection not found
    // ---------------------------------------------------------------

    [Fact]
    public async Task ExecuteSyncAsync_ConnectionNotFound_ReturnsError()
    {
        var connectionId = Guid.NewGuid();
        _connectionRepo.Setup(r => r.GetByIdWithDetailsAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Connection?)null);

        var result = await _sut.ExecuteSyncAsync(connectionId, "test@example.com");

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
        result.ErrorStep.Should().Be("init");
    }

    // ---------------------------------------------------------------
    // Fetch failure
    // ---------------------------------------------------------------

    [Fact]
    public async Task ExecuteSyncAsync_FetchFailure_ReturnsFailedWithFetchStep()
    {
        var connection = CreateTestConnection();
        SetupSuccessfulPipeline(connection);

        _apiConnector.Setup(a => a.SendRequestAsync(It.IsAny<ApiRequestConfig>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse { IsSuccess = false, StatusCode = 500, ErrorMessage = "Server error" });

        var result = await _sut.ExecuteSyncAsync(connection.Id, "scheduled");

        result.Success.Should().BeFalse();
        result.ErrorStep.Should().Be("fetch");
        result.ErrorMessage.Should().Contain("Server error");
    }

    // ---------------------------------------------------------------
    // Transform failure
    // ---------------------------------------------------------------

    [Fact]
    public async Task ExecuteSyncAsync_TransformFailure_ReturnsFailedWithTransformStep()
    {
        var connection = CreateTestConnection();
        SetupSuccessfulPipeline(connection);

        _transformEngine.Setup(t => t.Transform(It.IsAny<JsonElement>(), It.IsAny<List<ConnectionMapping>>()))
            .Throws(new InvalidOperationException("Bad mapping configuration"));

        var result = await _sut.ExecuteSyncAsync(connection.Id, "scheduled");

        result.Success.Should().BeFalse();
        result.ErrorStep.Should().Be("transform");
        result.ErrorMessage.Should().Contain("Bad mapping configuration");
    }

    // ---------------------------------------------------------------
    // SFTP delivery failure
    // ---------------------------------------------------------------

    [Fact]
    public async Task ExecuteSyncAsync_SftpDeliveryFailure_ReturnsFailedWithDeliverStep()
    {
        var connection = CreateTestConnection();
        SetupSuccessfulPipeline(connection);

        _sftpDelivery.Setup(s => s.DeliverFileAsync(It.IsAny<SftpDeliveryRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SftpDeliveryResult { Success = false, ErrorMessage = "Connection refused" });

        var result = await _sut.ExecuteSyncAsync(connection.Id, "scheduled");

        result.Success.Should().BeFalse();
        result.ErrorStep.Should().Be("deliver");
        result.ErrorMessage.Should().Contain("Connection refused");
    }
}
