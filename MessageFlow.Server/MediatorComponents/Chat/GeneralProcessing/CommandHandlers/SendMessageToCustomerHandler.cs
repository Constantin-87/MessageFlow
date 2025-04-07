using MediatR;
using AutoMapper;
using MessageFlow.DataAccess.Models;
using MessageFlow.Server.MediatorComponents.Chat.FacebookProcessing.Commands;
using MessageFlow.DataAccess.Services;
using MessageFlow.Server.MediatorComponents.Chat.GeneralProcessing.Commands;
using MessageFlow.Server.MediatorComponents.Chat.WhatsappProcessing.Commands;

namespace MessageFlow.Server.MediatorComponents.Chat.Messaging.Handlers;

public class SendMessageToCustomerHandler : IRequestHandler<SendMessageToCustomerCommand, (bool Success, string ErrorMessage)>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IMediator _mediator;

    public SendMessageToCustomerHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IMediator mediator)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _mediator = mediator;
    }

    public async Task<(bool Success, string ErrorMessage)> Handle(SendMessageToCustomerCommand request, CancellationToken cancellationToken)
    {
        var dto = request.MessageDto;

        if (dto == null)
            return (false, "Message is null.");

        var conversation = await _unitOfWork.Conversations.GetConversationByIdAsync(dto.ConversationId);
        if (conversation == null)
            return (false, $"Conversation {dto.ConversationId} not found.");

        var message = _mapper.Map<Message>(dto);

        await _unitOfWork.Messages.AddEntityAsync(message);
        await _unitOfWork.SaveChangesAsync();

        switch (conversation.Source)
        {
            case "Facebook":
                await _mediator.Send(new SendMessageToFacebookCommand(
                    conversation.SenderId,
                    message.Content,
                    conversation.CompanyId,
                message.Id), cancellationToken);
                break;

            case "WhatsApp":
                await _mediator.Send(new SendMessageToWhatsAppCommand(
                     conversation.SenderId,
                     message.Content,
                     conversation.CompanyId,
                     message.Id), cancellationToken);

                //await _whatsAppService.SendMessageToWhatsAppAsync(
                //    conversation.SenderId,
                //    message.Content,
                //    conversation.CompanyId,
                //    message.Id);
                break;

            default:
                return (false, $"Unknown source: {conversation.Source}");
        }

        return (true, "Message sent.");
    }
}
