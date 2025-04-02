using MessageFlow.Infrastructure.Mediator.Interfaces;
using MessageFlow.Server.Services.Interfaces;
using MessageFlow.Server.MediatorComponents.Chat.Commands;

namespace MessageFlow.Server.MediatorComponents.Chat.CommandHandlers
{
    public class ProcessMessageHandler : IRequestHandler<ProcessMessageCommand, bool>
    {
        private readonly IMessageProcessingService _messageProcessingService;

        public ProcessMessageHandler(IMessageProcessingService messageProcessingService)
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
