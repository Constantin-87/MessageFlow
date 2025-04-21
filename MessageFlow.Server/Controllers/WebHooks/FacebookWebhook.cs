using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using MessageFlow.Server.Configuration;
using Microsoft.Extensions.Options;
using MediatR;
using MessageFlow.Server.MediatR.Chat.FacebookProcessing.Commands;
using MessageFlow.Server.Helpers;

[Route("api/[controller]")]
[ApiController]
public class FacebookWebhook : ControllerBase
{
    private readonly ILogger<FacebookWebhook> _logger;
    private readonly IMediator _mediator;
    private readonly GlobalChannelSettings _globalChannelSettings;

    public FacebookWebhook(ILogger<FacebookWebhook> logger, IMediator mediator, IOptions<GlobalChannelSettings> globalChannelSettings)
    {
        _logger = logger;
        _mediator = mediator;
        _globalChannelSettings = globalChannelSettings.Value;
    }

    [HttpGet]
    public IActionResult Verify([FromQuery(Name = "hub.mode")] string hub_mode,
                                [FromQuery(Name = "hub.challenge")] string hub_challenge,
                                [FromQuery(Name = "hub.verify_token")] string hub_verify_token)
    {
        _logger.LogInformation($"Verifying Facebook Webhook: mode={hub_mode}, token={hub_verify_token}");

        if (hub_mode != "subscribe")
        {
            _logger.LogWarning("Invalid hub mode.");
            return Unauthorized();
        }

        // Compare with verify token from appsettings.json
        if (_globalChannelSettings.FacebookWebhookVerifyToken == hub_verify_token)
        {
            return Ok(hub_challenge);
        }

        _logger.LogWarning("Webhook verification failed.");
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
                    await _mediator.Send(new ProcessFacebookWebhookEventCommand(entry));
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
