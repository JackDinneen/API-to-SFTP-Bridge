using FluentAssertions;
using API.Core.Entities;
using API.Core.Models;

namespace UnitTests.Core.Entities;

// Concrete subclass for testing the abstract BaseEntity
file class TestEntity : BaseEntity { }

public class BaseEntityTests
{
    [Fact]
    public void BaseEntity_SetsDefaultId_NonEmptyGuid()
    {
        var entity = new TestEntity();

        entity.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void BaseEntity_TwoInstances_HaveDifferentIds()
    {
        var entity1 = new TestEntity();
        var entity2 = new TestEntity();

        entity1.Id.Should().NotBe(entity2.Id);
    }

    [Fact]
    public void BaseEntity_SetsCreatedAt_ToApproximatelyNow()
    {
        var before = DateTime.UtcNow;
        var entity = new TestEntity();
        var after = DateTime.UtcNow;

        entity.CreatedAt.Should().BeOnOrAfter(before);
        entity.CreatedAt.Should().BeOnOrBefore(after);
    }

    [Fact]
    public void BaseEntity_SetsUpdatedAt_ToApproximatelyNow()
    {
        var before = DateTime.UtcNow;
        var entity = new TestEntity();
        var after = DateTime.UtcNow;

        entity.UpdatedAt.Should().BeOnOrAfter(before);
        entity.UpdatedAt.Should().BeOnOrBefore(after);
    }

    [Fact]
    public void BaseEntity_IsDeleted_DefaultsToFalse()
    {
        var entity = new TestEntity();

        entity.IsDeleted.Should().BeFalse();
    }
}

public class ConnectionTests
{
    [Fact]
    public void Connection_Status_DefaultsToPaused()
    {
        var connection = new Connection();

        connection.Status.Should().Be(ConnectionStatus.Paused);
    }

    [Fact]
    public void Connection_SftpPort_DefaultsTo22()
    {
        var connection = new Connection();

        connection.SftpPort.Should().Be(22);
    }

    [Fact]
    public void Connection_ReportingLagDays_DefaultsTo5()
    {
        var connection = new Connection();

        connection.ReportingLagDays.Should().Be(5);
    }

    [Fact]
    public void Connection_Name_DefaultsToEmptyString()
    {
        var connection = new Connection();

        connection.Name.Should().BeEmpty();
    }

    [Fact]
    public void Connection_InheritsBaseEntity_HasId()
    {
        var connection = new Connection();

        connection.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Connection_Collections_DefaultToEmptyLists()
    {
        var connection = new Connection();

        connection.Credentials.Should().BeEmpty();
        connection.Mappings.Should().BeEmpty();
        connection.SyncRuns.Should().BeEmpty();
    }
}

public class SyncRunTests
{
    [Fact]
    public void SyncRun_Status_DefaultsToPending()
    {
        var syncRun = new SyncRun();

        syncRun.Status.Should().Be(SyncRunStatus.Pending);
    }

    [Fact]
    public void SyncRun_RetryCount_DefaultsToZero()
    {
        var syncRun = new SyncRun();

        syncRun.RetryCount.Should().Be(0);
    }

    [Fact]
    public void SyncRun_CompletedAt_DefaultsToNull()
    {
        var syncRun = new SyncRun();

        syncRun.CompletedAt.Should().BeNull();
    }

    [Fact]
    public void SyncRun_InheritsBaseEntity_HasId()
    {
        var syncRun = new SyncRun();

        syncRun.Id.Should().NotBeEmpty();
    }
}

public class NotificationConfigTests
{
    [Fact]
    public void NotificationConfig_NotifyOnSuccess_DefaultsToTrue()
    {
        var config = new NotificationConfig();

        config.NotifyOnSuccess.Should().BeTrue();
    }

    [Fact]
    public void NotificationConfig_NotifyOnFailure_DefaultsToTrue()
    {
        var config = new NotificationConfig();

        config.NotifyOnFailure.Should().BeTrue();
    }

    [Fact]
    public void NotificationConfig_NotifyOnValidationWarning_DefaultsToTrue()
    {
        var config = new NotificationConfig();

        config.NotifyOnValidationWarning.Should().BeTrue();
    }

    [Fact]
    public void NotificationConfig_NotifyOnNewMeter_DefaultsToTrue()
    {
        var config = new NotificationConfig();

        config.NotifyOnNewMeter.Should().BeTrue();
    }

    [Fact]
    public void NotificationConfig_InheritsBaseEntity_HasId()
    {
        var config = new NotificationConfig();

        config.Id.Should().NotBeEmpty();
    }
}

public class ConnectionMappingTests
{
    [Fact]
    public void ConnectionMapping_TransformType_DefaultsToDirectMapping()
    {
        var mapping = new ConnectionMapping();

        mapping.TransformType.Should().Be(TransformType.DirectMapping);
    }

    [Fact]
    public void ConnectionMapping_SourcePath_DefaultsToEmptyString()
    {
        var mapping = new ConnectionMapping();

        mapping.SourcePath.Should().BeEmpty();
    }

    [Fact]
    public void ConnectionMapping_TargetColumn_DefaultsToEmptyString()
    {
        var mapping = new ConnectionMapping();

        mapping.TargetColumn.Should().BeEmpty();
    }

    [Fact]
    public void ConnectionMapping_InheritsBaseEntity_HasId()
    {
        var mapping = new ConnectionMapping();

        mapping.Id.Should().NotBeEmpty();
    }
}
