﻿using MessageFlow.Data;
using MessageFlow.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Text.Json;

namespace MessageFlow.Components.Chat.Services
{
    public class MessageProcessingService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IHubContext<ChatHub> _chatHub;
        private readonly ILogger<MessageProcessingService> _logger;
        private readonly AIChatBotService _aiChatBotService;

        public MessageProcessingService(ApplicationDbContext dbContext, IHubContext<ChatHub> chatHub, ILogger<MessageProcessingService> logger, AIChatBotService aiChatBotService)
        {
            _dbContext = dbContext;
            _chatHub = chatHub;
            _logger = logger;
            _aiChatBotService = aiChatBotService;
        }

        public async Task ProcessMessageAsync(int companyId, string senderId, string username, string messageText, string providerMessageId, string source)
        {
            var conversation = await _dbContext.Conversations
                .FirstOrDefaultAsync(c => c.SenderId == senderId && c.CompanyId == companyId.ToString());

            //if (conversation != null && conversation.IsActive)
            //{
            //    await AddMessageToConversationAsync(conversation, senderId, messageText, providerMessageId);
            //}
            //else
            //{
            //    await CreateAndBroadcastNewConversationAsync(companyId, senderId, username, messageText, providerMessageId, source);
            //}

            
            if (conversation != null && conversation.IsActive)
            {
                if (!string.IsNullOrEmpty(conversation.AssignedUserId))
                {
                    if (conversation.AssignedUserId == "AI")
                    {
                        // ✅ Conversation is still with AI, let AI handle this message
                        await HandleAIConversation(conversation, messageText, providerMessageId);
                    }
                    else
                    {
                        // ✅ Conversation assigned to a human agent, send message to them
                        await AddMessageToConversationAsync(conversation, senderId, messageText, providerMessageId);
                    }
                }
            }
            else
            {
                // ✅ Create a new conversation and assign it to AI first
                await CreateAndAssignToAI(companyId, senderId, username, messageText, providerMessageId, source);
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



        private async Task CreateAndAssignToAI(int companyId, string senderId, string username, string messageText, string providerMessageId, string source)
        {
            var conversation = new Conversation
            {
                Id = Guid.NewGuid().ToString(),
                SenderId = senderId,
                SenderUsername = username,
                CompanyId = companyId.ToString(),
                IsActive = true,
                AssignedUserId = "AI", // 🚀 Assign AI as the first responder
                Source = source
            };

            _dbContext.Conversations.Add(conversation);
            await _dbContext.SaveChangesAsync();            

            await HandleAIConversation(conversation, messageText, providerMessageId);
        }

        private async Task HandleAIConversation(Conversation conversation, string messageText, string providerMessageId)
        {
            var message = CreateMessage(conversation.Id, conversation.SenderId, "Customer", messageText, providerMessageId);

            _dbContext.Messages.Add(message);
            await _dbContext.SaveChangesAsync();

            var (answered, response, targetTeamId) = await _aiChatBotService.HandleUserQueryAsync(messageText, conversation.CompanyId, conversation.Id);

            if (answered)
            {
                if (!string.IsNullOrEmpty(targetTeamId))
                {
                    // 🚀 AI detected a request for redirection to a specific team
                    await EscalateCompanyTeam(conversation, conversation.SenderId, messageText, providerMessageId, targetTeamId);
                }
                else
                {
                    // ✅ AI handled the message → send response back
                    var aiMessage = CreateMessage(conversation.Id, "AI", "AI Assistant", response, providerMessageId);
                    _dbContext.Messages.Add(aiMessage);
                    await _dbContext.SaveChangesAsync();
                    await SendAIResponseToPlatform(conversation.Source, conversation.SenderId, response, conversation.CompanyId, providerMessageId);
                }
            }
            else
            {
                // ❌ AI couldn't handle → escalate to a general human agent
                await SendAIResponseToPlatform(conversation.Source, conversation.SenderId, response, conversation.CompanyId, providerMessageId);
            }
        }

        private async Task EscalateCompanyTeam(Conversation conversation, string senderId, string messageText, string providerMessageId, string targetTeamId)
        {
            // ✅ Update conversation to assign it to the detected team
            conversation.AssignedTeamId = targetTeamId; // Store team ID for routing
            await _dbContext.SaveChangesAsync();

            // ✅ Notify human agents in the **specific team group**, not the entire company
            await _chatHub.Clients.Group($"Team_{targetTeamId}").SendAsync("NewConversationAdded", conversation);

            _logger.LogInformation($"Escalated conversation {conversation.Id} to team {targetTeamId}.");

            // ✅ Notify user about escalation
            string escalationMessage = $"Your request is being redirected to the **{targetTeamId}** team. Please wait for an available agent.";
            await SendAIResponseToPlatform(conversation.Source, senderId, escalationMessage, conversation.CompanyId, providerMessageId);
        }


        private async Task SendAIResponseToPlatform(string source, string recipientId, string response, string companyId, string localMessageId)
        {
            switch (source)
            {
                case "Facebook":
                    var facebookService = _dbContext.GetService<FacebookService>();
                    if (facebookService != null)
                    {
                        await facebookService.SendMessageToFacebookAsync(recipientId, response, companyId, localMessageId);
                    }
                    break;

                case "WhatsApp":
                    var whatsappService = _dbContext.GetService<WhatsAppService>();
                    if (whatsappService != null)
                    {
                        await whatsappService.SendMessageToWhatsAppAsync(recipientId, response, companyId, localMessageId);
                    }
                    break;

                default:
                    _logger.LogWarning($"Unknown source: {source}. Message not sent.");
                    break;
            }
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
