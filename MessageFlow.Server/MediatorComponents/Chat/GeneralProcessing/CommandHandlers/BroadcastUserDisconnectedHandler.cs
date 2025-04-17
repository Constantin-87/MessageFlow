using MediatR;
using Microsoft.AspNetCore.SignalR;
using MessageFlow.Shared.DTOs;
using MessageFlow.Server.MediatorComponents.Chat.GeneralProcessing.Commands;
using System.Collections.Concurrent;

namespace MessageFlow.Server.MediatorComponents.Chat.GeneralProcessing.Handlers;

public class BroadcastUserDisconnectedHandler : IRequestHandler<BroadcastUserDisconnectedCommand, Unit>
{
    private readonly IHubContext<ChatHub> _hubContext;

    // Reference to static OnlineUsers
    private static readonly ConcurrentDictionary<string, ApplicationUserDTO> OnlineUsers = ChatHub.OnlineUsers;

    public BroadcastUserDisconnectedHandler(IHubContext<ChatHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task<Unit> Handle(BroadcastUserDisconnectedCommand request, CancellationToken cancellationToken)
    {
        if (OnlineUsers.TryGetValue(request.ConnectionId, out var userInfo))
        {
            var disconnectedUser = new ApplicationUserDTO
            {
                Id = userInfo.Id,
                UserName = userInfo.UserName,
                CompanyId = userInfo.CompanyId,
                Role = userInfo.Role,
                LockoutEnabled = userInfo.LockoutEnabled,
                TeamsDTO = userInfo.TeamsDTO,
                LastActivity = DateTime.UtcNow
            };

            await _hubContext
                .Clients
                .Group($"Company_{request.CompanyId}")
                .SendAsync("RemoveTeamMember", disconnectedUser, cancellationToken);
        }

        return Unit.Value;
    }
}
