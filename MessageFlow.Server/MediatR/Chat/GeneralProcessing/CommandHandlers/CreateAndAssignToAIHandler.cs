using MediatR;
using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Services;
using MessageFlow.Server.MediatR.Chat.GeneralProcessing.Commands;

namespace MessageFlow.Server.MediatR.Chat.GeneralProcessing.CommandHandlers
{
    public class CreateAndAssignToAIHandler : IRequestHandler<CreateAndAssignToAICommand, Unit>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMediator _mediator;

        public CreateAndAssignToAIHandler(
            IUnitOfWork unitOfWork,
            IMediator mediator)
        {
            _unitOfWork = unitOfWork;
            _mediator = mediator;
        }

        public async Task<Unit> Handle(CreateAndAssignToAICommand request, CancellationToken cancellationToken)
        {
            var conversation = new Conversation
            {
                Id = Guid.NewGuid().ToString(),
                SenderId = request.SenderId,
                SenderUsername = request.Username,
                CompanyId = request.CompanyId,
                IsActive = true,
                AssignedUserId = "AI",
                Source = request.Source
            };

            await _unitOfWork.Conversations.AddEntityAsync(conversation);
            await _unitOfWork.SaveChangesAsync();

            await _mediator.Send(new HandleAIConversationCommand(
                conversation,
                request.MessageText,
                request.ProviderMessageId
            ), cancellationToken);

            return Unit.Value;
        }
    }
}