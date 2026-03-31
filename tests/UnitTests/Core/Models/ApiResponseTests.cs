using FluentAssertions;
using API.Core.Models;

namespace UnitTests.Core.Models;

public class ApiResponseTests
{
    [Fact]
    public void Ok_ReturnsSuccessTrue_WithCorrectData()
    {
        // Arrange
        var data = "hello";

        // Act
        var response = ApiResponse<string>.Ok(data);

        // Assert
        response.Success.Should().BeTrue();
        response.Data.Should().Be("hello");
        response.Message.Should().BeNull();
        response.Errors.Should().BeNull();
    }

    [Fact]
    public void Ok_WithMessage_SetsMessage()
    {
        // Arrange & Act
        var response = ApiResponse<int>.Ok(42, "Operation completed");

        // Assert
        response.Success.Should().BeTrue();
        response.Data.Should().Be(42);
        response.Message.Should().Be("Operation completed");
    }

    [Fact]
    public void Ok_WithComplexType_ReturnsCorrectData()
    {
        // Arrange
        var data = new List<string> { "a", "b", "c" };

        // Act
        var response = ApiResponse<List<string>>.Ok(data);

        // Assert
        response.Success.Should().BeTrue();
        response.Data.Should().BeEquivalentTo(new[] { "a", "b", "c" });
    }

    [Fact]
    public void Fail_ReturnsSuccessFalse_WithMessage()
    {
        // Act
        var response = ApiResponse<string>.Fail("Something went wrong");

        // Assert
        response.Success.Should().BeFalse();
        response.Message.Should().Be("Something went wrong");
        response.Data.Should().BeNull();
        response.Errors.Should().BeNull();
    }

    [Fact]
    public void Fail_WithErrors_IncludesErrorsList()
    {
        // Arrange
        var errors = new List<string> { "Field X is required", "Field Y is invalid" };

        // Act
        var response = ApiResponse<string>.Fail("Validation failed", errors);

        // Assert
        response.Success.Should().BeFalse();
        response.Message.Should().Be("Validation failed");
        response.Errors.Should().HaveCount(2);
        response.Errors.Should().Contain("Field X is required");
        response.Errors.Should().Contain("Field Y is invalid");
    }

    [Fact]
    public void Ok_WithNullMessage_MessageIsNull()
    {
        // Act
        var response = ApiResponse<string>.Ok("data", null);

        // Assert
        response.Success.Should().BeTrue();
        response.Message.Should().BeNull();
    }
}
