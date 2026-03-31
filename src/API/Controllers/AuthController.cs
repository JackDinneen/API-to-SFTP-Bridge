using API.Core.Interfaces;
using API.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AuthController : ControllerBase
{
    private readonly ICurrentUserService _currentUser;

    public AuthController(ICurrentUserService currentUser)
    {
        _currentUser = currentUser;
    }

    [HttpGet("me")]
    public ActionResult<ApiResponse<object>> GetCurrentUser()
    {
        if (!_currentUser.IsAuthenticated)
            return Unauthorized(ApiResponse<object>.Fail("Not authenticated"));

        var profile = new
        {
            _currentUser.UserId,
            _currentUser.Email,
            _currentUser.DisplayName,
            Role = _currentUser.Role?.ToString()
        };

        return Ok(ApiResponse<object>.Ok(profile));
    }
}
