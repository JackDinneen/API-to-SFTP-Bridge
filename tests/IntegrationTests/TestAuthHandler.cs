using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IntegrationTests;

public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "TestScheme";

    /// <summary>
    /// Set to false to simulate an unauthenticated request.
    /// </summary>
    public static bool Authenticate { get; set; } = true;

    /// <summary>
    /// Role claim value for the test user (e.g. "Admin", "Operator", "Viewer").
    /// </summary>
    public static string Role { get; set; } = "Admin";

    /// <summary>
    /// User ID for the test user.
    /// </summary>
    public static string UserId { get; set; } = "00000000-0000-0000-0000-000000000001";

    /// <summary>
    /// Email claim for the test user.
    /// </summary>
    public static string Email { get; set; } = "test@obibridge.com";

    /// <summary>
    /// Display name for the test user.
    /// </summary>
    public static string DisplayName { get; set; } = "Test User";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Authenticate)
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, UserId),
            new(ClaimTypes.Email, Email),
            new(ClaimTypes.Name, DisplayName),
            new(ClaimTypes.Role, Role),
            new("name", DisplayName),
            new("preferred_username", Email),
            // Azure AD-style object ID claim
            new("http://schemas.microsoft.com/identity/claims/objectidentifier", UserId)
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    /// <summary>
    /// Reset all settings to defaults between tests.
    /// </summary>
    public static void Reset()
    {
        Authenticate = true;
        Role = "Admin";
        UserId = "00000000-0000-0000-0000-000000000001";
        Email = "test@obibridge.com";
        DisplayName = "Test User";
    }
}
