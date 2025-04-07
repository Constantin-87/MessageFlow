using MediatR;
using MessageFlow.DataAccess.Services;
using Microsoft.AspNetCore.SignalR;
using System.Text.Json;
using MessageFlow.DataAccess.Models;
using MessageFlow.Server.MediatorComponents.Chat.GeneralProcessing.Commands;

namespace MessageFlow.Server.MediatorComponents.Chat.GeneralProcessing.CommandHandlers
{
    public class ProcessMessageStatusUpdateHandler : IRequestHandler<ProcessMessageStatusUpdateCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHubContext<ChatHub> _chatHub;
        private readonly ILogger<ProcessMessageStatusUpdateHandler> _logger;

        public ProcessMessageStatusUpdateHandler(
            IUnitOfWork unitOfWork,
            IHubContext<ChatHub> chatHub,
            ILogger<ProcessMessageStatusUpdateHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _chatHub = chatHub;
            _logger = logger;
        }

        public async Task<bool> Handle(ProcessMessageStatusUpdateCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var messageId = request.StatusElement.GetProperty("id").GetString();
                var timestamp = ParseTimestamp(request.StatusElement);
                var message = await _unitOfWork.Messages.GetMessageByProviderIdAsync(messageId);

                if (message == null)
                {
                    _logger.LogWarning($"Message with ID {messageId} not found.");
                    return false;
                }

                await UpdateMessageStatusAsync(message, request.StatusElement);
                message.ChangedAt = timestamp;
                _unitOfWork.Messages.UpdateEntityAsync(message);
                await _unitOfWork.SaveChangesAsync();

                await NotifyAssignedUserAsync(message, messageId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing status update: {ex.Message}");
                return false;
            }
        }

        private DateTime ParseTimestamp(JsonElement statusElement)
        {
            if (statusElement.TryGetProperty("timestamp", out var tsProp) &&
                long.TryParse(tsProp.GetString(), out var unix))
            {
                return DateTimeOffset.FromUnixTimeSeconds(unix).UtcDateTime;
            }
            return DateTime.UtcNow;
        }

        private async Task UpdateMessageStatusAsync(Message message, JsonElement statusElement)
        {
            var order = new List<string> { "SentToProvider", "sent", "delivered", "read" };
            var status = statusElement.GetProperty("status").GetString();

            if (status == "error")
            {
                LogErrorDetails(statusElement);
                message.Status = "error";
            }
            else if (order.IndexOf(status) > order.IndexOf(message.Status))
            {
                message.Status = status;
            }
        }

        private void LogErrorDetails(JsonElement statusElement)
        {
            if (statusElement.TryGetProperty("errors", out var errors))
            {
                foreach (var err in errors.EnumerateArray())
                {
                    if (err.TryGetProperty("message", out var msg))
                    {
                        _logger.LogError($"Error: {msg.GetString()}");
                    }
                }
            }
        }

        private async Task NotifyAssignedUserAsync(Message message, string messageId)
        {
            var conversation = await _unitOfWork.Conversations.GetConversationByIdAsync(message.ConversationId);
            if (conversation?.AssignedUserId != null)
            {
                if (ChatHub.OnlineUsers.Values.Any(u => u.Id == conversation.AssignedUserId))
                {
                    await _chatHub.Clients.User(conversation.AssignedUserId)
                        .SendAsync("MessageStatusUpdated", message.Id, message.Status);
                }
            }
        }
    }

}
