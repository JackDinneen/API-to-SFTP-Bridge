using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using API.Application.Auth;

namespace UnitTests.Application.Auth;

public class AuthorizationPoliciesTests
{
    [Fact]
    public void AdminOnly_PolicyNameConstant_IsCorrect()
    {
        AuthorizationPolicies.AdminOnly.Should().Be("AdminOnly");
    }

    [Fact]
    public void AdminOrOperator_PolicyNameConstant_IsCorrect()
    {
        AuthorizationPolicies.AdminOrOperator.Should().Be("AdminOrOperator");
    }

    [Fact]
    public void AllAuthenticated_PolicyNameConstant_IsCorrect()
    {
        AuthorizationPolicies.AllAuthenticated.Should().Be("AllAuthenticated");
    }

    [Fact]
    public void ConfigurePolicies_AddsAdminOnlyPolicy()
    {
        var options = new AuthorizationOptions();

        AuthorizationPolicies.ConfigurePolicies(options);

        var policy = options.GetPolicy("AdminOnly");
        policy.Should().NotBeNull();
        policy!.Requirements.Should().ContainSingle()
            .Which.Should().BeOfType<RolesAuthorizationRequirement>();
    }

    [Fact]
    public void ConfigurePolicies_AddsAdminOrOperatorPolicy()
    {
        var options = new AuthorizationOptions();

        AuthorizationPolicies.ConfigurePolicies(options);

        var policy = options.GetPolicy("AdminOrOperator");
        policy.Should().NotBeNull();
        policy!.Requirements.Should().ContainSingle()
            .Which.Should().BeOfType<RolesAuthorizationRequirement>();
    }

    [Fact]
    public void ConfigurePolicies_AddsAllAuthenticatedPolicy()
    {
        var options = new AuthorizationOptions();

        AuthorizationPolicies.ConfigurePolicies(options);

        var policy = options.GetPolicy("AllAuthenticated");
        policy.Should().NotBeNull();
        policy!.Requirements.Should().ContainSingle()
            .Which.Should().BeOfType<DenyAnonymousAuthorizationRequirement>();
    }

    [Fact]
    public void ConfigurePolicies_AddsAllThreePolicies()
    {
        var options = new AuthorizationOptions();

        AuthorizationPolicies.ConfigurePolicies(options);

        options.GetPolicy("AdminOnly").Should().NotBeNull();
        options.GetPolicy("AdminOrOperator").Should().NotBeNull();
        options.GetPolicy("AllAuthenticated").Should().NotBeNull();
    }
}
