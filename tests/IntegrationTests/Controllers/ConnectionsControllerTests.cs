using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using API.Core.DTOs;
using API.Core.Models;
using FluentAssertions;

namespace IntegrationTests.Controllers;

[Collection("Integration")]
public class ConnectionsControllerTests
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ConnectionsControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAll_Authenticated_Returns200()
    {
        // Arrange
        TestAuthHandler.Reset();
        TestAuthHandler.Role = "Admin";
        var client = _factory.CreateClient();

        try
        {
            // Act
            var response = await client.GetAsync("/api/connections");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<List<ConnectionDto>>>(content, _jsonOptions);
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
        }
        finally
        {
            TestAuthHandler.Reset();
        }
    }

    [Fact]
    public async Task GetAll_Unauthenticated_Returns401()
    {
        // Arrange
        TestAuthHandler.Reset();
        TestAuthHandler.Authenticate = false;
        var client = _factory.CreateClient();

        try
        {
            // Act
            var response = await client.GetAsync("/api/connections");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
        finally
        {
            TestAuthHandler.Reset();
        }
    }

    [Fact]
    public async Task Create_AdminRole_ReturnsCreated()
    {
        // Arrange
        TestAuthHandler.Reset();
        TestAuthHandler.Role = "Admin";
        var client = _factory.CreateClient();

        var request = new CreateConnectionRequest
        {
            Name = "Test Connection",
            BaseUrl = "https://api.example.com",
            AuthType = AuthType.ApiKey,
            ClientName = "testclient",
            PlatformName = "TestPlatform",
            Mappings = new List<CreateMappingDto>
            {
                new()
                {
                    SourcePath = "data.assetId",
                    TargetColumn = "Asset ID",
                    TransformType = "DirectMapping"
                }
            },
            Credentials = new CreateCredentialDto
            {
                ApiKey = "test-key-123"
            }
        };

        try
        {
            // Act
            var response = await client.PostAsJsonAsync("/api/connections", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<ConnectionDto>>(content, _jsonOptions);
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data!.Name.Should().Be("Test Connection");
            result.Data.ClientName.Should().Be("testclient");
        }
        finally
        {
            TestAuthHandler.Reset();
        }
    }

    [Fact]
    public async Task Create_ViewerRole_Returns403()
    {
        // Arrange
        TestAuthHandler.Reset();
        TestAuthHandler.Role = "Viewer";
        var client = _factory.CreateClient();

        var request = new CreateConnectionRequest
        {
            Name = "Should Not Create",
            BaseUrl = "https://api.example.com",
            AuthType = AuthType.ApiKey,
            ClientName = "testclient",
            PlatformName = "TestPlatform"
        };

        try
        {
            // Act
            var response = await client.PostAsJsonAsync("/api/connections", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }
        finally
        {
            TestAuthHandler.Reset();
        }
    }

    [Fact]
    public async Task GetById_NonExistent_Returns404()
    {
        // Arrange
        TestAuthHandler.Reset();
        TestAuthHandler.Role = "Admin";
        var client = _factory.CreateClient();

        var nonExistentId = Guid.NewGuid();

        try
        {
            // Act
            var response = await client.GetAsync($"/api/connections/{nonExistentId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
        finally
        {
            TestAuthHandler.Reset();
        }
    }

    [Fact]
    public async Task Delete_OperatorRole_Returns403()
    {
        // Arrange
        TestAuthHandler.Reset();
        TestAuthHandler.Role = "Operator";
        var client = _factory.CreateClient();

        var connectionId = Guid.NewGuid();

        try
        {
            // Act
            var response = await client.DeleteAsync($"/api/connections/{connectionId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }
        finally
        {
            TestAuthHandler.Reset();
        }
    }
}
