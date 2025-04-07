using MediatR;
using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Services;
using Microsoft.AspNetCore.SignalR;
using MessageFlow.Server.MediatorComponents.Chat.GeneralProcessing.Commands;
using MessageFlow.Server.MediatorComponents.Chat.FacebookProcessing.Commands;
using MessageFlow.Server.MediatorComponents.Chat.WhatsappProcessing.Commands;

namespace MessageFlow.Server.MediatorComponents.Chat.GeneralProcessing.CommandHandlers
{
    public class EscalateCompanyTeamHandler : IRequestHandler<EscalateCompanyTeamCommand, Unit>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHubContext<ChatHub> _chatHub;
        private readonly ILogger<EscalateCompanyTeamHandler> _logger;
        private readonly IMediator _mediator;

        public EscalateCompanyTeamHandler(
            IUnitOfWork unitOfWork,
            IHubContext<ChatHub> chatHub,
            ILogger<EscalateCompanyTeamHandler> logger,
            IMediator mediator)
        {
            _unitOfWork = unitOfWork;
            _chatHub = chatHub;
            _logger = logger;
            _mediator = mediator;
        }

        public async Task<Unit> Handle(EscalateCompanyTeamCommand request, CancellationToken cancellationToken)
        {
            request.Conversation.AssignedTeamId = request.TargetTeamId;
            await _unitOfWork.SaveChangesAsync();

            await _chatHub.Clients.Group($"Team_{request.TargetTeamId}")
                .SendAsync("NewConversationAdded", request.Conversation);

            _logger.LogInformation($"Escalated conversation {request.Conversation.Id} to team {request.TargetTeamId}.");

            var escalationMessage = $"Your request is being redirected to the **{request.TargetTeamId}** team. Please wait for an available agent.";

            // Send escalation message using AI response logic
            var aiMessage = new Message
            {
                Id = Guid.NewGuid().ToString(),
                ProviderMessageId = request.ProviderMessageId,
                ConversationId = request.Conversation.Id,
                UserId = "AI",
                Username = "AI Assistant",
                Content = escalationMessage,
                SentAt = DateTime.UtcNow,
                Status = ""
            };

            await _unitOfWork.Messages.AddEntityAsync(aiMessage);
            await _unitOfWork.SaveChangesAsync();

            await _chatHub.Clients.User(request.SenderId)
                .SendAsync("SendMessageToAssignedUser", request.Conversation, aiMessage);

            // Send escalation message to external platform
            switch (request.Conversation.Source)
            {
                case "Facebook":
                    await _mediator.Send(new SendMessageToFacebookCommand(
                        request.SenderId,
                        escalationMessage,
                        request.Conversation.CompanyId,
                        request.ProviderMessageId));
                    break;

                case "WhatsApp":
                    await _mediator.Send(new SendMessageToWhatsAppCommand(
                        request.SenderId,
                        escalationMessage,
                        request.Conversation.CompanyId,
                        request.ProviderMessageId));
                    break;

                default:
                    _logger.LogWarning($"Unknown source: {request.Conversation.Source}");
                    break;
            }

            return Unit.Value;
        }
    }
}
