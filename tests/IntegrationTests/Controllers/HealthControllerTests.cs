using System.Net;
using FluentAssertions;

namespace IntegrationTests.Controllers;

[Collection("Integration")]
public class HealthControllerTests
{
    private readonly CustomWebApplicationFactory _factory;

    public HealthControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetHealth_ReturnsOk_WithoutAuthentication()
    {
        // Arrange - disable auth to prove anonymous access works
        TestAuthHandler.Reset();
        TestAuthHandler.Authenticate = false;

        var client = _factory.CreateClient();

        try
        {
            // Act
            var response = await client.GetAsync("/api/health");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("Healthy");
        }
        finally
        {
            TestAuthHandler.Reset();
        }
    }
}
