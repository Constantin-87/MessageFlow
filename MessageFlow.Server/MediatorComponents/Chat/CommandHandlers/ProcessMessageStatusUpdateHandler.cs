using MessageFlow.Infrastructure.Mediator.Interfaces;
using MessageFlow.Server.Services.Interfaces;
using MessageFlow.Server.MediatorComponents.Chat.Commands;

namespace MessageFlow.Server.MediatorComponents.Chat.CommandHandlers
{
    public class ProcessMessageStatusUpdateHandler : IRequestHandler<ProcessMessageStatusUpdateCommand, bool>
    {
        private readonly IMessageProcessingService _messageProcessingService;

        public ProcessMessageStatusUpdateHandler(IMessageProcessingService messageProcessingService)
        {
            _messageProcessingService = messageProcessingService;
        }

        public async Task<bool> Handle(ProcessMessageStatusUpdateCommand request, CancellationToken cancellationToken)
        {
            await _messageProcessingService.ProcessMessageStatusUpdateAsync(request.StatusElement, request.Platform);
            return true;
        }
    }
}
