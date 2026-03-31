using FluentAssertions;
using API.Core.Models;

namespace UnitTests.Core.Models;

public class EnumsTests
{
    [Theory]
    [InlineData(ConnectionStatus.Active, 0)]
    [InlineData(ConnectionStatus.Paused, 1)]
    [InlineData(ConnectionStatus.Error, 2)]
    public void ConnectionStatus_HasExpectedValues(ConnectionStatus status, int expectedValue)
    {
        ((int)status).Should().Be(expectedValue);
    }

    [Fact]
    public void ConnectionStatus_HasExactlyThreeValues()
    {
        Enum.GetValues<ConnectionStatus>().Should().HaveCount(3);
    }

    [Theory]
    [InlineData(AuthType.ApiKey, 0)]
    [InlineData(AuthType.OAuth2ClientCredentials, 1)]
    [InlineData(AuthType.BasicAuth, 2)]
    [InlineData(AuthType.CustomHeaders, 3)]
    public void AuthType_HasExpectedValues(AuthType authType, int expectedValue)
    {
        ((int)authType).Should().Be(expectedValue);
    }

    [Fact]
    public void AuthType_HasExactlyFourValues()
    {
        Enum.GetValues<AuthType>().Should().HaveCount(4);
    }

    [Theory]
    [InlineData(UserRole.Admin, 0)]
    [InlineData(UserRole.Operator, 1)]
    [InlineData(UserRole.Viewer, 2)]
    public void UserRole_HasExpectedValues(UserRole role, int expectedValue)
    {
        ((int)role).Should().Be(expectedValue);
    }

    [Fact]
    public void UserRole_HasExactlyThreeValues()
    {
        Enum.GetValues<UserRole>().Should().HaveCount(3);
    }

    [Theory]
    [InlineData(SyncRunStatus.Pending, 0)]
    [InlineData(SyncRunStatus.Running, 1)]
    [InlineData(SyncRunStatus.Succeeded, 2)]
    [InlineData(SyncRunStatus.Failed, 3)]
    public void SyncRunStatus_HasExpectedValues(SyncRunStatus status, int expectedValue)
    {
        ((int)status).Should().Be(expectedValue);
    }

    [Fact]
    public void SyncRunStatus_HasExactlyFourValues()
    {
        Enum.GetValues<SyncRunStatus>().Should().HaveCount(4);
    }

    [Theory]
    [InlineData(UtilityType.Electricity, 0)]
    [InlineData(UtilityType.Gas, 1)]
    [InlineData(UtilityType.Water, 2)]
    [InlineData(UtilityType.Waste, 3)]
    [InlineData(UtilityType.DistrictHeating, 4)]
    [InlineData(UtilityType.DistrictCooling, 5)]
    public void UtilityType_HasExpectedValues(UtilityType utilityType, int expectedValue)
    {
        ((int)utilityType).Should().Be(expectedValue);
    }

    [Fact]
    public void UtilityType_HasExactlySixValues()
    {
        Enum.GetValues<UtilityType>().Should().HaveCount(6);
    }

    [Theory]
    [InlineData(TransformType.DirectMapping, 0)]
    [InlineData(TransformType.ValueMapping, 1)]
    [InlineData(TransformType.UnitConversion, 2)]
    [InlineData(TransformType.DateParse, 3)]
    [InlineData(TransformType.StaticValue, 4)]
    [InlineData(TransformType.Concatenation, 5)]
    [InlineData(TransformType.Split, 6)]
    public void TransformType_HasExpectedValues(TransformType transformType, int expectedValue)
    {
        ((int)transformType).Should().Be(expectedValue);
    }

    [Fact]
    public void TransformType_HasExactlySevenValues()
    {
        Enum.GetValues<TransformType>().Should().HaveCount(7);
    }

    [Theory]
    [InlineData(AggregationType.Sum, 0)]
    [InlineData(AggregationType.Average, 1)]
    [InlineData(AggregationType.Last, 2)]
    [InlineData(AggregationType.Max, 3)]
    public void AggregationType_HasExpectedValues(AggregationType aggregationType, int expectedValue)
    {
        ((int)aggregationType).Should().Be(expectedValue);
    }

    [Fact]
    public void AggregationType_HasExactlyFourValues()
    {
        Enum.GetValues<AggregationType>().Should().HaveCount(4);
    }
}
