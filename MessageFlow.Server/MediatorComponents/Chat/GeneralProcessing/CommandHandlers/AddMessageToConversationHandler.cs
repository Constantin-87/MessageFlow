using MediatR;
using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Services;
using Microsoft.AspNetCore.SignalR;
using MessageFlow.Server.MediatorComponents.Chat.GeneralProcessing.Commands;

namespace MessageFlow.Server.MediatorComponents.Chat.GeneralProcessing.CommandHandlers
{
    public class AddMessageToConversationHandler : IRequestHandler<AddMessageToConversationCommand, Unit>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHubContext<ChatHub> _chatHub;
        private readonly ILogger<AddMessageToConversationHandler> _logger;

        public AddMessageToConversationHandler(
            IUnitOfWork unitOfWork,
            IHubContext<ChatHub> chatHub,
            ILogger<AddMessageToConversationHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _chatHub = chatHub;
            _logger = logger;
        }

        public async Task<Unit> Handle(AddMessageToConversationCommand request, CancellationToken cancellationToken)
        {
            var message = new Message
            {
                Id = Guid.NewGuid().ToString(),
                ProviderMessageId = request.ProviderMessageId,
                ConversationId = request.Conversation.Id,
                UserId = request.SenderId,
                Username = "Customer",
                Content = request.MessageText,
                SentAt = DateTime.UtcNow,
                Status = ""
            };

            await _unitOfWork.Messages.AddEntityAsync(message);
            await _unitOfWork.SaveChangesAsync();

            if (!string.IsNullOrEmpty(request.Conversation.AssignedUserId))
            {
                await _chatHub.Clients.User(request.Conversation.AssignedUserId)
                    .SendAsync("SendMessageToAssignedUser", request.Conversation, message);
            }

            return Unit.Value;
        }
    }
}
