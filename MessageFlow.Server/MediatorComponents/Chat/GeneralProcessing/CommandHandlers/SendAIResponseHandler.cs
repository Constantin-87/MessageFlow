using MediatR;
using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Services;
using MessageFlow.Server.MediatorComponents.Chat.FacebookProcessing.Commands;
using MessageFlow.Server.MediatorComponents.Chat.GeneralProcessing.Commands;
using MessageFlow.Server.MediatorComponents.Chat.WhatsappProcessing.Commands;

namespace MessageFlow.Server.MediatorComponents.Chat.GeneralProcessing.CommandHandlers
{
    public class SendAIResponseHandler : IRequestHandler<SendAIResponseCommand, Unit>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<SendAIResponseHandler> _logger;
        private readonly IMediator _mediator;

        public SendAIResponseHandler(
            IUnitOfWork unitOfWork,
            ILogger<SendAIResponseHandler> logger,
            IMediator mediator)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _mediator = mediator;
        }

        public async Task<Unit> Handle(SendAIResponseCommand request, CancellationToken cancellationToken)
        {
            var message = new Message
            {
                Id = Guid.NewGuid().ToString(),
                ProviderMessageId = request.ProviderMessageId,
                ConversationId = request.Conversation.Id,
                UserId = "AI",
                Username = "AI Assistant",
                Content = request.Response,
                SentAt = DateTime.UtcNow,
                Status = ""
            };

            await _unitOfWork.Messages.AddEntityAsync(message);
            await _unitOfWork.SaveChangesAsync();

            switch (request.Conversation.Source)
            {
                case "Facebook":
                    await _mediator.Send(new SendMessageToFacebookCommand(
                        request.Conversation.SenderId,
                        request.Response,
                        request.Conversation.CompanyId,
                        request.ProviderMessageId));
                    break;

                case "WhatsApp":
                    await _mediator.Send(new SendMessageToWhatsAppCommand(
                        request.Conversation.SenderId,
                        request.Response,
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
