using Moq;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using MessageFlow.Identity.MediatR.Commands;
using MessageFlow.Identity.MediatR.Queries;
using MessageFlow.Identity.Models;
using MediatR;
using Microsoft.AspNetCore.Http;
using MessageFlow.Shared.DTOs;
using MessageFlow.DataAccess.Models;

namespace MessageFlow.Tests.UnitTests.Identity.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _controller = new AuthController(_mediatorMock.Object);
    }

    private void SetUserContext(string userId)
    {
        var identity = new ClaimsIdentity(new[] {
            new Claim(ClaimTypes.NameIdentifier, userId)
        }, "mock");

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity)
            }
        };
    }

    [Fact]
    public async Task Login_ReturnsOk_WhenCredentialsAreValid()
    {
        var request = new LoginRequest { Username = "user", Password = "pass" };
        _mediatorMock.Setup(m => m.Send(It.IsAny<LoginCommand>(), default)).ReturnsAsync(
             new LoginResultDTO
             {
                 Success = true,
                 Token = "token123",
                 RefreshToken = "refresh456",
                 User = new ApplicationUserDTO { Id = "u1", UserName = "user" }
             });

        var result = await _controller.Login(request);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_WhenInvalid()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<LoginCommand>(), default))
            .ReturnsAsync(new LoginResultDTO
            {
                Success = false,
                ErrorMessage = "Invalid"
            });

        var result = await _controller.Login(new LoginRequest());

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        var dto = Assert.IsType<LoginResultDTO>(unauthorized.Value);
        Assert.Equal("Invalid", dto.ErrorMessage);
    }

    [Fact]
    public async Task Login_ReturnsLocked_WhenUserLockedOut()
    {
        var lockoutEnd = DateTimeOffset.UtcNow.AddMinutes(5);

        _mediatorMock.Setup(m => m.Send(It.IsAny<LoginCommand>(), default))
            .ReturnsAsync(new LoginResultDTO
            {
                Success = false,
                ErrorMessage = "Account is locked.",
                LockoutEnd = lockoutEnd
            });

        var result = await _controller.Login(new LoginRequest());

        var locked = Assert.IsType<ObjectResult>(result);
        Assert.Equal(423, locked.StatusCode);
        var dto = Assert.IsType<LoginResultDTO>(locked.Value);
        Assert.Equal(lockoutEnd, dto.LockoutEnd);
    }

    [Fact]
    public async Task Logout_ReturnsOk_WhenSuccessful()
    {
        SetUserContext("u1");

        _mediatorMock.Setup(m => m.Send(It.IsAny<LogoutCommand>(), default))
            .ReturnsAsync(true);

        var result = await _controller.Logout();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Logged out successfully", ok.Value);
    }

    [Fact]
    public async Task Logout_ReturnsBadRequest_WhenFailed()
    {
        SetUserContext("u1");

        _mediatorMock.Setup(m => m.Send(It.IsAny<LogoutCommand>(), default))
            .ReturnsAsync(false);

        var result = await _controller.Logout();

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Failed to logout", bad.Value);
    }

    [Fact]
    public async Task UpdateLastActivity_ReturnsOk_WhenSuccess()
    {
        SetUserContext("u1");

        _mediatorMock.Setup(m => m.Send(It.IsAny<UpdateLastActivityCommand>(), default))
            .ReturnsAsync(true);

        var result = await _controller.UpdateLastActivity();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("User activity updated.", ok.Value);
    }

    [Fact]
    public async Task UpdateLastActivity_ReturnsUnauthorized_WhenUserNull()
    {
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        var result = await _controller.UpdateLastActivity();

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal("User not authenticated.", unauthorized.Value);
    }

    [Fact]
    public async Task UpdateLastActivity_ReturnsBadRequest_WhenFails()
    {
        SetUserContext("u1");

        _mediatorMock.Setup(m => m.Send(It.IsAny<UpdateLastActivityCommand>(), default))
            .ReturnsAsync(false);

        var result = await _controller.UpdateLastActivity();

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Failed to update last activity.", bad.Value);
    }

    [Fact]
    public async Task ValidateSession_ReturnsOk_WhenValid()
    {
        SetUserContext("u1");

        _mediatorMock.Setup(m => m.Send(It.IsAny<ValidateSessionQuery>(), default))
            .ReturnsAsync((true, new ApplicationUser
            {
                Id = "u1",
                UserName = "john",
                LastActivity = DateTime.UtcNow
            }));

        var result = await _controller.ValidateSession();

        var ok = Assert.IsType<OkObjectResult>(result);
        var json = System.Text.Json.JsonSerializer.Serialize(ok.Value);
        var resultObj = System.Text.Json.JsonSerializer.Deserialize<SessionResultDto>(json);

        Assert.Equal("u1", resultObj!.UserId);
        Assert.Equal("john", resultObj.Username);
    }

    [Fact]
    public async Task ValidateSession_ReturnsUnauthorized_WhenInvalid()
    {
        SetUserContext("u1");

        _mediatorMock.Setup(m => m.Send(It.IsAny<ValidateSessionQuery>(), default))
            .ReturnsAsync((false, null));

        var result = await _controller.ValidateSession();

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task RefreshToken_ReturnsOk_WhenValid()
    {
        var tokenModel = new TokenApiModel { AccessToken = "token", RefreshToken = "refresh" };
        _mediatorMock.Setup(m => m.Send(It.IsAny<RefreshTokenCommand>(), default))
            .ReturnsAsync((true, "newAccess", "newRefresh", null));

        var result = await _controller.RefreshToken(tokenModel);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task RefreshToken_ReturnsBadRequest_WhenInvalid()
    {
        var tokenModel = new TokenApiModel();
        _mediatorMock.Setup(m => m.Send(It.IsAny<RefreshTokenCommand>(), default))
            .ReturnsAsync((false, null, null, "Invalid"));

        var result = await _controller.RefreshToken(tokenModel);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Invalid", bad.Value);
    }

    [Fact]
    public async Task RevokeRefreshToken_ReturnsOk_WhenSuccess()
    {
        SetUserContext("u1");

        _mediatorMock.Setup(m => m.Send(It.IsAny<RevokeRefreshTokenCommand>(), default))
            .ReturnsAsync(true);

        var result = await _controller.RevokeRefreshToken();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Refresh token revoked", ok.Value);
    }

    [Fact]
    public async Task RevokeRefreshToken_ReturnsBadRequest_WhenFails()
    {
        SetUserContext("u1");

        _mediatorMock.Setup(m => m.Send(It.IsAny<RevokeRefreshTokenCommand>(), default))
            .ReturnsAsync(false);

        var result = await _controller.RevokeRefreshToken();

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Failed to revoke token", bad.Value);
    }

    [Fact]
    public async Task RevokeRefreshToken_ReturnsUnauthorized_WhenUserNull()
    {
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        var result = await _controller.RevokeRefreshToken();

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task GetCurrentUser_ReturnsNotFound_WhenUserNotFound()
    {
        SetUserContext("u1");

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetCurrentUserQuery>(), default))
            .ReturnsAsync((ApplicationUserDTO?)null);

        var result = await _controller.GetCurrentUser();

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("User not found.", notFound.Value);
    }

    [Fact]
    public async Task GetCurrentUser_ReturnsUnauthorized_WhenUserIdMissing()
    {
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        var result = await _controller.GetCurrentUser();

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal("User not authenticated.", unauthorized.Value);
    }

    private class SessionResultDto
    {
        public string UserId { get; set; }
        public string Username { get; set; }
        public DateTime LastActivity { get; set; }
    }
}