namespace UnitTests.Controllers;

using API.Controllers;
using API.Core.DTOs;
using API.Core.Entities;
using API.Core.Interfaces;
using API.Core.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

public class SyncControllerTests
{
    private readonly Mock<ISyncRunRepository> _syncRunRepo;
    private readonly Mock<IConnectionRepository> _connectionRepo;
    private readonly SyncController _sut;

    private static readonly Guid TestConnectionId = Guid.NewGuid();

    public SyncControllerTests()
    {
        _syncRunRepo = new Mock<ISyncRunRepository>();
        _connectionRepo = new Mock<IConnectionRepository>();

        _sut = new SyncController(_syncRunRepo.Object, _connectionRepo.Object);
    }

    private static Connection BuildTestConnection()
    {
        return new Connection
        {
            Id = TestConnectionId,
            Name = "Test Connection",
            BaseUrl = "https://api.example.com",
            ClientName = "acme",
            PlatformName = "energystar"
        };
    }

    private static SyncRun BuildTestSyncRun(bool withRecords = false)
    {
        var syncRun = new SyncRun
        {
            Id = Guid.NewGuid(),
            ConnectionId = TestConnectionId,
            Status = SyncRunStatus.Succeeded,
            CompletedAt = new DateTime(2026, 3, 15, 10, 0, 0, DateTimeKind.Utc),
            RecordCount = 3,
            FileSize = 1024,
            FileName = "acme_energystar_15032026.csv",
            TriggeredBy = "scheduled",
            RetryCount = 0
        };

        if (withRecords)
        {
            syncRun.Records = new List<SyncRunRecord>
            {
                new()
                {
                    SyncRunId = syncRun.Id,
                    AssetId = "AST-001",
                    AssetName = "Tower A",
                    SubmeterCode = "SM-EL-001",
                    UtilityType = "Electricity",
                    Year = 2026,
                    Month = 2,
                    Value = 14523.50m,
                    IsValid = true,
                    ValidationMessage = null
                },
                new()
                {
                    SyncRunId = syncRun.Id,
                    AssetId = "AST-002",
                    AssetName = "Building B",
                    SubmeterCode = "SM-WT-001",
                    UtilityType = "Water",
                    Year = 2026,
                    Month = 2,
                    Value = 342.75m,
                    IsValid = true,
                    ValidationMessage = null
                },
                new()
                {
                    SyncRunId = syncRun.Id,
                    AssetId = "AST-003",
                    AssetName = "Building C",
                    SubmeterCode = "SM-GS-001",
                    UtilityType = "Gas",
                    Year = 2026,
                    Month = 2,
                    Value = 8234.00m,
                    IsValid = false,
                    ValidationMessage = "Asset ID not found in reference data"
                }
            };
        }

        return syncRun;
    }

    // ---------------------------------------------------------------
    // GetLatest - includeRecords = true
    // ---------------------------------------------------------------

    [Fact]
    public async Task GetLatest_WithIncludeRecords_ReturnsRecordsList()
    {
        var syncRun = BuildTestSyncRun(withRecords: false);
        var syncRunWithRecords = BuildTestSyncRun(withRecords: true);
        syncRunWithRecords.Id = syncRun.Id;

        _connectionRepo.Setup(x => x.GetByIdAsync(TestConnectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildTestConnection());

        _syncRunRepo.Setup(x => x.GetByConnectionIdAsync(TestConnectionId, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SyncRun> { syncRun });

        _syncRunRepo.Setup(x => x.GetByIdWithRecordsAsync(syncRun.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(syncRunWithRecords);

        var result = await _sut.GetLatest(TestConnectionId, includeRecords: true, CancellationToken.None);

        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        var apiResponse = okResult!.Value as ApiResponse<SyncRunDto>;
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data!.Records.Should().NotBeNull();
        apiResponse.Data.Records.Should().HaveCount(3);

        var firstRecord = apiResponse.Data.Records![0];
        firstRecord.AssetId.Should().Be("AST-001");
        firstRecord.AssetName.Should().Be("Tower A");
        firstRecord.SubmeterCode.Should().Be("SM-EL-001");
        firstRecord.UtilityType.Should().Be("Electricity");
        firstRecord.Year.Should().Be(2026);
        firstRecord.Month.Should().Be(2);
        firstRecord.Value.Should().Be(14523.50m);
        firstRecord.IsValid.Should().BeTrue();

        var invalidRecord = apiResponse.Data.Records[2];
        invalidRecord.IsValid.Should().BeFalse();
        invalidRecord.ValidationMessage.Should().Be("Asset ID not found in reference data");
    }

    // ---------------------------------------------------------------
    // GetLatest - includeRecords = false
    // ---------------------------------------------------------------

    [Fact]
    public async Task GetLatest_WithoutIncludeRecords_ReturnsNullRecords()
    {
        var syncRun = BuildTestSyncRun(withRecords: false);

        _connectionRepo.Setup(x => x.GetByIdAsync(TestConnectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildTestConnection());

        _syncRunRepo.Setup(x => x.GetByConnectionIdAsync(TestConnectionId, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SyncRun> { syncRun });

        var result = await _sut.GetLatest(TestConnectionId, includeRecords: false, CancellationToken.None);

        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        var apiResponse = okResult!.Value as ApiResponse<SyncRunDto>;
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data!.Records.Should().BeNull();

        // Core fields should still be populated
        apiResponse.Data.Status.Should().Be(SyncRunStatus.Succeeded);
        apiResponse.Data.RecordCount.Should().Be(3);
        apiResponse.Data.FileName.Should().Be("acme_energystar_15032026.csv");
        apiResponse.Data.TriggeredBy.Should().Be("scheduled");
    }

    // ---------------------------------------------------------------
    // GetLatest - no sync runs
    // ---------------------------------------------------------------

    [Fact]
    public async Task GetLatest_NoSyncRuns_Returns404()
    {
        _connectionRepo.Setup(x => x.GetByIdAsync(TestConnectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildTestConnection());

        _syncRunRepo.Setup(x => x.GetByConnectionIdAsync(TestConnectionId, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SyncRun>());

        var result = await _sut.GetLatest(TestConnectionId, includeRecords: false, CancellationToken.None);

        var notFound = result.Result as NotFoundObjectResult;
        notFound.Should().NotBeNull();
        var apiResponse = notFound!.Value as ApiResponse<SyncRunDto>;
        apiResponse!.Success.Should().BeFalse();
        apiResponse.Message.Should().Contain("No sync runs found");
    }

    // ---------------------------------------------------------------
    // GetLatest - connection not found
    // ---------------------------------------------------------------

    [Fact]
    public async Task GetLatest_ConnectionNotFound_Returns404()
    {
        var unknownId = Guid.NewGuid();
        _connectionRepo.Setup(x => x.GetByIdAsync(unknownId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Connection?)null);

        var result = await _sut.GetLatest(unknownId, includeRecords: false, CancellationToken.None);

        var notFound = result.Result as NotFoundObjectResult;
        notFound.Should().NotBeNull();
        var apiResponse = notFound!.Value as ApiResponse<SyncRunDto>;
        apiResponse!.Success.Should().BeFalse();
        apiResponse.Message.Should().Contain("not found");
    }

    // ---------------------------------------------------------------
    // GetHistory - happy path
    // ---------------------------------------------------------------

    [Fact]
    public async Task GetHistory_ReturnsListOfSyncRuns()
    {
        _connectionRepo.Setup(x => x.GetByIdAsync(TestConnectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildTestConnection());

        var runs = new List<SyncRun>
        {
            BuildTestSyncRun(),
            BuildTestSyncRun()
        };

        _syncRunRepo.Setup(x => x.GetByConnectionIdAsync(TestConnectionId, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(runs);

        var result = await _sut.GetHistory(TestConnectionId, 50, CancellationToken.None);

        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        var apiResponse = okResult!.Value as ApiResponse<List<SyncRunDto>>;
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data.Should().HaveCount(2);
    }

    // ---------------------------------------------------------------
    // GetHistory - connection not found
    // ---------------------------------------------------------------

    [Fact]
    public async Task GetHistory_ConnectionNotFound_Returns404()
    {
        var unknownId = Guid.NewGuid();
        _connectionRepo.Setup(x => x.GetByIdAsync(unknownId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Connection?)null);

        var result = await _sut.GetHistory(unknownId, 50, CancellationToken.None);

        var notFound = result.Result as NotFoundObjectResult;
        notFound.Should().NotBeNull();
    }
}
