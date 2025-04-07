using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using MessageFlow.Identity.Models;
using Microsoft.AspNetCore.Authorization;
using MessageFlow.Identity.MediatorComponents.Commands;
using MediatR;
using MessageFlow.Identity.MediatorComponents.Queries;

[Route("api/auth")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var (success, token, refreshToken, errorMessage, userDto) =
            await _mediator.Send(new LoginCommand(request.Username, request.Password));

        if (!success)
            return Unauthorized(errorMessage);

        return Ok(new { Token = token, RefreshToken = refreshToken, User = userDto });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var result = await _mediator.Send(new LogoutCommand(User));
        if (!result)
            return BadRequest("Failed to logout");

        return Ok("Logged out successfully");
    }

    [HttpPost("update-activity")]
    public async Task<IActionResult> UpdateLastActivity()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized("User not authenticated.");

        var success = await _mediator.Send(new UpdateLastActivityCommand(userId));
        if (!success)
            return BadRequest("Failed to update last activity.");

        return Ok("User activity updated.");
    }

    [HttpGet("session")]
    public async Task<IActionResult> ValidateSession()
    {
        var (success, user) = await _mediator.Send(new ValidateSessionQuery(User));
        if (!success || user == null)
            return Unauthorized("Session invalid");

        return Ok(new { UserId = user.Id, Username = user.UserName, LastActivity = user.LastActivity });
    }

    [AllowAnonymous]
    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] TokenApiModel tokenModel)
    {
        var (success, newAccessToken, newRefreshToken, errorMessage) =
        await _mediator.Send(new RefreshTokenCommand(tokenModel.AccessToken, tokenModel.RefreshToken));

        if (!success)
            return BadRequest(errorMessage);

        return Ok(new
        {
            token = newAccessToken,
            refreshToken = newRefreshToken
        });
    }

    [HttpPost("revoke-refresh-token")]
    public async Task<IActionResult> RevokeRefreshToken()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized();

        var success = await _mediator.Send(new RevokeRefreshTokenCommand(userId));
        return success ? Ok("Refresh token revoked") : BadRequest("Failed to revoke token");
    }

    [Authorize]
    [HttpGet("getCurrentUser")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized("User not authenticated.");

        var user = await _mediator.Send(new GetCurrentUserQuery(userId));
        if (user == null)
            return NotFound("User not found.");

        return Ok(user);
    }

}
