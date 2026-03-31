namespace API.Application.Auth;
using API.Core.Interfaces;
using API.Core.Models;
using Microsoft.AspNetCore.Authorization;

public class ConnectionAccessHandler : AuthorizationHandler<ConnectionAccessRequirement>
{
    private readonly ICurrentUserService _currentUser;

    public ConnectionAccessHandler(ICurrentUserService currentUser)
    {
        _currentUser = currentUser;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ConnectionAccessRequirement requirement)
    {
        if (_currentUser.IsAdmin)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // Operators and Viewers need connection-level access checks
        // This will be enhanced when we have the connection repository
        if (_currentUser.IsAuthenticated)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
