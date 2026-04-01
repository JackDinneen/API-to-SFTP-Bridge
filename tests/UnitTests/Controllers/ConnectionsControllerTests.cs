namespace UnitTests.Controllers;

using API.Controllers;
using API.Core.DTOs;
using API.Core.Entities;
using API.Core.Interfaces;
using API.Core.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

public class ConnectionsControllerTests
{
    private readonly Mock<IConnectionRepository> _connectionRepo;
    private readonly Mock<ISchedulerService> _schedulerService;
    private readonly Mock<IApiConnectorService> _apiConnectorService;
    private readonly Mock<ICredentialVaultService> _credentialVaultService;
    private readonly Mock<ISyncRunRepository> _syncRunRepo;
    private readonly Mock<ICurrentUserService> _currentUser;
    private readonly ConnectionsController _sut;

    private static readonly string TestUserId = "00000000-0000-0000-0000-000000000001";
    private static readonly string TestEmail = "test@test.com";

    public ConnectionsControllerTests()
    {
        _connectionRepo = new Mock<IConnectionRepository>();
        _schedulerService = new Mock<ISchedulerService>();
        _apiConnectorService = new Mock<IApiConnectorService>();
        _credentialVaultService = new Mock<ICredentialVaultService>();
        _syncRunRepo = new Mock<ISyncRunRepository>();
        _currentUser = new Mock<ICurrentUserService>();

        _currentUser.Setup(x => x.UserId).Returns(TestUserId);
        _currentUser.Setup(x => x.Email).Returns(TestEmail);

        _connectionRepo.Setup(x => x.CreateAsync(It.IsAny<Connection>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Connection c, CancellationToken _) =>
            {
                c.Id = Guid.NewGuid();
                return c;
            });

        _credentialVaultService.Setup(x => x.StoreCredentialAsync(It.IsAny<StoreCredentialRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CredentialReference());

        _sut = new ConnectionsController(
            _connectionRepo.Object,
            _schedulerService.Object,
            _apiConnectorService.Object,
            _credentialVaultService.Object,
            _syncRunRepo.Object,
            _currentUser.Object);
    }

    private static CreateConnectionRequest BuildValidRequest(bool activate = false)
    {
        return new CreateConnectionRequest
        {
            Name = "Test Connection",
            BaseUrl = "https://api.example.com",
            AuthType = AuthType.ApiKey,
            ClientName = "acme",
            PlatformName = "energystar",
            SftpPort = 22,
            ReportingLagDays = 5,
            Activate = activate,
            Mappings = new List<CreateMappingDto>
            {
                new()
                {
                    SourcePath = "data.assetId",
                    TargetColumn = "Asset ID",
                    TransformType = "DirectMapping"
                },
                new()
                {
                    SourcePath = "data.value",
                    TargetColumn = "Value",
                    TransformType = "UnitConversion",
                    TransformConfig = "{\"from\":\"MWh\",\"to\":\"kWh\",\"factor\":1000}"
                }
            },
            Credentials = new CreateCredentialDto
            {
                ApiKey = "secret-key",
                SftpUsername = "sftp-user",
                SftpPassword = "sftp-pass"
            }
        };
    }

    // ---------------------------------------------------------------
    // Create - activation status
    // ---------------------------------------------------------------

    [Fact]
    public async Task Create_WithActivateTrue_SetsStatusActive()
    {
        var request = BuildValidRequest(activate: true);

        var result = await _sut.Create(request, CancellationToken.None);

        var createdResult = result.Result as CreatedAtActionResult;
        createdResult.Should().NotBeNull();
        var apiResponse = createdResult!.Value as ApiResponse<ConnectionDto>;
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data!.Status.Should().Be(ConnectionStatus.Active);
    }

    [Fact]
    public async Task Create_WithActivateFalse_SetsStatusPaused()
    {
        var request = BuildValidRequest(activate: false);

        var result = await _sut.Create(request, CancellationToken.None);

        var createdResult = result.Result as CreatedAtActionResult;
        createdResult.Should().NotBeNull();
        var apiResponse = createdResult!.Value as ApiResponse<ConnectionDto>;
        apiResponse!.Data!.Status.Should().Be(ConnectionStatus.Paused);
    }

    // ---------------------------------------------------------------
    // Create - mappings persistence
    // ---------------------------------------------------------------

    [Fact]
    public async Task Create_PersistsMappings_WithCorrectCount()
    {
        var request = BuildValidRequest();
        Connection? capturedConnection = null;

        _connectionRepo.Setup(x => x.CreateAsync(It.IsAny<Connection>(), It.IsAny<CancellationToken>()))
            .Callback<Connection, CancellationToken>((c, _) => capturedConnection = c)
            .ReturnsAsync((Connection c, CancellationToken _) =>
            {
                c.Id = Guid.NewGuid();
                return c;
            });

        await _sut.Create(request, CancellationToken.None);

        capturedConnection.Should().NotBeNull();
        capturedConnection!.Mappings.Should().HaveCount(2);

        var mappingsList = capturedConnection.Mappings.ToList();
        mappingsList[0].TargetColumn.Should().Be("Asset ID");
        mappingsList[0].TransformType.Should().Be(TransformType.DirectMapping);
        mappingsList[1].TargetColumn.Should().Be("Value");
        mappingsList[1].TransformType.Should().Be(TransformType.UnitConversion);
    }

    // ---------------------------------------------------------------
    // Create - credential storage
    // ---------------------------------------------------------------

    [Fact]
    public async Task Create_ApiKeyAuth_StoresCorrectCredentials()
    {
        var request = BuildValidRequest();

        await _sut.Create(request, CancellationToken.None);

        // ApiKey auth should store: api-key, sftp-username, sftp-password
        _credentialVaultService.Verify(x => x.StoreCredentialAsync(
            It.Is<StoreCredentialRequest>(r => r.Label == "api-key"),
            It.IsAny<CancellationToken>()), Times.Once);

        _credentialVaultService.Verify(x => x.StoreCredentialAsync(
            It.Is<StoreCredentialRequest>(r => r.Label == "sftp-username"),
            It.IsAny<CancellationToken>()), Times.Once);

        _credentialVaultService.Verify(x => x.StoreCredentialAsync(
            It.Is<StoreCredentialRequest>(r => r.Label == "sftp-password"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---------------------------------------------------------------
    // Create - invalid user
    // ---------------------------------------------------------------

    [Fact]
    public async Task Create_WithInvalidUserId_ReturnsBadRequest()
    {
        _currentUser.Setup(x => x.UserId).Returns("not-a-guid");
        var request = BuildValidRequest();

        var result = await _sut.Create(request, CancellationToken.None);

        var badRequest = result.Result as BadRequestObjectResult;
        badRequest.Should().NotBeNull();
        var apiResponse = badRequest!.Value as ApiResponse<ConnectionDto>;
        apiResponse!.Success.Should().BeFalse();
        apiResponse.Message.Should().Contain("Cannot determine current user");
    }

    // ---------------------------------------------------------------
    // GetAll - with sync summary
    // ---------------------------------------------------------------

    [Fact]
    public async Task GetAll_PopulatesSyncSummary_WhenSyncRunsExist()
    {
        var connectionId = Guid.NewGuid();
        var connection = new Connection
        {
            Id = connectionId,
            Name = "Test",
            BaseUrl = "https://api.example.com",
            ClientName = "acme",
            PlatformName = "energystar"
        };

        var completedAt = new DateTime(2026, 3, 15, 10, 0, 0, DateTimeKind.Utc);
        var syncRun = new SyncRun
        {
            Id = Guid.NewGuid(),
            ConnectionId = connectionId,
            Status = SyncRunStatus.Succeeded,
            CompletedAt = completedAt,
            RecordCount = 247,
            TriggeredBy = "scheduled"
        };

        _connectionRepo.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Connection> { connection });

        _syncRunRepo.Setup(x => x.GetLatestByConnectionIdsAsync(
                It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, SyncRun> { { connectionId, syncRun } });

        _syncRunRepo.Setup(x => x.GetSuccessRatesByConnectionIdsAsync(
                It.IsAny<IEnumerable<Guid>>(), 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, decimal?> { { connectionId, 95.0m } });

        var result = await _sut.GetAll(CancellationToken.None);

        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        var apiResponse = okResult!.Value as ApiResponse<List<ConnectionDto>>;
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data.Should().HaveCount(1);

        var dto = apiResponse.Data![0];
        dto.LastSyncAt.Should().Be(completedAt);
        dto.LastSyncRecordCount.Should().Be(247);
        dto.SuccessRate.Should().Be(95.0m);
    }

    [Fact]
    public async Task GetAll_WithNoSyncRuns_HasNullSummaryFields()
    {
        var connection = new Connection
        {
            Id = Guid.NewGuid(),
            Name = "Test",
            BaseUrl = "https://api.example.com",
            ClientName = "acme",
            PlatformName = "energystar"
        };

        _connectionRepo.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Connection> { connection });

        _syncRunRepo.Setup(x => x.GetLatestByConnectionIdsAsync(
                It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, SyncRun>());

        _syncRunRepo.Setup(x => x.GetSuccessRatesByConnectionIdsAsync(
                It.IsAny<IEnumerable<Guid>>(), 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, decimal?>());

        var result = await _sut.GetAll(CancellationToken.None);

        var okResult = result.Result as OkObjectResult;
        var apiResponse = okResult!.Value as ApiResponse<List<ConnectionDto>>;
        var dto = apiResponse!.Data![0];
        dto.LastSyncAt.Should().BeNull();
        dto.LastSyncRecordCount.Should().BeNull();
        dto.SuccessRate.Should().BeNull();
    }

    // ---------------------------------------------------------------
    // GetById - not found
    // ---------------------------------------------------------------

    [Fact]
    public async Task GetById_NotFound_Returns404()
    {
        var id = Guid.NewGuid();
        _connectionRepo.Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Connection?)null);

        var result = await _sut.GetById(id, CancellationToken.None);

        var notFound = result.Result as NotFoundObjectResult;
        notFound.Should().NotBeNull();
        var apiResponse = notFound!.Value as ApiResponse<ConnectionDto>;
        apiResponse!.Success.Should().BeFalse();
    }

    // ---------------------------------------------------------------
    // Delete - not found
    // ---------------------------------------------------------------

    [Fact]
    public async Task Delete_NotFound_Returns404()
    {
        var id = Guid.NewGuid();
        _connectionRepo.Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Connection?)null);

        var result = await _sut.Delete(id, CancellationToken.None);

        var notFound = result.Result as NotFoundObjectResult;
        notFound.Should().NotBeNull();
    }

    // ---------------------------------------------------------------
    // Create - schedules when active with cron
    // ---------------------------------------------------------------

    [Fact]
    public async Task Create_ActiveWithCron_SchedulesConnection()
    {
        var request = BuildValidRequest(activate: true);
        request.ScheduleCron = "0 5 * * *";

        await _sut.Create(request, CancellationToken.None);

        _schedulerService.Verify(x => x.ScheduleConnectionAsync(
            It.IsAny<Guid>(), "0 5 * * *", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_PausedWithCron_DoesNotSchedule()
    {
        var request = BuildValidRequest(activate: false);
        request.ScheduleCron = "0 5 * * *";

        await _sut.Create(request, CancellationToken.None);

        _schedulerService.Verify(x => x.ScheduleConnectionAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
