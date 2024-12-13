using MessageFlow.Data;
using MessageFlow.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using static MessageFlow.Client.Components.NewConversationsList;

namespace MessageFlow.Components.Channels.Services
{
    public class FacebookService
    {
        private readonly ApplicationDbContext _dbContext;

        public FacebookService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
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
                existingSettings.AppId = facebookSettings.AppId;
                existingSettings.AccessToken = facebookSettings.AccessToken;
                existingSettings.WebhookVerifyToken = facebookSettings.WebhookVerifyToken;
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


        public async Task ProcessFacebookMessagesAsync(string pageId, IEnumerable<JsonElement> messagingArray, IHubContext<ChatHub> chatHub, ILogger logger)
        {
            // Fetch the Facebook settings associated with this page ID directly
            var facebookSettings = await GetFacebookSettingsByPageIdAsync(pageId);
            var companyId = facebookSettings?.CompanyId ?? 0;

            if (companyId != 0)
            {
                foreach (var eventData in messagingArray)
                {
                    var senderId = eventData.GetProperty("sender").GetProperty("id").GetString();

                    // Check if the event contains a "message" property
                    if (eventData.TryGetProperty("message", out var messageProperty))
                    {
                        var messageText = messageProperty.GetProperty("text").GetString();
                        var conversationTitle = $"Chat with {senderId}, from: Facebook";

                        logger.LogInformation($"New message received from {senderId} for Page ID {pageId}: {messageText}");

                        // Mock GPT model processing
                        var gptResponse = await ProcessMessageWithGPTAsync(messageText);

                        if (gptResponse == "HANDLED")
                        {
                            logger.LogInformation("Message handled by GPT model.");
                        }
                        else
                        {
                            logger.LogInformation($"Sending new conversation to group: Company_{companyId}");
                            // Send new conversation to the specific company group
                            await chatHub.Clients.Group($"Company_{companyId}").SendAsync("NewConversationAdded", new Conversation
                            {
                                Title = conversationTitle,
                                SenderId = senderId,
                                CompanyID = companyId,
                                MessageText = messageText
                            });
                            logger.LogInformation($"Message sent to group: Company_{companyId}");
                        }
                    }
                    else
                    {
                        logger.LogWarning($"Unhandled event type for sender ID {senderId} and Page ID {pageId}");
                    }
                }
            }
            else
            {
                logger.LogWarning($"No Facebook settings found for Page ID {pageId}");
            }
        }

        // Mock method for GPT model integration
        private async Task<string> ProcessMessageWithGPTAsync(string message)
        {
            // Simulate GPT processing
            await Task.Delay(500); // Simulate processing delay

            // For demo purposes, let's assume GPT handles simple messages
            return message.Contains("simple") ? "HANDLED" : "NOT_HANDLED";
        }


    }
}
