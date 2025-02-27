using MessageFlow.Infrastructure.Mediator.Commands.Chat;
using MessageFlow.Infrastructure.Mediator.Interfaces;
using MessageFlow.Shared.Interfaces;

namespace MessageFlow.Infrastructure.Mediator.Handlers
{
    public class ProcessMessageStatusUpdateCommandHandler : IRequestHandler<ProcessMessageStatusUpdateCommand, bool>
    {
        private readonly IMessageProcessingService _messageProcessingService;

        public ProcessMessageStatusUpdateCommandHandler(IMessageProcessingService messageProcessingService)
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
