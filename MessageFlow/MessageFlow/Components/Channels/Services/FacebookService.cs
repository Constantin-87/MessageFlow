using MessageFlow.Data;
using MessageFlow.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Text;
using System.Text.Json;

namespace MessageFlow.Components.Channels.Services
{
    public class FacebookService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IHubContext<ChatHub> _chatHub;

        public FacebookService(ApplicationDbContext dbContext, IHubContext<ChatHub> chatHub)
        {
            _dbContext = dbContext;
            _chatHub = chatHub;
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


        public async Task SendMessageToFacebookAsync(string recipientId, string messageText, string companyId, string localMessageId)
        {
            var facebookSettings = await GetFacebookSettingsAsync(int.Parse(companyId));

            if (facebookSettings != null)
            {
                var conversation = await _dbContext.Conversations.FirstOrDefaultAsync(c => c.SenderId == recipientId && c.CompanyId == companyId);

                if (conversation != null && !string.IsNullOrEmpty(conversation.AssignedUserId))
                {
                    // Store the message in the database with the localMessageId
                    var message = new MessageFlow.Models.Message
                    {
                        Id = localMessageId,
                        ConversationId = await GetConversationIdAsync(recipientId, companyId),
                        UserId = conversation.AssignedUserId,
                        Content = messageText,
                        SentAt = DateTime.UtcNow
                    };

                    _dbContext.Messages.Add(message);
                    await _dbContext.SaveChangesAsync();

                    var httpClient = new HttpClient();
                    var jsonMessage = new
                    {
                        recipient = new { id = recipientId },
                        message = new
                        {
                            text = messageText,
                            metadata = localMessageId // Attach the unique identifier as metadata
                        }
                    };

                    var jsonContent = new StringContent(JsonConvert.SerializeObject(jsonMessage), Encoding.UTF8, "application/json");

                    Console.WriteLine($"Sending HTTP POST to Facebook: {jsonContent}");

                    var response = await httpClient.PostAsync(
                        $"https://graph.facebook.com/v11.0/me/messages?access_token={facebookSettings.AccessToken}",
                        jsonContent
                    );

                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"Message sent successfully to recipient {recipientId} with metadata {localMessageId}.");
                    }
                    else
                    {
                        Console.WriteLine($"Failed to send message to recipient {recipientId}: {await response.Content.ReadAsStringAsync()}");
                    }
                }
                else
                {
                    Console.WriteLine($"No active conversation found for recipient {recipientId} or AssignedUserId is empty.");
                }
            }
            else
            {
                Console.WriteLine($"Facebook settings not found for company ID {companyId}.");
            }
        }

        // Helper method to get or create a conversation
        private async Task<string> GetConversationIdAsync(string recipientId, string companyId)
        {
            var conversation = await _dbContext.Conversations
                .FirstOrDefaultAsync(c => c.SenderId == recipientId && c.CompanyId == companyId);

            if (conversation == null)
            {
                conversation = new Conversation
                {
                    Id = Guid.NewGuid().ToString(),
                    SenderId = recipientId,
                    CompanyId = companyId,
                    Title = $"Chat with {recipientId}",
                    IsActive = true,
                    Source = "Facebook"
                };

                _dbContext.Conversations.Add(conversation);
                await _dbContext.SaveChangesAsync();
            }

            return conversation.Id;
        }

        public async Task ProcessFacebookMessagesAsync(string pageId, IEnumerable<JsonElement> messagingArray, ILogger logger)
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
                        // Handle echo messages
                        if (messageProperty.TryGetProperty("is_echo", out var isEcho) && isEcho.GetBoolean())
                        {
                            var echoMessageText = messageProperty.GetProperty("text").GetString();
                            var recipientId = eventData.GetProperty("recipient").GetProperty("id").GetString();
                            var metadata = messageProperty.TryGetProperty("metadata", out var meta) ? meta.GetString() : null;

                            logger.LogInformation($"Echo message received for recipient {recipientId} with metadata {metadata}: {echoMessageText}");

                            if (!string.IsNullOrEmpty(metadata))
                            {
                                // Find the message in the database by the metadata (localMessageId)
                                var storedMessage = await _dbContext.Messages
                                    .FirstOrDefaultAsync(m => m.Id == metadata);

                                if (storedMessage != null)
                                {
                                    // Notify the assigned user of the delivery confirmation
                                    var conversation = await _dbContext.Conversations.FindAsync(storedMessage.ConversationId);
                                    if (conversation != null && !string.IsNullOrEmpty(conversation.AssignedUserId))
                                    {
                                        await _chatHub.Clients.User(conversation.AssignedUserId)
                                            .SendAsync("MessageDelivered", recipientId, echoMessageText, metadata);

                                        logger.LogInformation($"Delivery confirmation sent to user {conversation.AssignedUserId} for message ID {metadata}");
                                    }
                                }
                                else
                                {
                                    logger.LogWarning($"No matching message found for metadata {metadata}");
                                }
                            }
                            else
                            {
                                logger.LogWarning($"No metadata found in echo for recipient {recipientId} and message ID {metadata}");
                            }

                            continue;
                        }





                        // Process regular messages
                        var messageText = messageProperty.GetProperty("text").GetString();
                        var conversationTitle = $"Chat with {senderId}, from: Facebook";

                        logger.LogInformation($"New message received from {senderId} for Page ID {pageId}: {messageText}");

                        // Check if a conversation already exists for this senderId
                        var existingConversation = await _dbContext.Conversations
                            .FirstOrDefaultAsync(c => c.SenderId == senderId && c.CompanyId == companyId.ToString());

                        if (existingConversation != null && existingConversation.IsActive)
                        {
                            logger.LogInformation($"Active conversation already exists for senderId: {senderId}. Adding new message.");

                            // Create a new message associated with the existing active conversation
                            var message = new MessageFlow.Models.Message
                            {
                                Id = Guid.NewGuid().ToString(),
                                ConversationId = existingConversation.Id,
                                UserId = senderId,
                                Content = messageText,
                                SentAt = DateTime.UtcNow
                            };

                            _dbContext.Messages.Add(message);
                            await _dbContext.SaveChangesAsync();

                            // If the conversation is assigned, send the message to the assigned user's chat window
                            if (!string.IsNullOrEmpty(existingConversation.AssignedUserId))
                            {
                                Console.WriteLine($"Delegating message sending to ChatHub for user: {existingConversation.AssignedUserId}");

                                await _chatHub.Clients.User(existingConversation.AssignedUserId).SendAsync("SendMessageToAssignedUser", message.Content, senderId);

                            }
                            else
                            {
                                Console.WriteLine("AssignedUserId is null or empty.");
                            }


                        }
                        else
                        {
                            // If the conversation doesn't exist or is inactive, create a new conversation
                            logger.LogInformation($"No active conversation for senderId: {senderId}. Creating a new conversation.");
                            await CreateAndSendNewConversation(companyId, senderId, conversationTitle, messageText, logger);
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


        // Helper method to create and send a new conversation
        private async Task CreateAndSendNewConversation(int companyId, string senderId, string conversationTitle, string messageText, ILogger logger)
        {
            var conversation = new MessageFlow.Models.Conversation
            {
                Title = conversationTitle,
                SenderId = senderId,
                CompanyId = companyId.ToString(),
                IsActive = true, // Set the new conversation as active
                Source = "Facebook"
            };

            // Add the conversation to the database
            _dbContext.Conversations.Add(conversation);
            await _dbContext.SaveChangesAsync();

            // Create a new message associated with the conversation
            var message = new MessageFlow.Models.Message
            {
                Id = Guid.NewGuid().ToString(),
                ConversationId = conversation.Id,
                UserId = senderId,
                Content = messageText,
                SentAt = DateTime.UtcNow
            };

            // Add the message to the database
            _dbContext.Messages.Add(message);
            await _dbContext.SaveChangesAsync();

            // Send new conversation to the specific company group
            await _chatHub.Clients.Group($"Company_{companyId}").SendAsync("NewConversationAdded", conversation);

            logger.LogInformation($"New conversation created and sent to group: Company_{companyId}");
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
