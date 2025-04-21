using MediatR;
using Microsoft.AspNetCore.SignalR;
using MessageFlow.Shared.DTOs;
using AutoMapper;
using MessageFlow.DataAccess.Services;
using MessageFlow.Server.MediatR.Chat.GeneralProcessing.Commands;

namespace MessageFlow.Server.MediatR.Chat.GeneralProcessing.CommandHandlers;

public class AssignConversationToUserHandler : IRequestHandler<AssignConversationToUserCommand, (bool Success, string ErrorMessage)>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHubContext<ChatHub> _hubContext;
    private readonly IMapper _mapper;

    public AssignConversationToUserHandler(IUnitOfWork unitOfWork, IHubContext<ChatHub> hubContext, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _hubContext = hubContext;
        _mapper = mapper;
    }

    public async Task<(bool Success, string ErrorMessage)> Handle(AssignConversationToUserCommand request, CancellationToken cancellationToken)
    {
        var conversation = await _unitOfWork.Conversations.GetConversationByIdAsync(request.ConversationId);

        if (conversation == null)
            return (false, "Conversation not found.");

        conversation.AssignedUserId = request.UserId;
        conversation.IsAssigned = true;

        await _unitOfWork.Conversations.UpdateEntityAsync(conversation);
        await _unitOfWork.SaveChangesAsync();

        var conversationDto = _mapper.Map<ConversationDTO>(conversation);
        conversationDto.Messages = conversationDto.Messages?.OrderBy(m => m.SentAt).ToList();

        // Notify assigned user
        await _hubContext.Clients.User(request.UserId)
            .SendAsync("AssignConversation", conversationDto, cancellationToken);

        // Remove from unassigned pool
        await _hubContext.Clients.Group($"Company_{conversation.CompanyId}")
            .SendAsync("RemoveNewConversation", conversationDto, cancellationToken);

        return (true, "Conversation assigned.");
    }
}
