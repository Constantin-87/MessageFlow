using MessageFlow.Data;
using MessageFlow.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace MessageFlow.Components.Channels.Helpers
{
    public static class MessageProcessingHelper
    {
        public static async Task ProcessMessageAsync(
            ApplicationDbContext dbContext,
            IHubContext<ChatHub> chatHub,
            ILogger logger,
            int companyId,
            string senderId,
            string username,
            string messageText,
            string providerMessageId,
            //string providerConversationId,
            string source)
        {
            // Check if a conversation exists using ProviderConversationId
            //var conversation = await dbContext.Conversations
            //    .FirstOrDefaultAsync(c => c.ProviderConversationId == providerConversationId && c.CompanyId == companyId.ToString());

            // Check if a conversation exists
            var conversation = await dbContext.Conversations.FirstOrDefaultAsync(c => c.SenderId == senderId && c.CompanyId == companyId.ToString());

            if (conversation != null && conversation.IsActive)
            {
                await AddMessageToConversationAsync(dbContext, chatHub, conversation, senderId, messageText, providerMessageId);
            }
            else
            {
                await CreateAndBroadcastNewConversationAsync(dbContext, chatHub, logger, companyId, senderId, username, messageText, providerMessageId, source);
            }
        }

        public static async Task HandleStatusUpdateAsync(
             ApplicationDbContext dbContext,
             IHubContext<ChatHub> chatHub,
             ILogger logger,
             JsonElement statusElement,
             string platform)
        {
            try
            {
                var messageId = statusElement.GetProperty("id").GetString();
                var status = statusElement.GetProperty("status").GetString();

                DateTime timestamp;
                if (statusElement.TryGetProperty("timestamp", out var timestampProperty))
                {
                    var timestampString = timestampProperty.GetString();
                    if (long.TryParse(timestampString, out var timestampUnix))
                    {
                        timestamp = DateTimeOffset.FromUnixTimeSeconds(timestampUnix).UtcDateTime;
                    }
                    else
                    {
                        logger.LogWarning($"Unable to parse timestamp '{timestampString}' as a long integer. Using current time.");
                        timestamp = DateTime.UtcNow;
                    }
                }
                else
                {
                    logger.LogWarning("Timestamp property not found in status element. Using current time.");
                    timestamp = DateTime.UtcNow;
                }

                var message = await dbContext.Messages.FirstOrDefaultAsync(m => m.ProviderMessageId == messageId);

                if (message == null)
                {
                    logger.LogWarning($"Message with ID {messageId} not found in the database.");
                    return;
                }

                // TODO: Check for error information !!!!!!
                string errorMessage = null;
                if (statusElement.TryGetProperty("errors", out var errorsElement))
                {
                    var errorArray = errorsElement.EnumerateArray();
                    foreach (var error in errorArray)
                    {
                        if (error.TryGetProperty("message", out var errorMessageElement))
                        {
                            errorMessage = errorMessageElement.GetString();
                            break; // Only take the first error message
                        }
                    }
                }

                // Define the order of statuses
                var statusOrder = new List<string> { "SentToProvider", "sent", "delivered", "read" };

                // Update the message's status and timestamp
                if (!string.IsNullOrEmpty(errorMessage))
                {
                    message.Status = "error";
                    //message.ErrorMessage = errorMessage;
                }
                else if (statusOrder.IndexOf(status) > statusOrder.IndexOf(message.Status))
                {
                    message.Status = status;
                    //message.ErrorMessage = null; // Clear any previous error message
                }

                message.ChangedAt = timestamp;
                await dbContext.SaveChangesAsync();
                logger.LogInformation($"Message {messageId} updated with status '{message.Status}'.");


                // Notify the user assigned to the conversation
                var conversation = await dbContext.Conversations.FindAsync(message.ConversationId);
                if (conversation != null && !string.IsNullOrEmpty(conversation.AssignedUserId))
                {
                    // Send status update with optional error message
                    await chatHub.Clients.User(conversation.AssignedUserId)
                        .SendAsync("MessageStatusUpdated", message.Id, message.Status, errorMessage);

                    logger.LogInformation($"Notified user {conversation.AssignedUserId} about status update for message {message.Id}.");
                }
                else
                {
                    logger.LogWarning($"No conversation or assigned user found for message ID {messageId}.");
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Error processing status update: {ex.Message}");
            }
        }

        private static async Task AddMessageToConversationAsync(
            ApplicationDbContext dbContext,
            IHubContext<ChatHub> chatHub,
            Conversation conversation,
            string senderId,
            string messageText,
            string providerMessageId)
        {
            var message = new Message
            {
                Id = Guid.NewGuid().ToString(),
                ProviderMessageId = providerMessageId,
                ConversationId = conversation.Id,
                UserId = senderId,
                Username = "Customer",
                Content = messageText,
                SentAt = DateTime.UtcNow,
                Conversation = conversation,
                Status = ""
            };

            dbContext.Messages.Add(message);
            await dbContext.SaveChangesAsync();

            if (!string.IsNullOrEmpty(conversation.AssignedUserId))
            {
                await chatHub.Clients.User(conversation.AssignedUserId)
                    .SendAsync("SendMessageToAssignedUser", conversation, message);
            }
        }

        private static async Task CreateAndBroadcastNewConversationAsync(
             ApplicationDbContext dbContext,
             IHubContext<ChatHub> chatHub,
             ILogger logger,
             int companyId,
             string senderId,
             string username,
             string messageText,
             string providerMessageId,
             //string providerConversationId,
             string source)
        {
            var conversation = new Conversation
            {
                Id = Guid.NewGuid().ToString(),
                SenderId = senderId,
                SenderUsername = username,
                CompanyId = companyId.ToString(),
                IsActive = true,
                Source = source
            };

            dbContext.Conversations.Add(conversation);
            await dbContext.SaveChangesAsync();

            var message = new Message
            {
                Id = Guid.NewGuid().ToString(),
                ProviderMessageId = providerMessageId,
                ConversationId = conversation.Id,
                UserId = senderId,
                Username = "Customer",
                Content = messageText,
                SentAt = DateTime.UtcNow,
                Conversation = conversation,
                Status = ""
            };

            dbContext.Messages.Add(message);
            await dbContext.SaveChangesAsync();

            await chatHub.Clients.Group($"Company_{companyId}")
                .SendAsync("NewConversationAdded", conversation);

            logger.LogInformation($"New conversation created and sent to group: Company_{companyId}");
        }
    }
}
