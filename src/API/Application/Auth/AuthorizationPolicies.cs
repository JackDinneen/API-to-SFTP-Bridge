namespace API.Application.Auth;
using Microsoft.AspNetCore.Authorization;

public static class AuthorizationPolicies
{
    public const string AdminOnly = "AdminOnly";
    public const string AdminOrOperator = "AdminOrOperator";
    public const string AllAuthenticated = "AllAuthenticated";

    public static void ConfigurePolicies(AuthorizationOptions options)
    {
        options.AddPolicy(AdminOnly, policy =>
            policy.RequireRole("Admin"));

        options.AddPolicy(AdminOrOperator, policy =>
            policy.RequireRole("Admin", "Operator"));

        options.AddPolicy(AllAuthenticated, policy =>
            policy.RequireAuthenticatedUser());
    }
}
