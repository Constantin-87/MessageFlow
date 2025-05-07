using Xunit;
using Moq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using MessageFlow.Server.Authorization;
using System.Security.Claims;

namespace MessageFlow.Tests.UnitTests.Server.Authorization;
public class AuthorizationHelperTests
{
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock = new();
    private readonly Mock<IConfiguration> _configurationMock = new();
    private readonly AuthorizationHelper _helper;

    public AuthorizationHelperTests()
    {
        _configurationMock.Setup(c => c["SuperAdminCompanyId"]).Returns("super-company");
        _helper = new AuthorizationHelper(_httpContextAccessorMock.Object, _configurationMock.Object);
    }

    private void SetUser(string role, string companyId)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Role, role),
            new("CompanyId", companyId)
        };

        var identity = new ClaimsIdentity(claims, "mock");
        var user = new ClaimsPrincipal(identity);
        _httpContextAccessorMock.Setup(x => x.HttpContext!.User).Returns(user);
    }

    [Fact]
    public async Task CompanyAccess_SuperAdmin_ReturnsAuthorized()
    {
        SetUser("SuperAdmin", "irrelevant");
        var result = await _helper.CompanyAccess("any-company");

        Assert.True(result.isAuthorized);
        Assert.True(result.isSuperAdmin);
    }

    [Fact]
    public async Task CompanyAccess_Admin_SameCompany_ReturnsAuthorized()
    {
        SetUser("Admin", "company1");
        var result = await _helper.CompanyAccess("company1");

        Assert.True(result.isAuthorized);
    }

    [Fact]
    public async Task CompanyAccess_Admin_DifferentCompany_ReturnsUnauthorized()
    {
        SetUser("Admin", "company1");
        var result = await _helper.CompanyAccess("other");

        Assert.False(result.isAuthorized);
        Assert.Equal("Unauthorized for this company.", result.errorMessage);
    }

    [Fact]
    public async Task TeamAccess_SameCompany_ReturnsAuthorized()
    {
        SetUser("Admin", "company1");
        var result = await _helper.TeamAccess("company1");

        Assert.True(result.isAuthorized);
    }

    [Fact]
    public async Task TeamAccess_DifferentCompany_ReturnsUnauthorized()
    {
        SetUser("Admin", "company1");
        var result = await _helper.TeamAccess("company2");

        Assert.False(result.isAuthorized);
    }

    [Fact]
    public async Task UserManagementAccess_SuperAdmin_AssignsSuperAdminToOwnCompany_ReturnsAuthorized()
    {
        SetUser("SuperAdmin", "super-company");
        var result = await _helper.UserManagementAccess("super-company", new() { "SuperAdmin" });

        Assert.True(result.isAuthorized);
    }

    [Fact]
    public async Task UserManagementAccess_SuperAdmin_AssignsSuperAdminToWrongCompany_ReturnsUnauthorized()
    {
        SetUser("SuperAdmin", "super-company");
        var result = await _helper.UserManagementAccess("wrong", new() { "SuperAdmin" });

        Assert.False(result.isAuthorized);
    }

    [Fact]
    public async Task UserManagementAccess_Admin_TryingToAssignSuperAdmin_ReturnsUnauthorized()
    {
        SetUser("Admin", "company1");
        var result = await _helper.UserManagementAccess("company1", new() { "SuperAdmin" });

        Assert.False(result.isAuthorized);
    }

    [Fact]
    public async Task ChannelSettingsAccess_SuperAdmin_ReturnsAuthorized()
    {
        SetUser("SuperAdmin", "any");
        var result = await _helper.ChannelSettingsAccess("company");

        Assert.True(result.isAuthorized);
    }

    [Fact]
    public async Task ChannelSettingsAccess_Admin_OwnCompany_ReturnsAuthorized()
    {
        SetUser("Admin", "company1");
        var result = await _helper.ChannelSettingsAccess("company1");

        Assert.True(result.isAuthorized);
    }

    [Fact]
    public async Task ChannelSettingsAccess_Admin_OtherCompany_ReturnsUnauthorized()
    {
        SetUser("Admin", "company1");
        var result = await _helper.ChannelSettingsAccess("company2");

        Assert.False(result.isAuthorized);
    }
}
