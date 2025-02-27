using MessageFlow.Infrastructure.Mediator.Commands.Chat;
using MessageFlow.Infrastructure.Mediator.Interfaces;
using MessageFlow.Shared.Interfaces;

namespace MessageFlow.Infrastructure.Mediator.Handlers
{
    public class ProcessMessageCommandHandler : IRequestHandler<ProcessMessageCommand, bool>
    {
        private readonly IMessageProcessingService _messageProcessingService;

        public ProcessMessageCommandHandler(IMessageProcessingService messageProcessingService)
        {
            _messageProcessingService = messageProcessingService;
        }

        public async Task<bool> Handle(ProcessMessageCommand request, CancellationToken cancellationToken)
        {
            await _messageProcessingService.ProcessMessageAsync(
                request.CompanyId, request.SenderId, request.Username, request.MessageText, request.ProviderMessageId, request.Source);

            return true;
        }
    }
}
