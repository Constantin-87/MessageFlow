﻿using MediatR;
using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Services;
using MessageFlow.Server.DataTransferObjects.Internal;
using MessageFlow.Server.MediatR.Chat.AiBotProcessing.CommandHandlers;
using MessageFlow.Server.MediatR.Chat.AiBotProcessing.Commands;
using MessageFlow.Server.MediatR.Chat.GeneralProcessing.Commands;

namespace MessageFlow.Server.MediatR.Chat.GeneralProcessing.CommandHandlers
{
    public class HandleAIConversationHandler : IRequestHandler<HandleAIConversationCommand, Unit>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMediator _mediator;
        private readonly ILogger<HandleAIConversationHandler> _logger;

        public HandleAIConversationHandler(
            IUnitOfWork unitOfWork,
            IMediator mediator,
            ILogger<HandleAIConversationHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _mediator = mediator;
            _logger = logger;
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
            var result = new UserQueryResponseDTO();
            try
            {
                result = await _mediator.Send(new HandleUserQueryCommand(
                    request.MessageText,
                    request.Conversation.CompanyId,
                    request.Conversation.Id
                ), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error dispatching HandleUserQueryCommand");
                throw;
            }            

            if (result.Answered && !string.IsNullOrEmpty(result.TargetTeamId))
            {
                await _mediator.Send(new EscalateCompanyTeamCommand(
                    request.Conversation,
                    request.Conversation.SenderId,
                    request.ProviderMessageId,
                    result.TargetTeamId,
                    result.TargetTeamName), cancellationToken);
            }
            else
            {
                await _mediator.Send(new SendAIResponseCommand(
                    request.Conversation,
                    result.RawResponse,
                    request.ProviderMessageId), cancellationToken);
            }

            return Unit.Value;
        }
    }
}