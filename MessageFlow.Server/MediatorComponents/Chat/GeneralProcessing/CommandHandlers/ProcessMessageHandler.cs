using MediatR;
using MessageFlow.DataAccess.Services;
using MessageFlow.Server.MediatorComponents.Chat.GeneralProcessing.Commands;

namespace MessageFlow.Server.MediatorComponents.Chat.GeneralProcessing.CommandHandlers
{
    public class ProcessMessageHandler : IRequestHandler<ProcessMessageCommand, Unit>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMediator _mediator;

        public ProcessMessageHandler(
            IUnitOfWork unitOfWork,
            IMediator mediator)
        {
            _unitOfWork = unitOfWork;
            _mediator = mediator;
        }

        public async Task<Unit> Handle(ProcessMessageCommand request, CancellationToken cancellationToken)
        {
            var conversation = await _unitOfWork.Conversations
                .GetConversationBySenderAndCompanyAsync(request.SenderId, request.CompanyId);

            if (conversation != null && conversation.IsActive)
            {
                if (!string.IsNullOrEmpty(conversation.AssignedUserId))
                {
                    if (conversation.AssignedUserId == "AI")
                    {
                        await _mediator.Send(new HandleAIConversationCommand(
                            conversation,
                            request.MessageText,
                            request.ProviderMessageId), cancellationToken);
                    }
                    else
                    {
                        await _mediator.Send(new AddMessageToConversationCommand(
                            conversation,
                            request.SenderId,
                            request.MessageText,
                            request.ProviderMessageId), cancellationToken);
                    }
                }
            }
            else
            {
                await _mediator.Send(new CreateAndAssignToAICommand(
                    request.CompanyId,
                    request.SenderId,
                    request.Username,
                    request.MessageText,
                    request.ProviderMessageId,
                    request.Source), cancellationToken);
            }

            return Unit.Value;
        }
    }
}
