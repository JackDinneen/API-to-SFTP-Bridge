namespace UnitTests.Application.Services;

using API.Application.Services;
using API.Core.Entities;
using API.Core.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

public class NotificationServiceTests
{
    private readonly Mock<INotificationConfigRepository> _configRepoMock;
    private readonly Mock<IConnectionRepository> _connectionRepoMock;
    private readonly Mock<ILogger<NotificationService>> _loggerMock;
    private readonly NotificationService _service;

    public NotificationServiceTests()
    {
        _configRepoMock = new Mock<INotificationConfigRepository>();
        _connectionRepoMock = new Mock<IConnectionRepository>();
        _loggerMock = new Mock<ILogger<NotificationService>>();
        _service = new NotificationService(
            _configRepoMock.Object,
            _connectionRepoMock.Object,
            _loggerMock.Object);
    }

    private static readonly Guid TestConnectionId = Guid.NewGuid();

    private NotificationConfig MakeConfig(
        bool onSuccess = true,
        bool onFailure = true,
        bool onWarning = true,
        bool onNewMeter = true)
    {
        return new NotificationConfig
        {
            ConnectionId = TestConnectionId,
            NotifyOnSuccess = onSuccess,
            NotifyOnFailure = onFailure,
            NotifyOnValidationWarning = onWarning,
            NotifyOnNewMeter = onNewMeter,
            EmailRecipients = "test@example.com"
        };
    }

    private void SetupConfig(NotificationConfig? config)
    {
        _configRepoMock
            .Setup(r => r.GetByConnectionIdAsync(TestConnectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        _connectionRepoMock
            .Setup(r => r.GetByIdAsync(TestConnectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Connection { Id = TestConnectionId, Name = "Test Connection", CreatedById = Guid.NewGuid() });
    }

    // ---------------------------------------------------------------
    // SendSyncSuccessAsync
    // ---------------------------------------------------------------

    [Fact]
    public async Task SendSyncSuccessAsync_WhenEnabled_LogsNotification()
    {
        SetupConfig(MakeConfig(onSuccess: true));

        await _service.SendSyncSuccessAsync(TestConnectionId, "test.csv", 42);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("NOTIFICATION") && v.ToString()!.Contains("Sync Succeeded")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendSyncSuccessAsync_WhenDisabled_DoesNotLog()
    {
        SetupConfig(MakeConfig(onSuccess: false));

        await _service.SendSyncSuccessAsync(TestConnectionId, "test.csv", 42);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("NOTIFICATION")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public async Task SendSyncSuccessAsync_WhenNoConfig_DoesNotLog()
    {
        SetupConfig(null);

        await _service.SendSyncSuccessAsync(TestConnectionId, "test.csv", 42);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("NOTIFICATION")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    // ---------------------------------------------------------------
    // SendSyncFailureAsync
    // ---------------------------------------------------------------

    [Fact]
    public async Task SendSyncFailureAsync_WhenEnabled_LogsNotification()
    {
        SetupConfig(MakeConfig(onFailure: true));

        await _service.SendSyncFailureAsync(TestConnectionId, "Connection timeout");

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("NOTIFICATION") && v.ToString()!.Contains("FAILED")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendSyncFailureAsync_WhenDisabled_DoesNotLog()
    {
        SetupConfig(MakeConfig(onFailure: false));

        await _service.SendSyncFailureAsync(TestConnectionId, "Connection timeout");

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("NOTIFICATION")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    // ---------------------------------------------------------------
    // SendValidationWarningAsync
    // ---------------------------------------------------------------

    [Fact]
    public async Task SendValidationWarningAsync_WhenEnabled_LogsNotification()
    {
        SetupConfig(MakeConfig(onWarning: true));

        var report = new ValidationReport
        {
            TotalRows = 10,
            PassedRows = 8,
            WarningRows = 2,
            ErrorRows = 0,
            Rows = new List<RowValidation>
            {
                new() { RowNumber = 1, Status = "Warning", Messages = new List<string> { "Value too high" } }
            }
        };

        await _service.SendValidationWarningAsync(TestConnectionId, report);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("NOTIFICATION") && v.ToString()!.Contains("Validation Warnings")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendValidationWarningAsync_WhenDisabled_DoesNotLog()
    {
        SetupConfig(MakeConfig(onWarning: false));

        var report = new ValidationReport { TotalRows = 1 };

        await _service.SendValidationWarningAsync(TestConnectionId, report);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("NOTIFICATION")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    // ---------------------------------------------------------------
    // SendNewMeterDetectedAsync
    // ---------------------------------------------------------------

    [Fact]
    public async Task SendNewMeterDetectedAsync_WhenEnabled_LogsNotification()
    {
        SetupConfig(MakeConfig(onNewMeter: true));

        var meters = new List<string> { "NEW-METER-001", "NEW-METER-002" };

        await _service.SendNewMeterDetectedAsync(TestConnectionId, meters);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("NOTIFICATION") && v.ToString()!.Contains("New Meters")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendNewMeterDetectedAsync_WhenDisabled_DoesNotLog()
    {
        SetupConfig(MakeConfig(onNewMeter: false));

        await _service.SendNewMeterDetectedAsync(TestConnectionId, new List<string> { "M1" });

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("NOTIFICATION")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }
}
