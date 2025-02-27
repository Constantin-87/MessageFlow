using MessageFlow.Infrastructure.Mediator.Commands.Chat;
using MessageFlow.Infrastructure.Mediator.Interfaces;
using MessageFlow.Shared.Interfaces;

namespace MessageFlow.Infrastructure.Mediator.Handlers.Chat
{
    public class SendFacebookMessageCommandHandler : IRequestHandler<SendFacebookMessageCommand, bool>
    {
        private readonly IFacebookService _facebookService;

        public SendFacebookMessageCommandHandler(IFacebookService facebookService)
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
