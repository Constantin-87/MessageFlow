using MediatR;
using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Services;
using MessageFlow.Server.MediatorComponents.Chat.GeneralProcessing.Commands;
using MessageFlow.Server.Services;

namespace MessageFlow.Server.MediatorComponents.Chat.GeneralProcessing.CommandHandlers
{
    public class HandleAIConversationHandler : IRequestHandler<HandleAIConversationCommand, Unit>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMediator _mediator;
        private readonly AIChatBotService _aiChatBotService;

        public HandleAIConversationHandler(
            IUnitOfWork unitOfWork,
            IMediator mediator,
            AIChatBotService aiChatBotService)
        {
            _unitOfWork = unitOfWork;
            _mediator = mediator;
            _aiChatBotService = aiChatBotService;
        }

        public async Task<Unit> Handle(HandleAIConversationCommand request, CancellationToken cancellationToken)
        {
            var message = new Message
            {
                Id = Guid.NewGuid().ToString(),
                ProviderMessageId = request.ProviderMessageId,
                ConversationId = request.Conversation.Id,
                UserId = request.Conversation.SenderId,
                Username = "Customer",
                Content = request.MessageText,
                SentAt = DateTime.UtcNow,
                Status = ""
            };

            await _unitOfWork.Messages.AddEntityAsync(message);
            await _unitOfWork.SaveChangesAsync();

            var (answered, response, targetTeamId) = await _aiChatBotService.HandleUserQueryAsync(
                request.MessageText, request.Conversation.CompanyId, request.Conversation.Id);

            if (answered && !string.IsNullOrEmpty(targetTeamId))
            {
                await _mediator.Send(new EscalateCompanyTeamCommand(
                    request.Conversation,
                    request.Conversation.SenderId,
                    request.ProviderMessageId,
                    targetTeamId), cancellationToken);
            }
            else
            {
                await _mediator.Send(new SendAIResponseCommand(
                    request.Conversation,
                    response,
                    request.ProviderMessageId), cancellationToken);
            }

            return Unit.Value;
        }
    }
}
