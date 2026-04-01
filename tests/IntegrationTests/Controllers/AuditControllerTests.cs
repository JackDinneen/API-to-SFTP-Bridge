using System.Net;
using FluentAssertions;

namespace IntegrationTests.Controllers;

[Collection("Integration")]
public class AuditControllerTests
{
    private readonly CustomWebApplicationFactory _factory;

    public AuditControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAll_AdminRole_Returns200()
    {
        // Arrange
        TestAuthHandler.Reset();
        TestAuthHandler.Role = "Admin";
        var client = _factory.CreateClient();

        try
        {
            // Act
            var response = await client.GetAsync("/api/audit");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
        finally
        {
            TestAuthHandler.Reset();
        }
    }

    [Fact]
    public async Task GetAll_OperatorRole_Returns403()
    {
        // Arrange
        TestAuthHandler.Reset();
        TestAuthHandler.Role = "Operator";
        var client = _factory.CreateClient();

        try
        {
            // Act
            var response = await client.GetAsync("/api/audit");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }
        finally
        {
            TestAuthHandler.Reset();
        }
    }

    [Fact]
    public async Task GetAll_ViewerRole_Returns403()
    {
        // Arrange
        TestAuthHandler.Reset();
        TestAuthHandler.Role = "Viewer";
        var client = _factory.CreateClient();

        try
        {
            // Act
            var response = await client.GetAsync("/api/audit");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }
        finally
        {
            TestAuthHandler.Reset();
        }
    }
}
