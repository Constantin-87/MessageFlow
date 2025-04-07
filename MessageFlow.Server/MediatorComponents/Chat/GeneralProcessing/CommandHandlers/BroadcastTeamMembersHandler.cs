using System.Collections.Concurrent;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using MessageFlow.Shared.DTOs;
using MessageFlow.Server.MediatorComponents.Chat.GeneralProcessing.Commands;

namespace MessageFlow.Server.MediatorComponents.Chat.GeneralProcessing.Handlers;

public class BroadcastTeamMembersHandler : IRequestHandler<BroadcastTeamMembersCommand, Unit>
{
    private readonly IHubContext<ChatHub> _hubContext;

    // Access the static dictionary from ChatHub
    private static readonly ConcurrentDictionary<string, ApplicationUserDTO> OnlineUsers = ChatHub.OnlineUsers;

    public BroadcastTeamMembersHandler(IHubContext<ChatHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task<Unit> Handle(BroadcastTeamMembersCommand request, CancellationToken cancellationToken)
    {
        var teamMembers = OnlineUsers.Values.Where(u => u.CompanyId == request.CompanyId);

        foreach (var member in teamMembers)
        {
            await _hubContext.Clients.Group($"Company_{request.CompanyId}")
                .SendAsync("AddTeamMember", member, cancellationToken);
        }

        return Unit.Value;
    }
}
