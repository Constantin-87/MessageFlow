using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MessageFlow.Components.Channels.Services;
using System.Text.Json;

[Route("api/[controller]")]
[ApiController]
public class WhatsAppWebhook : ControllerBase
{
    private readonly ILogger<WhatsAppWebhook> _logger;
    private readonly WhatsAppService _whatsAppService;

    public WhatsAppWebhook(ILogger<WhatsAppWebhook> logger, WhatsAppService whatsAppService)
    {
        _logger = logger;
        _whatsAppService = whatsAppService;
    }

    [HttpGet]
    public async Task<IActionResult> Verify([FromQuery(Name = "hub.mode")] string hub_mode,
                                         [FromQuery(Name = "hub.challenge")] string hub_challenge,
                                         [FromQuery(Name = "hub.verify_token")] string hub_verify_token)
    {
        // Log the incoming values for debugging
        _logger.LogInformation($"Received verification request: hub_mode={hub_mode}, hub_verify_token={hub_verify_token}, hub_challenge={hub_challenge}");

        // Fetch all companies' WhatsApp settings
        var allWhatsAppSettings = await _whatsAppService.GetAllWhatsAppSettingsAsync();

        // Find a match for the verify token
        var matchedSettings = allWhatsAppSettings.FirstOrDefault(settings => settings.WebhookVerifyToken == hub_verify_token);

        if (hub_mode == "subscribe" && matchedSettings != null)
        {
            _logger.LogInformation($"WhatsApp webhook verified successfully for company ID {matchedSettings.CompanyId}");
            return Ok(hub_challenge); // Respond with the challenge sent by Meta
        }
        else
        {
            _logger.LogWarning("WhatsApp webhook verification failed");
            return Unauthorized();
        }
    }

    [HttpPost]
    public async Task<IActionResult> Receive([FromBody] JsonElement body)
    {
        _logger.LogInformation($"Received WhatsApp event: {body}");

        try
        {
            var businessAccountId = body.GetProperty("entry")[0].GetProperty("id").GetString();
            _logger.LogInformation($"Received BusinessAccountId: {businessAccountId}");

            var whatsAppSettings = await _whatsAppService.GetWhatsAppSettingsByBusinessAccountIdAsync(businessAccountId);

            if (whatsAppSettings == null)
            {
                _logger.LogWarning($"No WhatsApp settings found for BusinessAccountId: {businessAccountId}");
                return NotFound("No settings found for this BusinessAccountId.");
            }

            var messages = body.GetProperty("entry")[0].GetProperty("changes")[0]
                                .GetProperty("value").GetProperty("messages");

            foreach (var message in messages.EnumerateArray())
            {
                var from = message.GetProperty("from").GetString();
                var messageText = message.GetProperty("text").GetProperty("body").GetString();

                _logger.LogInformation($"Message received from {from}: {messageText}");

                // Process the message (you can add logic to notify specific users)
                await _whatsAppService.ProcessIncomingMessageAsync(from, messageText, whatsAppSettings.CompanyId);
            }

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error processing WhatsApp webhook: {ex.Message}");
            return BadRequest();
        }
    }





}
