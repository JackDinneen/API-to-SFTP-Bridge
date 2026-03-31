using FluentAssertions;
using Moq;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using API.Application.Services;
using API.Core.Models;

namespace UnitTests.Application.Services;

public class CurrentUserServiceTests
{
    private static CurrentUserService CreateService(ClaimsPrincipal? user = null)
    {
        var httpContext = new DefaultHttpContext();
        if (user != null)
        {
            httpContext.User = user;
        }

        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(x => x.HttpContext).Returns(httpContext);

        return new CurrentUserService(accessor.Object);
    }

    private static ClaimsPrincipal CreatePrincipal(IEnumerable<Claim> claims, string authenticationType = "TestAuth")
    {
        var identity = new ClaimsIdentity(claims, authenticationType);
        return new ClaimsPrincipal(identity);
    }

    // --- Unauthenticated user ---

    [Fact]
    public void UnauthenticatedUser_IsAuthenticated_ReturnsFalse()
    {
        // DefaultHttpContext has an unauthenticated user by default
        var service = CreateService();

        service.IsAuthenticated.Should().BeFalse();
    }

    [Fact]
    public void UnauthenticatedUser_AllProperties_AreNull()
    {
        var service = CreateService();

        service.UserId.Should().BeNull();
        service.Email.Should().BeNull();
        service.DisplayName.Should().BeNull();
        service.Role.Should().BeNull();
    }

    [Fact]
    public void UnauthenticatedUser_RoleChecks_AllFalse()
    {
        var service = CreateService();

        service.IsAdmin.Should().BeFalse();
        service.IsOperator.Should().BeFalse();
        service.IsViewer.Should().BeFalse();
    }

    // --- Null HttpContext ---

    [Fact]
    public void NullHttpContext_IsAuthenticated_ReturnsFalse()
    {
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        var service = new CurrentUserService(accessor.Object);

        service.IsAuthenticated.Should().BeFalse();
        service.UserId.Should().BeNull();
        service.Email.Should().BeNull();
        service.DisplayName.Should().BeNull();
        service.Role.Should().BeNull();
    }

    // --- Authenticated user with standard claims ---

    [Fact]
    public void AuthenticatedUser_WithStandardClaims_ReturnsCorrectUserId()
    {
        var principal = CreatePrincipal(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user-123"),
            new Claim(ClaimTypes.Email, "test@example.com"),
            new Claim(ClaimTypes.Name, "Test User"),
            new Claim(ClaimTypes.Role, "Admin")
        });

        var service = CreateService(principal);

        service.UserId.Should().Be("user-123");
    }

    [Fact]
    public void AuthenticatedUser_WithStandardClaims_ReturnsCorrectEmail()
    {
        var principal = CreatePrincipal(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user-123"),
            new Claim(ClaimTypes.Email, "test@example.com"),
            new Claim(ClaimTypes.Name, "Test User")
        });

        var service = CreateService(principal);

        service.Email.Should().Be("test@example.com");
    }

    [Fact]
    public void AuthenticatedUser_WithStandardClaims_ReturnsCorrectDisplayName()
    {
        var principal = CreatePrincipal(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user-123"),
            new Claim(ClaimTypes.Name, "Test User")
        });

        var service = CreateService(principal);

        service.DisplayName.Should().Be("Test User");
    }

    [Fact]
    public void AuthenticatedUser_IsAuthenticated_ReturnsTrue()
    {
        var principal = CreatePrincipal(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user-123")
        });

        var service = CreateService(principal);

        service.IsAuthenticated.Should().BeTrue();
    }

    // --- Azure AD claims (fallback) ---

    [Fact]
    public void AzureAdUser_WithOidClaim_ReturnsCorrectUserId()
    {
        var principal = CreatePrincipal(new[]
        {
            new Claim("oid", "azure-object-id-456")
        });

        var service = CreateService(principal);

        service.UserId.Should().Be("azure-object-id-456");
    }

    [Fact]
    public void AzureAdUser_WithPreferredUsername_ReturnsCorrectEmail()
    {
        var principal = CreatePrincipal(new[]
        {
            new Claim("preferred_username", "azure.user@company.com")
        });

        var service = CreateService(principal);

        service.Email.Should().Be("azure.user@company.com");
    }

    [Fact]
    public void AzureAdUser_WithNameClaim_ReturnsCorrectDisplayName()
    {
        var principal = CreatePrincipal(new[]
        {
            new Claim("name", "Azure User")
        });

        var service = CreateService(principal);

        service.DisplayName.Should().Be("Azure User");
    }

    [Fact]
    public void AzureAdUser_StandardClaimsTakePrecedence_OverAzureClaims()
    {
        var principal = CreatePrincipal(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "standard-id"),
            new Claim("oid", "azure-id"),
            new Claim(ClaimTypes.Email, "standard@example.com"),
            new Claim("preferred_username", "azure@example.com"),
            new Claim(ClaimTypes.Name, "Standard Name"),
            new Claim("name", "Azure Name")
        });

        var service = CreateService(principal);

        service.UserId.Should().Be("standard-id");
        service.Email.Should().Be("standard@example.com");
        service.DisplayName.Should().Be("Standard Name");
    }

    // --- Role tests ---

    [Fact]
    public void AdminRole_IsAdmin_ReturnsTrue()
    {
        var principal = CreatePrincipal(new[]
        {
            new Claim(ClaimTypes.Role, "Admin")
        });

        var service = CreateService(principal);

        service.Role.Should().Be(UserRole.Admin);
        service.IsAdmin.Should().BeTrue();
        service.IsOperator.Should().BeFalse();
        service.IsViewer.Should().BeFalse();
    }

    [Fact]
    public void OperatorRole_IsOperator_ReturnsTrue()
    {
        var principal = CreatePrincipal(new[]
        {
            new Claim(ClaimTypes.Role, "Operator")
        });

        var service = CreateService(principal);

        service.Role.Should().Be(UserRole.Operator);
        service.IsOperator.Should().BeTrue();
        service.IsAdmin.Should().BeFalse();
        service.IsViewer.Should().BeFalse();
    }

    [Fact]
    public void ViewerRole_IsViewer_ReturnsTrue()
    {
        var principal = CreatePrincipal(new[]
        {
            new Claim(ClaimTypes.Role, "Viewer")
        });

        var service = CreateService(principal);

        service.Role.Should().Be(UserRole.Viewer);
        service.IsViewer.Should().BeTrue();
        service.IsAdmin.Should().BeFalse();
        service.IsOperator.Should().BeFalse();
    }

    [Fact]
    public void NoRoleClaim_Role_ReturnsNull()
    {
        var principal = CreatePrincipal(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user-123")
        });

        var service = CreateService(principal);

        service.Role.Should().BeNull();
        service.IsAdmin.Should().BeFalse();
        service.IsOperator.Should().BeFalse();
        service.IsViewer.Should().BeFalse();
    }

    [Theory]
    [InlineData("admin", UserRole.Admin)]
    [InlineData("ADMIN", UserRole.Admin)]
    [InlineData("Admin", UserRole.Admin)]
    [InlineData("operator", UserRole.Operator)]
    [InlineData("OPERATOR", UserRole.Operator)]
    [InlineData("viewer", UserRole.Viewer)]
    [InlineData("VIEWER", UserRole.Viewer)]
    public void Role_CaseInsensitiveParsing_ReturnsCorrectRole(string roleClaim, UserRole expectedRole)
    {
        var principal = CreatePrincipal(new[]
        {
            new Claim(ClaimTypes.Role, roleClaim)
        });

        var service = CreateService(principal);

        service.Role.Should().Be(expectedRole);
    }

    [Fact]
    public void InvalidRoleClaim_Role_ReturnsNull()
    {
        var principal = CreatePrincipal(new[]
        {
            new Claim(ClaimTypes.Role, "SuperAdmin")
        });

        var service = CreateService(principal);

        service.Role.Should().BeNull();
    }

    [Fact]
    public void AzureAdExtensionRole_FallsBackCorrectly()
    {
        var principal = CreatePrincipal(new[]
        {
            new Claim("extension_Role", "Operator")
        });

        var service = CreateService(principal);

        service.Role.Should().Be(UserRole.Operator);
        service.IsOperator.Should().BeTrue();
    }
}
