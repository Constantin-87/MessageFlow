using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using MessageFlow.Components.Accounts.Services;
using System.Text;
using System.Text.Json;


[Route("api/[controller]")]
[ApiController]
public class WebhookController : ControllerBase
{
    private readonly ILogger<WebhookController> _logger;
    private readonly FacebookService _facebookService;
    private readonly CompanyManagementService _companyService;

    public WebhookController(ILogger<WebhookController> logger, FacebookService facebookService, CompanyManagementService companyService)
    {
        _logger = logger;
        _facebookService = facebookService;
        _companyService = companyService;
    }

    [HttpGet]
    public async Task<IActionResult> Verify([FromQuery(Name = "hub.mode")] string hub_mode,
                                         [FromQuery(Name = "hub.challenge")] string hub_challenge,
                                         [FromQuery(Name = "hub.verify_token")] string hub_verify_token)
    {
        // Log the incoming values from Facebook
        _logger.LogInformation("Received verification request: hub_mode={hub_mode}, hub_verify_token={hub_verify_token}, hub_challenge={hub_challenge}");

        // Fetch all companies' Facebook settings
        var allFacebookSettings = await _facebookService.GetAllFacebookSettingsAsync();  // You'll need to implement this in FacebookService

        // Find a match for the verify token
        var matchedSettings = allFacebookSettings.FirstOrDefault(settings => settings.AccessToken == hub_verify_token);

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

                    // Fetch the Facebook settings associated with this page ID directly
                    var facebookSettings = await _facebookService.GetFacebookSettingsByPageIdAsync(pageId);

                    if (facebookSettings != null)
                    {
                        foreach (var eventData in messagingArray)
                        {
                            var senderId = eventData.GetProperty("sender").GetProperty("id").GetString();
                            var messageText = eventData.GetProperty("message").GetProperty("text").GetString();

                            _logger.LogInformation($"Message received from {senderId} for Page ID {pageId}: {messageText}");

                            // Process the message according to your logic
                            // e.g., Forward to your app's logic, respond to the message, etc.
                        }
                    }
                    else
                    {
                        _logger.LogWarning($"No Facebook settings found for Page ID {pageId}");
                    }
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



    [HttpPost("send-message")]  // This ensures it's called only on a POST request to /api/webhook/send-message
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
