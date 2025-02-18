using MessageFlow.Server.Components.Chat.Helpers;
using MessageFlow.Server.Data;
using MessageFlow.Server.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace MessageFlow.Server.Components.Chat.Services
{
    public class WhatsAppService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IHubContext<ChatHub> _chatHub;
        private readonly ILogger<WhatsAppService> _logger;
        private readonly MessageProcessingService _messageProcessingService;

        public WhatsAppService(ApplicationDbContext dbContext, IHubContext<ChatHub> chatHub, ILogger<WhatsAppService> logger, MessageProcessingService messageProcessingService)
        {
            _dbContext = dbContext;
            _chatHub = chatHub;
            _logger = logger;
            _messageProcessingService = messageProcessingService;
        }
        public async Task<bool> SaveWhatsAppSettingsAsync(int companyId, WhatsAppSettingsModel whatsAppSettings)
        {
            var existingSettings = await GetWhatsAppSettingsAsync(companyId);

            if (existingSettings == null)
            {
                // Create new settings
                whatsAppSettings.CompanyId = companyId;
                _dbContext.WhatsAppSettingsModels.Add(whatsAppSettings);
            }
            else
            {
                // Update existing settings
                existingSettings.AccessToken = whatsAppSettings.AccessToken;
                existingSettings.BusinessAccountId = whatsAppSettings.BusinessAccountId;
                existingSettings.PhoneNumbers = whatsAppSettings.PhoneNumbers;
            }

            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<List<WhatsAppSettingsModel>> GetAllWhatsAppSettingsAsync()
        {
            return await _dbContext.WhatsAppSettingsModels.ToListAsync();
        }

        public async Task<WhatsAppSettingsModel?> GetWhatsAppSettingsByBusinessAccountIdAsync(string businessAccountId)
        {
            return await _dbContext.WhatsAppSettingsModels
                .FirstOrDefaultAsync(ws => ws.BusinessAccountId == businessAccountId);
        }

        // Retrieve WhatsApp settings for a company
        public async Task<WhatsAppSettingsModel?> GetWhatsAppSettingsAsync(int companyId)
        {
            return await _dbContext.WhatsAppSettingsModels
                .Include(ws => ws.PhoneNumbers)
                .FirstOrDefaultAsync(ws => ws.CompanyId == companyId);
        }

        public async Task ProcessIncomingMessageAsync(string businessAccountId, JsonElement changes)
        {
            var whatsAppSettings = await GetWhatsAppSettingsByBusinessAccountIdAsync(businessAccountId);
            var companyId = whatsAppSettings?.CompanyId ?? 0;

            if (companyId == 0)
            {
                _logger.LogWarning($"No WhatsApp settings found for BusinessAccountId {businessAccountId}");
                return;
            }

            foreach (var change in changes.EnumerateArray())
            {
                if (!change.TryGetProperty("value", out var value))
                {
                    _logger.LogWarning($"No value found in change entry for BusinessAccountId {businessAccountId}");
                    continue;
                }

                // Handle delivery statuses
                if (value.TryGetProperty("statuses", out var statuses))
                {
                    var sortedStatuses = statuses.EnumerateArray()
                                                 .OrderBy(s => s.GetProperty("timestamp").GetString())
                                                 .ToList();

                    foreach (var status in sortedStatuses)
                    {
                        await _messageProcessingService.ProcessMessageStatusUpdateAsync(status, "WhatsApp");
                    }
                }

                if (value.TryGetProperty("contacts", out var contacts))
                {
                    foreach (var contact in contacts.EnumerateArray())
                    {
                        var senderId = contact.GetProperty("wa_id").GetString();
                        var username = contact.GetProperty("profile").GetProperty("name").GetString();

                        if (value.TryGetProperty("messages", out var messages))
                        {
                            foreach (var message in messages.EnumerateArray())
                            {
                                var messageText = message.GetProperty("text").GetProperty("body").GetString();
                                var providerMessageId = message.GetProperty("id").GetString();

                                await _messageProcessingService.ProcessMessageAsync(companyId, senderId, username, messageText, providerMessageId, "WhatsApp");
                            }
                        }
                    }
                }
            }
        }

        // Send a message via WhatsApp Cloud API
        public async Task SendMessageToWhatsAppAsync(string recipientPhoneNumber, string messageText, string companyId, string localMessageId)
        {
            var whatsAppSettings = await GetWhatsAppSettingsAsync(int.Parse(companyId));

            if (whatsAppSettings != null && whatsAppSettings.PhoneNumbers.Any())
            {
                var phoneNumberInfo = whatsAppSettings.PhoneNumbers.FirstOrDefault();
                if (phoneNumberInfo != null)
                {
                    var url = $"https://graph.facebook.com/v17.0/{phoneNumberInfo.PhoneNumberId}/messages";
                    var payload = new
                    {
                        messaging_product = "whatsapp",
                        to = recipientPhoneNumber,
                        type = "text",
                        text = new { body = messageText }
                    };

                    var response = await MessageSenderHelper.SendMessageAsync(url, payload, whatsAppSettings.AccessToken, _logger);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseBody = await response.Content.ReadAsStringAsync();
                        var responseJson = JsonDocument.Parse(responseBody);

                        var whatsappMessageId = responseJson.RootElement
                            .GetProperty("messages")[0]
                            .GetProperty("id")
                            .GetString();

                        // Update local database with WhatsAppMessageId
                        var message = await _dbContext.Messages.FirstOrDefaultAsync(m => m.Id == localMessageId);
                        if (message != null)
                        {
                            message.ProviderMessageId = whatsappMessageId;
                            await _dbContext.SaveChangesAsync();
                        }

                        // Notify app user of the message status
                        await _chatHub.Clients.User(companyId)
                            .SendAsync("MessageStatusUpdated", localMessageId, "Sent");
                    }
                    else
                    {
                        var responseBody = await response.Content.ReadAsStringAsync();
                        _logger.LogError($"Failed to send WhatsApp message: {responseBody}");

                        var errorDetails = JsonDocument.Parse(responseBody).RootElement;
                        var errorMessage = errorDetails.GetProperty("error").GetProperty("message").GetString();
                        var jsonElement = JsonDocument.Parse($"{{\"id\":\"{localMessageId}\",\"status\":\"error\",\"errors\":[{{\"message\":\"{errorMessage}\"}}]}}").RootElement;

                        await _messageProcessingService.ProcessMessageStatusUpdateAsync(jsonElement, "WhatsApp");

                        _logger.LogError($"Failed to send WhatsApp message to {recipientPhoneNumber}");
                    }
                }
                else
                {
                    Console.WriteLine("No phone number found in the settings.");
                }
            }
            else
            {
                Console.WriteLine($"WhatsApp settings not found for company ID {companyId}.");
            }
        }

        // TO DO: Mark messages as read when clicking into the chat window 

        //public async Task MarkMessagesAsReadAsync(string whatsappConversationId, List<string> messageIds)
        //{

        //    // Retrieve WhatsApp settings for the given conversation ID
        //    var whatsAppSettings = await _dbContext.WhatsAppSettingsModels
        //        .FirstOrDefaultAsync(ws => ws.BusinessAccountId == whatsappConversationId);

        //    if (whatsAppSettings == null)
        //    {
        //        _logger.LogWarning($"No WhatsApp settings found for BusinessAccountId {whatsappConversationId}");
        //        return;
        //    }

        //    var url = $"https://graph.facebook.com/v17.0/{whatsappConversationId}/messages";
        //    var payload = new
        //    {
        //        messaging_product = "whatsapp",
        //        status = "read",
        //        ids = messageIds
        //    };

        //    await MessageSenderHelper.SendMessageAsync(url, payload, whatsAppSettings.AccessToken, _logger);
        //}
    }
}
