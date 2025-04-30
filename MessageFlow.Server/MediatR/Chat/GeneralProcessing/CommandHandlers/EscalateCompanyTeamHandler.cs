using MediatR;
using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Services;
using Microsoft.AspNetCore.SignalR;
using MessageFlow.Server.MediatR.Chat.GeneralProcessing.Commands;
using MessageFlow.Server.MediatR.Chat.FacebookProcessing.Commands;
using MessageFlow.Server.MediatR.Chat.WhatsappProcessing.Commands;
using MessageFlow.Shared.DTOs;
using AutoMapper;

namespace MessageFlow.Server.MediatR.Chat.GeneralProcessing.CommandHandlers
{
    public class EscalateCompanyTeamHandler : IRequestHandler<EscalateCompanyTeamCommand, Unit>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHubContext<ChatHub> _chatHub;
        private readonly ILogger<EscalateCompanyTeamHandler> _logger;
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;

        public EscalateCompanyTeamHandler(
            IUnitOfWork unitOfWork,
            IHubContext<ChatHub> chatHub,
            ILogger<EscalateCompanyTeamHandler> logger,
            IMediator mediator,
            IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _chatHub = chatHub;
            _logger = logger;
            _mediator = mediator;
            _mapper = mapper;
        }

        public async Task<Unit> Handle(EscalateCompanyTeamCommand request, CancellationToken cancellationToken)
        {
            request.Conversation.AssignedTeamId = request.TargetTeamId;
            await _unitOfWork.SaveChangesAsync();

            var conversationDto = _mapper.Map<ConversationDTO>(request.Conversation);
            await _chatHub.Clients.Group($"Team_{request.TargetTeamId}")
                .SendAsync("NewConversationAdded", conversationDto);
            var escalationMessage = $"Your request is being redirected to the {request.TargetTeamName} team. Please wait for an available agent.";

            // Send message using AI response logic
            var aiMessage = new Message
            {
                Id = Guid.NewGuid().ToString(),
                ProviderMessageId = request.ProviderMessageId,
                ConversationId = request.Conversation.Id,
                UserId = "AI",
                Username = "AI Assistant",
                Content = escalationMessage,
                SentAt = DateTime.UtcNow,
                Status = "read"
            };

            await _unitOfWork.Messages.AddEntityAsync(aiMessage);
            await _unitOfWork.SaveChangesAsync();

            // Send message to external platform
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