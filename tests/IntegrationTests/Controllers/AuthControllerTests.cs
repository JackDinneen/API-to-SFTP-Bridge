using System.Net;
using FluentAssertions;

namespace IntegrationTests.Controllers;

[Collection("Integration")]
public class AuthControllerTests
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetMe_Authenticated_ReturnsUserProfile()
    {
        // Arrange
        TestAuthHandler.Reset();
        TestAuthHandler.Role = "Admin";
        TestAuthHandler.Email = "admin@obibridge.com";
        TestAuthHandler.DisplayName = "Admin User";

        var client = _factory.CreateClient();

        try
        {
            // Act
            var response = await client.GetAsync("/api/auth/me");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("admin@obibridge.com");
        }
        finally
        {
            TestAuthHandler.Reset();
        }
    }

    [Fact]
    public async Task GetMe_Unauthenticated_Returns401()
    {
        // Arrange
        TestAuthHandler.Reset();
        TestAuthHandler.Authenticate = false;

        var client = _factory.CreateClient();

        try
        {
            // Act
            var response = await client.GetAsync("/api/auth/me");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
        finally
        {
            TestAuthHandler.Reset();
        }
    }
}
