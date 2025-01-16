using MessageFlow.Client.Components;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using MessageFlow.Data;
using MessageFlow.Components.Chat.Services;
using MessageFlow.Models;
using System.Security.Claims;

public class ChatHub : Hub
{
    // Track online users with company and team info
    private static ConcurrentDictionary<string, UserConnectionInfo> OnlineUsers = new();

    private readonly ApplicationDbContext _context;

    public ChatHub(ApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public override async Task OnConnectedAsync()
    {
        try
        {
            if (!IsAuthorized(Context.User))
            {
                Context.Abort();
                return;
            }

            var userId = Context.UserIdentifier;
            var companyId = GetClaimValue("CompanyId");
            var teams = ParseTeams(GetClaimValue("UserTeams"));

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(companyId))
            {
                Console.WriteLine("Missing userId or companyId. Aborting connection.");
                Context.Abort();
                return;
            }

            await AddUserToGroups(userId, companyId, teams);
            await LoadUserConversations(userId, companyId);
            await BroadcastTeamMembers(companyId);

            Console.WriteLine($"Connection established. ConnectionId: {Context.ConnectionId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in OnConnectedAsync: {ex.Message}");
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        try
        {
            var companyId = GetQueryValue("companyId");
            if (!string.IsNullOrEmpty(companyId))
            {
                await BroadcastUserDisconnected(companyId);
            }

            OnlineUsers.TryRemove(Context.ConnectionId, out _);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in OnDisconnectedAsync: {ex.Message}");
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task AssignConversationToUser(string conversationId)
    {
        try
        {
            var conversation = await _context.Conversations
                .Include(c => c.Messages)
                .FirstOrDefaultAsync(c => c.Id == conversationId);

            if (conversation != null)
            {
                var userId = Context.UserIdentifier;
                conversation.AssignedUserId = userId;
                conversation.IsAssigned = true;

                await _context.SaveChangesAsync();
                await NotifyConversationAssignment(conversation, userId);

                Console.WriteLine($"Assigned conversation {conversationId} to user {userId}");
            }
            else
            {
                Console.WriteLine($"Conversation {conversationId} not found.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in AssignConversationToUser: {ex.Message}");
        }
    }

    public async Task SendMessageToCustomer(Message message)
    {
        try
        {
            if (message == null)
            {
                Console.WriteLine("Message is null. Aborting.");
                return;
            }

            var conversation = await _context.Conversations.FirstOrDefaultAsync(c => c.Id == message.ConversationId);
            if (conversation == null)
            {
                Console.WriteLine($"Conversation {message.ConversationId} not found.");
                return;
            }

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            await SendMessageToProvider(conversation, message);

            Console.WriteLine($"Message sent to customer {conversation.SenderId}: {message.Content}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in SendMessageToCustomer: {ex.Message}");
        }
    }

    public async Task CloseAndAnonymizeChat(string customerId)
    {
        try
        {
            var archivingService = Context.GetHttpContext()?.RequestServices.GetService<ChatArchivingService>();
            if (archivingService != null)
            {
                await archivingService.ArchiveConversationAsync(customerId);
                Console.WriteLine($"Chat with customer {customerId} archived and closed.");
            }
            else
            {
                Console.WriteLine("Archiving service not found.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in CloseAndAnonymizeChat: {ex.Message}");
        }
    }

    private async Task AddUserToGroups(string userId, string companyId, List<Team> teams)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Company_{companyId}");
        OnlineUsers[Context.ConnectionId] = new UserConnectionInfo
        {
            UserId = userId,
            UserName = Context.User?.Identity?.Name ?? "Unknown User",
            UserTeams = teams,
            CompanyId = companyId
        };

        Console.WriteLine($"Added user {userId} to group Company_{companyId}");
    }

    private async Task LoadUserConversations(string userId, string companyId)
    {
        var assignedConversations = await _context.Conversations
            .Include(c => c.Messages)
            .Where(c => c.AssignedUserId == userId && c.CompanyId == companyId)
            .ToListAsync();

        var newConversations = await _context.Conversations
            .Where(c => c.CompanyId == companyId && !c.IsAssigned)
            .ToListAsync();

        await Clients.Caller.SendAsync("LoadAssignedConversations", assignedConversations);
        await Clients.Caller.SendAsync("LoadNewConversations", newConversations);

        Console.WriteLine("Loaded user conversations.");
    }

    private async Task NotifyConversationAssignment(Conversation conversation, string userId)
    {
        conversation.Messages = conversation.Messages.OrderBy(m => m.SentAt).ToList();
        await Clients.User(userId).SendAsync("AssignConversation", conversation);
        await Clients.Group($"Company_{conversation.CompanyId}").SendAsync("RemoveNewConversation", conversation);
    }

    private async Task SendMessageToProvider(Conversation conversation, MessageFlow.Models.Message message)
    {
        switch (conversation.Source)
        {
            case "Facebook":
                var facebookService = Context.GetHttpContext()?.RequestServices.GetService<FacebookService>();
                if (facebookService != null)
                {
                    await facebookService.SendMessageToFacebookAsync(conversation.SenderId, message.Content, conversation.CompanyId, message.Id);
                }
                break;

            case "WhatsApp":
                var whatsappService = Context.GetHttpContext()?.RequestServices.GetService<WhatsAppService>();
                if (whatsappService != null)
                {
                    await whatsappService.SendMessageToWhatsAppAsync(conversation.SenderId, message.Content, conversation.CompanyId, message.Id);
                }
                break;

            default:
                Console.WriteLine($"Unknown source {conversation.Source} for conversation {conversation.Id}");
                break;
        }
    }

    private async Task BroadcastTeamMembers(string companyId)
    {
        var teamMembers = OnlineUsers.Values
            .Where(user => user.CompanyId == companyId)
            .Select(user => new TeamMembers.TeamMember
            {
                Name = user.UserName,
                Team = string.Join(", ", user.UserTeams.Select(t => t.TeamName)),
                Status = "Online"
            });

        foreach (var member in teamMembers)
        {
            await Clients.Group($"Company_{companyId}").SendAsync("AddTeamMember", member);
        }
    }

    private string GetClaimValue(string claimType)
    {
        return Context.User?.Claims.FirstOrDefault(c => c.Type == claimType)?.Value ?? string.Empty;
    }

    private string GetQueryValue(string key)
    {
        return Context.GetHttpContext()?.Request.Query[key].ToString() ?? string.Empty;
    }

    private List<Team> ParseTeams(string teamsJson)
    {
        var teams = new List<Team>();

        if (!string.IsNullOrWhiteSpace(teamsJson))
        {
            var teamEntries = teamsJson.Split(';', StringSplitOptions.RemoveEmptyEntries);

            foreach (var entry in teamEntries)
            {
                var parts = entry.Split(':', 2);
                if (parts.Length == 2 && int.TryParse(parts[0], out var teamId))
                {
                    teams.Add(new Team { TeamId = teamId, TeamName = parts[1].Trim() });
                }
            }
        }

        return teams;
    }

    private bool IsAuthorized(ClaimsPrincipal? user)
    {
        return user?.IsInRole("Agent") == true || user?.IsInRole("Manager") == true ||
               user?.IsInRole("Admin") == true || user?.IsInRole("SuperAdmin") == true;
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
