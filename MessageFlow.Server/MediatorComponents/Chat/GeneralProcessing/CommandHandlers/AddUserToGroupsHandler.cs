using MediatR;
using Microsoft.AspNetCore.SignalR;
using MessageFlow.Shared.DTOs;
using System.Collections.Concurrent;
using MessageFlow.Server.MediatorComponents.Chat.GeneralProcessing.Commands;

namespace MessageFlow.Server.MediatorComponents.Chat.GeneralProcessing.Handlers;

public class AddUserToGroupsHandler : IRequestHandler<AddUserToGroupsCommand, Unit>
{
    private readonly IHubContext<ChatHub> _hubContext;

    // Must be static to match usage in ChatHub
    private static readonly ConcurrentDictionary<string, ApplicationUserDTO> OnlineUsers = ChatHub.OnlineUsers;

    public AddUserToGroupsHandler(IHubContext<ChatHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task<Unit> Handle(AddUserToGroupsCommand request, CancellationToken cancellationToken)
    {
        var user = request.ApplicationUser;
        var connectionId = request.ConnectionId;

        await _hubContext.Groups.AddToGroupAsync(connectionId, $"Company_{user.CompanyId}", cancellationToken);

        foreach (var teamId in user.TeamIds)
        {
            await _hubContext.Groups.AddToGroupAsync(connectionId, $"Team_{teamId}", cancellationToken);
        }

        OnlineUsers[connectionId] = user;

        Console.WriteLine($"Added user {user.Id} to Company_{user.CompanyId} and team groups.");
        return Unit.Value;
    }
}
