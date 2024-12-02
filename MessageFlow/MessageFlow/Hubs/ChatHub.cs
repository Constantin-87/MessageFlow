using Microsoft.AspNetCore.SignalR;
using System.Text.RegularExpressions;

public class ChatHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier ?? Context.ConnectionId; // Fallback for unauthenticated users
        await Groups.AddToGroupAsync(Context.ConnectionId, userId);
        await base.OnConnectedAsync();
    }

    public async Task SendMessageToAgent(string userId, string message)
    {
        await Clients.Group(userId).SendAsync("ReceiveMessage", message);
    }

    public async Task AddNewConversation(string title)
    {
        var conversation = new { Id = Guid.NewGuid().ToString(), Title = title };
        await Clients.All.SendAsync("NewConversationAdded", conversation);
    }

    public async Task UpdateTeamMembers(List<string> memberNames)
    {
        var members = memberNames.Select(name => new { Name = name, Status = "Online" });
        await Clients.All.SendAsync("UpdateTeamMembers", members);
    }
}
