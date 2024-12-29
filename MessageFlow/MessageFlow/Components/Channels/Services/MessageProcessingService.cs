using MessageFlow.Data;
using MessageFlow.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace MessageFlow.Components.Channels.Services
{
    public class MessageProcessingService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IHubContext<ChatHub> _chatHub;
        private readonly ILogger<MessageProcessingService> _logger;

        public MessageProcessingService(ApplicationDbContext dbContext, IHubContext<ChatHub> chatHub, ILogger<MessageProcessingService> logger)
        {
            _dbContext = dbContext;
            _chatHub = chatHub;
            _logger = logger;
        }

        public async Task ProcessMessageAsync(int companyId, string senderId, string username, string messageText, string providerMessageId, string source)
        {
            var conversation = await _dbContext.Conversations
                .FirstOrDefaultAsync(c => c.SenderId == senderId && c.CompanyId == companyId.ToString());

            if (conversation != null && conversation.IsActive)
            {
                await AddMessageToConversationAsync(conversation, senderId, messageText, providerMessageId);
            }
            else
            {
                await CreateAndBroadcastNewConversationAsync(companyId, senderId, username, messageText, providerMessageId, source);
            }
        }

        public async Task ProcessMessageStatusUpdateAsync(JsonElement statusElement, string platform)
        {
            try
            {
                var messageId = statusElement.GetProperty("id").GetString();
                var timestamp = ParseTimestamp(statusElement);

                var message = await _dbContext.Messages.FirstOrDefaultAsync(m => m.ProviderMessageId == messageId);
                if (message == null)
                {
                    _logger.LogWarning($"Message with ID {messageId} not found in the database.");
                    return;
                }

                await UpdateMessageStatusAsync(message, statusElement);
                message.ChangedAt = timestamp;
                await _dbContext.SaveChangesAsync();

                await NotifyAssignedUserAsync(message, messageId);

            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing status update: {ex.Message}");
            }
        }

        private DateTime ParseTimestamp(JsonElement statusElement)
        {
            if (statusElement.TryGetProperty("timestamp", out var timestampProperty))
            {
                var timestampString = timestampProperty.GetString();
                if (long.TryParse(timestampString, out var timestampUnix))
                {
                    return DateTimeOffset.FromUnixTimeSeconds(timestampUnix).UtcDateTime;
                }
                else
                {
                    _logger.LogWarning($"Unable to parse timestamp '{timestampString}' as a long integer. Using current time.");
                }
            }
            else
            {
                _logger.LogWarning("Timestamp property not found in status element. Using current time.");
            }
            return DateTime.UtcNow;
        }

        private async Task UpdateMessageStatusAsync(Message message, JsonElement statusElement)
        {
            var statusOrder = new List<string> { "SentToProvider", "sent", "delivered", "read" };
            var status = statusElement.GetProperty("status").GetString();

            if (status == "error")
            {
                LogErrorDetails(statusElement);
                message.Status = "error";
            }
            else if (statusOrder.IndexOf(status) > statusOrder.IndexOf(message.Status))
            {
                message.Status = status;
            }
        }

        private void LogErrorDetails(JsonElement statusElement)
        {
            if (statusElement.TryGetProperty("errors", out var errorsElement))
            {
                foreach (var error in errorsElement.EnumerateArray())
                {
                    if (error.TryGetProperty("message", out var errorMessageElement))
                    {
                        _logger.LogError($"Error: {errorMessageElement.GetString()}");
                    }
                }
            }
        }

        private async Task NotifyAssignedUserAsync(Message message, string messageId)
        {
            var conversation = await _dbContext.Conversations.FindAsync(message.ConversationId);
            if (conversation != null && !string.IsNullOrEmpty(conversation.AssignedUserId))
            {
                await _chatHub.Clients.User(conversation.AssignedUserId)
                    .SendAsync("MessageStatusUpdated", message.Id, message.Status);
                _logger.LogInformation($"Notified user {conversation.AssignedUserId} about status update for message {message.Id}.");
            }
            else
            {
                _logger.LogWarning($"No conversation or assigned user found for message ID {messageId}.");
            }
        }

        private async Task AddMessageToConversationAsync(Conversation conversation, string senderId, string messageText, string providerMessageId)
        {
            var message = CreateMessage(conversation.Id, senderId, "Customer", messageText, providerMessageId);

            _dbContext.Messages.Add(message);
            await _dbContext.SaveChangesAsync();

            if (!string.IsNullOrEmpty(conversation.AssignedUserId))
            {
                await _chatHub.Clients.User(conversation.AssignedUserId)
                    .SendAsync("SendMessageToAssignedUser", conversation, message);
            }
        }

        private async Task CreateAndBroadcastNewConversationAsync(int companyId, string senderId, string username, string messageText, string providerMessageId, string source)
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

            _dbContext.Conversations.Add(conversation);
            await _dbContext.SaveChangesAsync();

            var message = CreateMessage(conversation.Id, senderId, "Customer", messageText, providerMessageId);

            _dbContext.Messages.Add(message);
            await _dbContext.SaveChangesAsync();

            await _chatHub.Clients.Group($"Company_{companyId}")
                .SendAsync("NewConversationAdded", conversation);

            _logger.LogInformation($"New conversation created and sent to group: Company_{companyId}");
        }

        private Message CreateMessage(string conversationId, string senderId, string username, string content, string providerMessageId)
        {
            return new Message
            {
                Id = Guid.NewGuid().ToString(),
                ProviderMessageId = providerMessageId,
                ConversationId = conversationId,
                UserId = senderId,
                Username = username,
                Content = content,
                SentAt = DateTime.UtcNow,
                Status = ""
            };
        }
    }
}
