using MessageFlow.Client.Components;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

public class ChatHub : Hub
{
    // Track online users with company and team info
    private static ConcurrentDictionary<string, UserConnectionInfo> OnlineUsers = new();

    public override async Task OnConnectedAsync()
    {
        var user = Context.User;

        if (!user.IsInRole("Agent") && !user.IsInRole("Manager") && !user.IsInRole("Admin") && !user.IsInRole("SuperAdmin"))
        {
            Context.Abort();
            return;
        }

        var userId = Context.UserIdentifier;
        var companyId = Context.GetHttpContext()?.Request.Query["companyId"];
        var teamsJson = Context.GetHttpContext()?.Request.Query["teams"];

        Console.WriteLine($"User connected: userId={userId}, companyId={companyId}, teams={teamsJson}");

        if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(companyId) && !string.IsNullOrEmpty(teamsJson))
        {
            var teams = System.Text.Json.JsonSerializer.Deserialize<List<Team>>(teamsJson);

            if (teams != null)
            {
                var userName = Context.User?.Identity?.Name ?? "Unknown User";

                await Groups.AddToGroupAsync(Context.ConnectionId, $"Company_{companyId}");
                Console.WriteLine($"Added connection {Context.ConnectionId} to company group Company_{companyId}");

                OnlineUsers[Context.ConnectionId] = new UserConnectionInfo
                {
                    UserId = userId,
                    UserName = userName,
                    UserTeams = teams,
                    CompanyId = companyId
                };

                await BroadcastTeamMembers(companyId);
            }

        }

        await base.OnConnectedAsync();
    }


    private async Task BroadcastTeamMembers(string companyId)
    {
        var companyUsers = OnlineUsers.Values.Where(user => user.CompanyId == companyId);
        foreach (var userInfo in companyUsers)
        {
            var teamMember = new TeamMembers.TeamMember
            {
                Name = userInfo.UserName,
                Team = string.Join(", ", userInfo.UserTeams.Select(t => t.TeamName)),
                Status = "Online"
            };

            // Broadcast the team member to all clients in the company
            await Clients.Group($"Company_{companyId}").SendAsync("AddTeamMember", teamMember);
        }
    }


    private async Task BroadcastUserDisconnected(string companyId)
    {
        if (OnlineUsers.TryGetValue(Context.ConnectionId, out var userInfo))
        {
            var teamMember = new TeamMembers.TeamMember
            {
                Name = userInfo.UserName,
                Team = string.Join(", ", userInfo.UserTeams.Select(t => t.TeamName)),
                Status = "Offline"
            };

            // Broadcast the team member removal to all clients in the same company
            await Clients.Group($"Company_{companyId}").SendAsync("RemoveTeamMember", teamMember);
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var companyId = Context.GetHttpContext()?.Request.Query["companyId"];
        if (!string.IsNullOrEmpty(companyId))
        {
            await BroadcastUserDisconnected(companyId);
        }

        OnlineUsers.TryRemove(Context.ConnectionId, out _);
        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendMessageToAgent(string userId, string message, string companyId, List<int> teamIds)
    {
        foreach (var teamId in teamIds)
        {
            var teamGroupName = $"Company_{companyId}_Team_{teamId}";
            await Clients.Group(teamGroupName).SendAsync("ReceiveMessage", message);
        }
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

    public async Task AssignConversationToUser(string conversationId, string userId)
    {
        await Clients.Group(userId).SendAsync("AssignConversation", conversationId);
    }

    public class Team
    {
        public int TeamId { get; set; }
        public string TeamName { get; set; } = string.Empty;
    }

    public class UserConnectionInfo
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public List<Team> UserTeams { get; set; } = new();
        public string CompanyId { get; set; } = string.Empty;
    }


}
