using MessageFlow.Server.Components.Chat.Helpers;
using MessageFlow.Server.Data;
using MessageFlow.Server.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace MessageFlow.Server.Components.Chat.Services
{
    public class FacebookService
    {
        private readonly ILogger<FacebookService> _logger;
        private readonly ApplicationDbContext _dbContext;
        private readonly IHubContext<ChatHub> _chatHub;
        private readonly MessageProcessingService _messageProcessingService;

        public FacebookService(ILogger<FacebookService> logger, ApplicationDbContext dbContext, IHubContext<ChatHub> chatHub, MessageProcessingService messageProcessingService)
        {
            _dbContext = dbContext;
            _chatHub = chatHub;
            _logger = logger;
            _messageProcessingService = messageProcessingService;
        }

        // Retrieve Facebook settings for a company
        public async Task<FacebookSettingsModel?> GetFacebookSettingsAsync(int companyId)
        {
            return await _dbContext.FacebookSettingsModels
                .FirstOrDefaultAsync(fs => fs.CompanyId == companyId);
        }

        // Save Facebook settings for a company
        public async Task<bool> SaveFacebookSettingsAsync(int companyId, FacebookSettingsModel facebookSettings)
        {
            var existingSettings = await GetFacebookSettingsAsync(companyId);

            if (existingSettings == null)
            {
                // Create new settings
                facebookSettings.CompanyId = companyId;
                _dbContext.FacebookSettingsModels.Add(facebookSettings);
            }
            else
            {
                // Update existing settings
                existingSettings.PageId = facebookSettings.PageId;
                existingSettings.AccessToken = facebookSettings.AccessToken;
            }

            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<List<FacebookSettingsModel>> GetAllFacebookSettingsAsync()
        {
            return await _dbContext.FacebookSettingsModels.ToListAsync();
        }

        public async Task<FacebookSettingsModel?> GetFacebookSettingsByPageIdAsync(string pageId)
        {
            return await _dbContext.FacebookSettingsModels.FirstOrDefaultAsync(fs => fs.PageId == pageId);
        }


        public async Task SendMessageToFacebookAsync(string recipientId, string messageText, string companyId, string localMessageId)
        {
            var facebookSettings = await GetFacebookSettingsAsync(int.Parse(companyId));

            if (facebookSettings != null)
            {

                var url = $"https://graph.facebook.com/v11.0/me/messages";
                var payload = new
                {
                    recipient = new { id = recipientId },
                    messaging_type = "RESPONSE",
                    message = new { text = messageText }
                };

                var response = await MessageSenderHelper.SendMessageAsync(url, payload, facebookSettings.AccessToken, _logger);

                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var responseJson = JsonDocument.Parse(responseBody);

                    var facebookMessageId = responseJson.RootElement.GetProperty("message_id").GetString();

                    // Update local database with FacebookMessageId
                    var message = await _dbContext.Messages.FirstOrDefaultAsync(m => m.Id == localMessageId);
                    if (message != null)
                    {
                        message.ProviderMessageId = facebookMessageId;
                        await _dbContext.SaveChangesAsync();
                    }

                    _logger.LogInformation($"Message sent to Facebook. Message ID: {facebookMessageId}");
                }
                else
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Failed to send Facebook message: {responseBody}");

                    var errorDetails = JsonDocument.Parse(responseBody).RootElement;
                    var errorMessage = errorDetails.GetProperty("error").GetProperty("message").GetString();
                    var statusElement = JsonDocument.Parse($"{{\"id\":\"{localMessageId}\",\"status\":\"error\",\"errors\":[{{\"message\":\"{errorMessage}\"}}]}}").RootElement;
                    await _messageProcessingService.ProcessMessageStatusUpdateAsync(statusElement, "Facebook");
                }
            }
            else
            {
                Console.WriteLine($"Facebook settings not found for company ID {companyId}.");
            }
        }

        public async Task ProcessFacebookWebhookEventAsync(JsonElement entry)
        {
            if (!entry.TryGetProperty("messaging", out var messagingEvents))
                return;

            foreach (var messagingEvent in messagingEvents.EnumerateArray())
            {
                if (messagingEvent.TryGetProperty("delivery", out var delivery))
                {
                    await HandleDeliveryEvent(delivery);
                }
                else if (messagingEvent.TryGetProperty("read", out var read))
                {
                    var senderId = messagingEvent.GetProperty("sender").GetProperty("id").GetString();
                    var recipientId = messagingEvent.GetProperty("recipient").GetProperty("id").GetString();
                    await HandleReadEvent(read, senderId, recipientId);
                }
                else if (messagingEvent.TryGetProperty("message", out var messageElement))
                {
                    if (messageElement.TryGetProperty("is_echo", out var isEcho) && isEcho.GetBoolean())
                    {
                        // Log and skip processing for echo messages
                        _logger.LogInformation($"Ignoring echo message with ID: {messageElement.GetProperty("mid").GetString()}");
                        continue;
                    }

                    // Process regular messages
                    var pageId = entry.GetProperty("id").GetString();
                    await ProcessFacebookMessagesAsync(pageId, new[] { messagingEvent });
                }
            }
        }

        private async Task HandleDeliveryEvent(JsonElement delivery)
        {
            if (!delivery.TryGetProperty("mids", out var mids))
                return;

            foreach (var mid in mids.EnumerateArray())
            {
                var statusElement = JsonDocument.Parse($"{{\"id\":\"{mid.GetString()}\",\"status\":\"delivered\"}}").RootElement;
                await _messageProcessingService.ProcessMessageStatusUpdateAsync(statusElement, "Facebook");
            }
        }

        private async Task HandleReadEvent(JsonElement read, string senderId, string recipientId)
        {
            if (!read.TryGetProperty("watermark", out var watermarkProperty))
            {
                _logger.LogWarning("No 'watermark' property found in the 'read' event.");
                return;
            }

            long watermarkUnix;
            try
            {
                // Extract the watermark and validate it's within a reasonable range
                watermarkUnix = watermarkProperty.GetInt64();
                if (watermarkUnix > DateTimeOffset.MaxValue.ToUnixTimeMilliseconds() || watermarkUnix < DateTimeOffset.MinValue.ToUnixTimeMilliseconds())
                {
                    _logger.LogWarning($"Invalid watermark timestamp: {watermarkUnix}. Skipping processing.");
                    return;
                }
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError($"Failed to parse watermark: {ex.Message}");
                return;
            }

            var watermarkTimestamp = DateTimeOffset.FromUnixTimeMilliseconds(watermarkUnix).UtcDateTime;

            // Retrieve the CompanyId using the recipientId (PageId) from FacebookSettingsModel
            var facebookSettings = await _dbContext.FacebookSettingsModels
                .FirstOrDefaultAsync(fs => fs.PageId == recipientId);

            if (facebookSettings == null)
            {
                _logger.LogWarning($"No Facebook settings found for Page ID {recipientId}.");
                return;
            }

            // Use the CompanyId and SenderId to find the conversation
            var conversation = await _dbContext.Conversations
                .FirstOrDefaultAsync(c => c.CompanyId == facebookSettings.CompanyId.ToString() && c.SenderId == senderId);

            if (conversation == null)
            {
                _logger.LogWarning($"No conversation found for sender {senderId} in company {facebookSettings.CompanyId}.");
                return;
            }

            // Find messages in this conversation sent before the watermark and not yet marked as read
            var messagesToUpdate = await _dbContext.Messages
                .Where(m => m.ConversationId == conversation.Id && m.Status != "read" && m.SentAt <= watermarkTimestamp)
                .ToListAsync();

            foreach (var message in messagesToUpdate)
            {
                try
                {
                    // Format the JSON with the timestamp
                    var statusElementJson = $"{{\"id\":\"{message.ProviderMessageId}\",\"status\":\"read\",\"timestamp\":\"{watermarkUnix / 1000}\"}}";
                    var statusElement = JsonDocument.Parse(statusElementJson).RootElement;

                    await _messageProcessingService.ProcessMessageStatusUpdateAsync(statusElement, "Facebook");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error updating message ID {message.Id} to 'read': {ex.Message}");
                }
            }

            _logger.LogInformation($"Marked {messagesToUpdate.Count} messages as read in conversation {conversation.Id} up to watermark timestamp {watermarkTimestamp}.");
        }

        public async Task ProcessFacebookMessagesAsync(string pageId, IEnumerable<JsonElement> messagingArray)
        {
            var facebookSettings = await GetFacebookSettingsByPageIdAsync(pageId);
            var companyId = facebookSettings?.CompanyId ?? 0;

            if (companyId == 0)
            {
                _logger.LogWarning($"No Facebook settings found for Page ID {pageId}");
                return;
            }

            foreach (var eventData in messagingArray)
            {
                if (eventData.TryGetProperty("sender", out var senderElement) &&
                    eventData.TryGetProperty("message", out var messageElement) &&
                    messageElement.TryGetProperty("mid", out var midElement))
                {

                    var senderId = senderElement.GetProperty("id").GetString();
                    var messageText = messageElement.GetProperty("text").GetString();
                    var providerMessageId = midElement.GetString();

                    var senderUserName = senderId; // Placeholder; To be adjusted to fetch the actual username

                    await _messageProcessingService.ProcessMessageAsync(companyId, senderId, senderUserName, messageText, providerMessageId, "Facebook");
                }
                else
                {
                    _logger.LogWarning($"Unhandled event type in Facebook webhook payload: {eventData}");
                }
            }

        }
    }
}
