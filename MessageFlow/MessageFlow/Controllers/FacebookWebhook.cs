using Microsoft.AspNetCore.Mvc;
using MessageFlow.Components.Channels.Services;
using System.Text.Json;
using MessageFlow.Controllers;

[Route("api/[controller]")]
[ApiController]
public class FacebookWebhook : ControllerBase
{
    private readonly ILogger<FacebookWebhook> _logger;
    private readonly FacebookService _facebookService;

    public FacebookWebhook(ILogger<FacebookWebhook> logger, FacebookService facebookService)
    {
        _logger = logger;
        _facebookService = facebookService;
    }

    [HttpGet]
    public async Task<IActionResult> Verify([FromQuery(Name = "hub.mode")] string hub_mode,
                                        [FromQuery(Name = "hub.challenge")] string hub_challenge,
                                        [FromQuery(Name = "hub.verify_token")] string hub_verify_token)
    {
        var isValid = await WebhookProcessingHelper.VerifyTokenAsync(
            _facebookService.GetAllFacebookSettingsAsync,
            settings => settings.WebhookVerifyToken,
            hub_mode,
            hub_verify_token,
            _logger
        );

        if (isValid)
        {
            return Ok(hub_challenge);
        }

        return Unauthorized();
    }

    [HttpPost]
    public async Task<IActionResult> Receive([FromBody] JsonElement body)
    {
        _logger.LogInformation($"Received Facebook webhook event: {body}");

        try
        {
            await WebhookProcessingHelper.ProcessWebhookEntriesAsync(
                body,
                "page",
                _logger,
                async entry =>
                {
                    var messagingArray = entry.GetProperty("messaging").EnumerateArray();

                    // Delegate message processing to the Facebook service
                    await _facebookService.ProcessFacebookWebhookEventAsync(entry);
                });

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error processing Facebook webhook: {ex.Message}");
            return BadRequest();
        }
    }
}
