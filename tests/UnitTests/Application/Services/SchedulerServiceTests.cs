namespace UnitTests.Application.Services;

using System.Linq.Expressions;
using API.Application.Services;
using API.Core.Interfaces;
using FluentAssertions;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Microsoft.Extensions.Logging;
using Moq;

public class SchedulerServiceTests
{
    private readonly Mock<IRecurringJobManager> _recurringJobManager;
    private readonly Mock<IBackgroundJobClient> _backgroundJobClient;
    private readonly SchedulerService _sut;

    public SchedulerServiceTests()
    {
        _recurringJobManager = new Mock<IRecurringJobManager>();
        _backgroundJobClient = new Mock<IBackgroundJobClient>();

        _sut = new SchedulerService(
            _recurringJobManager.Object,
            _backgroundJobClient.Object,
            Mock.Of<ILogger<SchedulerService>>());
    }

    // ---------------------------------------------------------------
    // ScheduleConnectionAsync
    // ---------------------------------------------------------------

    [Fact]
    public async Task ScheduleConnectionAsync_CreatesRecurringJob()
    {
        var connectionId = Guid.NewGuid();
        var cron = "0 5 * * *"; // Daily at 5am

        await _sut.ScheduleConnectionAsync(connectionId, cron);

        _recurringJobManager.Verify(m => m.AddOrUpdate(
            $"sync-{connectionId}",
            It.IsAny<Job>(),
            cron,
            It.IsAny<RecurringJobOptions>()
        ), Times.Once);
    }

    // ---------------------------------------------------------------
    // UnscheduleConnectionAsync
    // ---------------------------------------------------------------

    [Fact]
    public async Task UnscheduleConnectionAsync_RemovesRecurringJob()
    {
        var connectionId = Guid.NewGuid();

        await _sut.UnscheduleConnectionAsync(connectionId);

        _recurringJobManager.Verify(m => m.RemoveIfExists($"sync-{connectionId}"), Times.Once);
    }

    // ---------------------------------------------------------------
    // TriggerManualSyncAsync
    // ---------------------------------------------------------------

    [Fact]
    public async Task TriggerManualSyncAsync_EnqueuesBackgroundJob()
    {
        var connectionId = Guid.NewGuid();

        _backgroundJobClient.Setup(c => c.Create(It.IsAny<Job>(), It.IsAny<IState>()))
            .Returns("job-123");

        var jobId = await _sut.TriggerManualSyncAsync(connectionId, "admin@example.com");

        jobId.Should().Be("job-123");
        _backgroundJobClient.Verify(c => c.Create(It.IsAny<Job>(), It.IsAny<EnqueuedState>()), Times.Once);
    }
}
