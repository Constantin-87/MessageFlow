using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using MessageFlow.Identity.Services;
using MessageFlow.Identity.Models;

[Route("api/auth")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var (success, token, refreshToken, errorMessage) = await _authService.LoginAsync(request.Username, request.Password);
        if (!success)
            return Unauthorized(errorMessage);

        return Ok(new { Token = token, refreshToken });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await _authService.LogoutAsync();
        return Ok("Logged out successfully");
    }

    [HttpPost("update-activity")]
    public async Task<IActionResult> UpdateLastActivity()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized("User not authenticated.");

        var success = await _authService.UpdateLastActivityAsync(userId);
        if (!success)
            return BadRequest("Failed to update last activity.");

        return Ok("User activity updated.");
    }

    [HttpGet("session")]
    public async Task<IActionResult> ValidateSession()
    {
        var (success, user) = await _authService.ValidateSessionAsync(User);
        if (!success || user == null)
            return Unauthorized("Session invalid");

        return Ok(new { UserId = user.Id, Username = user.UserName, LastActivity = user.LastActivity });
    }


    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] TokenApiModel tokenModel)
    {
        var (success, newAccessToken, newRefreshToken, errorMessage) = await _authService.RefreshTokenAsync(tokenModel.AccessToken, tokenModel.RefreshToken);

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

        var success = await _authService.RevokeRefreshTokenAsync(userId);
        return success ? Ok("Refresh token revoked") : BadRequest("Failed to revoke token");
    }
}
