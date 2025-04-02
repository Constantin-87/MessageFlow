using MessageFlow.Infrastructure.Mediator.Interfaces;
using MessageFlow.Server.Services.Interfaces;
using MessageFlow.Server.MediatorComponents.Chat.Commands;

namespace MessageFlow.Server.MediatorComponents.Chat.CommandHandlers
{
    public class SendFacebookMessageHandler : IRequestHandler<SendFacebookMessageCommand, bool>
    {
        private readonly IFacebookService _facebookService;

        public SendFacebookMessageHandler(IFacebookService facebookService)
        {
            _facebookService = facebookService;
        }

        public async Task<bool> Handle(SendFacebookMessageCommand request, CancellationToken cancellationToken)
        {
            await _facebookService.SendMessageToFacebookAsync(
                request.RecipientId, request.MessageText, request.CompanyId, request.LocalMessageId);
            return true;
        }
    }
}
