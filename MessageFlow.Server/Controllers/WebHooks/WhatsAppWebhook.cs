using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using MessageFlow.Server.Configuration;
using Microsoft.Extensions.Options;
using MessageFlow.Server.MediatorComponents.Chat.WhatsappProcessing.Commands;
using MediatR;
using MessageFlow.Server.Helpers;

[Route("api/[controller]")]
[ApiController]
public class WhatsAppWebhook : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly GlobalChannelSettings _globalChannelSettings;
    private readonly ILogger<WhatsAppWebhook> _logger;

    public WhatsAppWebhook(IMediator mediator, ILogger<WhatsAppWebhook> logger, IOptions<GlobalChannelSettings> globalChannelSettings)
    {
        _mediator = mediator;
        _logger = logger;
        _globalChannelSettings = globalChannelSettings.Value;
    }

    [HttpGet]
    public async Task<IActionResult> Verify([FromQuery(Name = "hub.mode")] string hub_mode,
                                        [FromQuery(Name = "hub.challenge")] string hub_challenge,
                                        [FromQuery(Name = "hub.verify_token")] string hub_verify_token)
    {
        _logger.LogInformation($"Verifying WhatsApp Webhook: mode={hub_mode}, token={hub_verify_token}");

        if (hub_mode != "subscribe")
        {
            _logger.LogWarning("Invalid hub mode.");
            return Unauthorized();
        }

        if (_globalChannelSettings.WhatsAppWebhookVerifyToken == hub_verify_token)
        {
            return Ok(hub_challenge);
        }

        _logger.LogWarning("Webhook verification failed.");
        return Unauthorized();
    }

    [HttpPost]
    public async Task<IActionResult> Receive([FromBody] JsonElement body)
     {
        _logger.LogInformation($"Received WhatsApp webhook event: {body}");

        try
        {
            await WebhookProcessingHelper.ProcessWebhookEntriesAsync(
                body,
                "whatsapp_business_account",
                _logger,
                async entry =>
                {
                    var businessAccountId = entry.GetProperty("id").GetString();
                    _logger.LogInformation($"Processing entry for BusinessAccountId: {businessAccountId}");

                    var changes = entry.GetProperty("changes");

                    // Delegate message processing to the WhatsApp service
                    await _mediator.Send(new ProcessIncomingWAMessageCommand(businessAccountId, changes));
                });

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error processing WhatsApp webhook: {ex.Message}");
            return BadRequest();
        }
    }
}
