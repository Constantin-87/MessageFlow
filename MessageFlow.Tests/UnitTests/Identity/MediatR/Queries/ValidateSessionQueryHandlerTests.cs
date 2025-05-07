using MessageFlow.DataAccess.Models;
using MessageFlow.Identity.MediatR.Queries;
using MessageFlow.Identity.MediatR.QueryHandlers;
using MessageFlow.Tests.Helpers.Factories;
using Microsoft.AspNetCore.Identity;
using Moq;
using System.Security.Claims;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;

namespace MessageFlow.Tests.UnitTests.Identity.MediatR.Queries;

public class ValidateSessionQueryHandlerTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly ValidateSessionQueryHandler _handler;

    public ValidateSessionQueryHandlerTests()
    {
        _userManagerMock = UnitTestFactory.CreateMockUserManager(Enumerable.Empty<ApplicationUser>().AsQueryable());
        _handler = new ValidateSessionQueryHandler(_userManagerMock.Object);
    }

    [Fact]
    public async Task Handle_NoUserIdClaim_ReturnsFalse()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity());
        var result = await _handler.Handle(new ValidateSessionQuery(principal), default);

        Assert.False(result.Item1);
        Assert.Null(result.Item2);
    }

    [Fact]
    public async Task Handle_UserNotFound_ReturnsFalse()
    {
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "user123") };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));

        _userManagerMock.Setup(m => m.FindByIdAsync("user123")).ReturnsAsync((ApplicationUser?)null);

        var result = await _handler.Handle(new ValidateSessionQuery(principal), default);

        Assert.False(result.Item1);
        Assert.Null(result.Item2);
    }

    [Fact]
    public async Task Handle_LastActivityTooOld_ReturnsFalse()
    {
        var user = new ApplicationUser
        {
            Id = "user123",
            LastActivity = DateTime.UtcNow.AddMinutes(-20)
        };

        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "user123") };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));

        _userManagerMock.Setup(m => m.FindByIdAsync("user123")).ReturnsAsync(user);

        var result = await _handler.Handle(new ValidateSessionQuery(principal), default);

        Assert.False(result.Item1);
        Assert.Null(result.Item2);
    }

    [Fact]
    public async Task Handle_ValidUserAndActivity_ReturnsTrue()
    {
        var user = new ApplicationUser
        {
            Id = "user123",
            LastActivity = DateTime.UtcNow
        };

        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "user123") };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));

        _userManagerMock.Setup(m => m.FindByIdAsync("user123")).ReturnsAsync(user);

        var result = await _handler.Handle(new ValidateSessionQuery(principal), default);

        Assert.True(result.Item1);
        Assert.Equal(user, result.Item2);
    }
}