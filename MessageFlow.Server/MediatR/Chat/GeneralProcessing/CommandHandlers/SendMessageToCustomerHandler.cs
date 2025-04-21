using MediatR;
using AutoMapper;
using MessageFlow.DataAccess.Models;
using MessageFlow.Server.MediatR.Chat.FacebookProcessing.Commands;
using MessageFlow.DataAccess.Services;
using MessageFlow.Server.MediatR.Chat.GeneralProcessing.Commands;
using MessageFlow.Server.MediatR.Chat.WhatsappProcessing.Commands;

namespace MessageFlow.Server.MediatR.Chat.Messaging.Handlers;

public class SendMessageToCustomerHandler : IRequestHandler<SendMessageToCustomerCommand, (bool Success, string ErrorMessage)>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IMediator _mediator;
    private readonly ILogger<SendMessageToCustomerHandler> _logger;

    public SendMessageToCustomerHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IMediator mediator,
        ILogger<SendMessageToCustomerHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<(bool Success, string ErrorMessage)> Handle(SendMessageToCustomerCommand request, CancellationToken cancellationToken)
    {
        var dto = request.MessageDto;

        if (dto == null)
        {
            _logger.LogWarning("Message DTO is null.");
            return (false, "Message is null.");
        }

        var conversation = await _unitOfWork.Conversations.GetConversationByIdAsync(dto.ConversationId);

        if (conversation == null)
        {
            _logger.LogWarning("Conversation not found for ID: {ConversationId}", dto.ConversationId);
            return (false, $"Conversation {dto.ConversationId} not found.");
        }

        var message = _mapper.Map<Message>(dto);

        try
        {
            await _unitOfWork.Messages.AddEntityAsync(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while adding message to the database.");
            throw;
        }

        await _unitOfWork.SaveChangesAsync();

        switch (conversation.Source)
        {
            case "Facebook":
                await _mediator.Send(
                    new SendMessageToFacebookCommand(
                        conversation.SenderId,
                        message.Content,
                        conversation.CompanyId,
                        message.Id
                    ),
                    cancellationToken
                );
            break;

            case "WhatsApp":
                await _mediator.Send(
                    new SendMessageToWhatsAppCommand(
                         conversation.SenderId,
                         message.Content,
                         conversation.CompanyId,
                         message.Id
                     ), 
                    cancellationToken
                );
            break;

            default:
                var sourceInfo = conversation.Source ?? "<null>";
                _logger.LogWarning("Unknown platform: {Source}", sourceInfo);
            return (false, $"Unknown source: {sourceInfo}");

        }
        return (true, "Message sent.");
    }
}