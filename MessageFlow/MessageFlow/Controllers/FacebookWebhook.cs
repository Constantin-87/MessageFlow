using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using MessageFlow.Components.Channels.Services;
using System.Text;
using System.Text.Json;

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
        // Log the incoming values from Facebook
        _logger.LogInformation("Received verification request: hub_mode={hub_mode}, hub_verify_token={hub_verify_token}, hub_challenge={hub_challenge}");

        // Fetch all companies' Facebook settings
        var allFacebookSettings = await _facebookService.GetAllFacebookSettingsAsync();

        // Find a match for the verify token
        var matchedSettings = allFacebookSettings.FirstOrDefault(settings => settings.CompanyId == 6 && settings.WebhookVerifyToken == hub_verify_token);

        if (hub_mode == "subscribe" && matchedSettings != null)
        {
            _logger.LogInformation("Webhook verified successfully for company ID {0}", matchedSettings.CompanyId);
            return Ok(hub_challenge); // Respond with the challenge sent by Facebook
        }
        else
        {
            _logger.LogWarning("Webhook verification failed");
            return Unauthorized();
        }
    }

    [HttpPost]
    public async Task<IActionResult> Receive([FromBody] JsonElement body)
    {
        // Log the raw body for debugging
        string serializedBody = body.ToString();
        _logger.LogInformation($"Received event: {serializedBody}");

        try
        {
            // Check if the object is a page
            if (body.GetProperty("object").GetString() == "page")
            {
                foreach (var entry in body.GetProperty("entry").EnumerateArray())
                {
                    var pageId = entry.GetProperty("id").GetString(); // Page ID associated with the Facebook settings
                    var messagingArray = entry.GetProperty("messaging").EnumerateArray();

                    // Delegate the handling of messages to the service method
                    await _facebookService.ProcessFacebookMessagesAsync(pageId, messagingArray);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error processing incoming event: {ex.Message}");
            return BadRequest();
        }

        return Ok();
    }


    [HttpPost("send-message")]
    public async Task<IActionResult> SendMessage(string recipientId, string messageText, int companyId)
    {
        var httpClient = new HttpClient();

        // Get Facebook settings for the company
        var facebookSettings = await _facebookService.GetFacebookSettingsAsync(companyId);

        if (facebookSettings == null)
        {
            _logger.LogError($"Facebook settings not found for company ID {companyId}");
            return BadRequest("Facebook settings not found.");
        }

        var jsonMessage = new
        {
            recipient = new { id = recipientId },
            message = new { text = messageText }
        };

        var jsonContent = new StringContent(JsonConvert.SerializeObject(jsonMessage), Encoding.UTF8, "application/json");

        // Use the company's Page Access Token
        var response = await httpClient.PostAsync($"https://graph.facebook.com/v11.0/me/messages?access_token={facebookSettings.WebhookVerifyToken}", jsonContent);

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Message sent successfully.");
            return Ok();
        }
        else
        {
            _logger.LogError($"Failed to send message: {await response.Content.ReadAsStringAsync()}");
            return BadRequest("Failed to send message.");
        }
    }

}
