using AutoMapper;
using MediatR;
using MessageFlow.Server.MediatR.Chat.GeneralProcessing.Commands;
using MessageFlow.Shared.DTOs;
using MessageFlow.DataAccess.Services;
using Microsoft.AspNetCore.SignalR;

namespace MessageFlow.Server.MediatR.Chat.GeneralProcessing.Handlers;

public class LoadUserConversationsHandler : IRequestHandler<LoadUserConversationsCommand, Unit>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public LoadUserConversationsHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Unit> Handle(LoadUserConversationsCommand request, CancellationToken cancellationToken)
    {
        var assigned = await _unitOfWork.Conversations.GetAssignedConversationsAsync(request.UserId, request.CompanyId);
        var unassigned = await _unitOfWork.Conversations.GetUnassignedConversationsAsync(request.CompanyId);

        var assignedDto = _mapper.Map<List<ConversationDTO>>(assigned);
        var unassignedDto = _mapper.Map<List<ConversationDTO>>(unassigned);

        await request.Caller.SendAsync("LoadAssignedConversations", assignedDto, cancellationToken);
        await request.Caller.SendAsync("LoadNewConversations", unassignedDto, cancellationToken);

        return Unit.Value;
    }
}
