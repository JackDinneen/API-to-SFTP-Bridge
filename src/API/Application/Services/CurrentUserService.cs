namespace API.Application.Services;
using API.Core.Interfaces;
using API.Core.Models;
using System.Security.Claims;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public string? UserId => User?.FindFirstValue(ClaimTypes.NameIdentifier)
                             ?? User?.FindFirstValue("oid"); // Azure AD object ID

    public string? Email => User?.FindFirstValue(ClaimTypes.Email)
                            ?? User?.FindFirstValue("preferred_username");

    public string? DisplayName => User?.FindFirstValue(ClaimTypes.Name)
                                  ?? User?.FindFirstValue("name");

    public UserRole? Role
    {
        get
        {
            var roleClaim = User?.FindFirstValue(ClaimTypes.Role)
                            ?? User?.FindFirstValue("extension_Role");
            return Enum.TryParse<UserRole>(roleClaim, true, out var role) ? role : null;
        }
    }

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;
    public bool IsAdmin => Role == UserRole.Admin;
    public bool IsOperator => Role == UserRole.Operator;
    public bool IsViewer => Role == UserRole.Viewer;
}
