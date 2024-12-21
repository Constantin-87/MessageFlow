using MessageFlow.Client.Components;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using MessageFlow.Data;
using MessageFlow.Components.Channels.Services;

public class ChatHub : Hub
{
    // Track online users with company and team info
    private static ConcurrentDictionary<string, UserConnectionInfo> OnlineUsers = new();

    private readonly ApplicationDbContext _context;

    public ChatHub(ApplicationDbContext context)
    {
        _context = context;
    }

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
        Console.WriteLine($"Connection established. ConnectionId: {Context.ConnectionId}");
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

    public async Task AssignConversationToUser(string conversationId)
    {
        var conversation = await _context.Conversations
            .Include(c => c.Messages) // Include the related messages
            .FirstOrDefaultAsync(c => c.Id == conversationId);

        if (conversation != null)
        {
            var userId = Context.UserIdentifier;
            conversation.AssignedUserId = userId;
            conversation.IsAssigned = true;
            await _context.SaveChangesAsync();

            // Get the latest message from the conversation
            var latestMessage = conversation.Messages
                .OrderByDescending(m => m.SentAt)
                .FirstOrDefault();

            var latestMessageContent = latestMessage?.Content ?? "No messages in this conversation.";

            // Notify the assigned user to open the chat with the latest message
            await Clients.User(userId).SendAsync("AssignConversation", conversation.SenderId, latestMessageContent);
            Console.WriteLine($"Assigned conversation {conversationId} to user {userId} with message: {latestMessageContent}");

            // Notify all users in the company to remove the conversation from their lists
            await Clients.Group($"Company_{conversation.CompanyId}").SendAsync("RemoveNewConversation", conversation);
            Console.WriteLine($"Removed new conversation for CompanyId: {conversation.CompanyId}");
        }
    }

    public async Task SendMessageToAssignedUser(string assignedUserId, string messageContent, string senderId)
    {
        if (!string.IsNullOrEmpty(assignedUserId))
        {
            Console.WriteLine($"Attempting to send message to assigned user: {assignedUserId}");

            var isUserOnline = OnlineUsers.Values.Any(u => u.UserId == assignedUserId);

            if (isUserOnline)
            {
                Console.WriteLine($"User {assignedUserId} is online. Sending message...");

                await Clients.User(assignedUserId).SendAsync("ReceiveMessage", messageContent, senderId);

                Console.WriteLine("Message sent to assigned user.");
            }
            else
            {
                Console.WriteLine($"User {assignedUserId} is not online. Mock log: Message queued for delivery.");
            }
        }
        else
        {
            Console.WriteLine("AssignedUserId is null or empty.");
        }
    }

    public async Task SendMessageToCustomer(string customerId, string messageText, string localMessageId)
    {
        var userId = Context.UserIdentifier;
        Console.WriteLine($"Attempting to send message from user {userId} to customer {customerId}: {messageText}");

        var companyId = OnlineUsers.Values.FirstOrDefault(u => u.UserId == userId)?.CompanyId;

        if (!string.IsNullOrEmpty(companyId))
        {
            Console.WriteLine($"Company ID found: {companyId}");

            // Fetch the conversation to determine the source (Facebook or WhatsApp)
            var conversation = await _context.Conversations
                .FirstOrDefaultAsync(c => c.SenderId == customerId && c.CompanyId == companyId);

            if (conversation != null)
            {
                if (conversation.Source == "Facebook")
                {
                    var facebookService = Context.GetHttpContext()?.RequestServices.GetService<FacebookService>();
                    if (facebookService != null)
                    {
                        await facebookService.SendMessageToFacebookAsync(customerId, messageText, companyId, localMessageId);
                    }
                    else
                    {
                        Console.WriteLine("FacebookService not found in the context.");
                        await Clients.User(userId).SendAsync("MessageFailed", "Failed to send message. Facebook service unavailable.");
                    }
                }
                else if (conversation.Source == "WhatsApp")
                {
                    var whatsAppService = Context.GetHttpContext()?.RequestServices.GetService<WhatsAppService>();
                    if (whatsAppService != null)
                    {
                        await whatsAppService.SendMessageToWhatsAppAsync(customerId, messageText, companyId, localMessageId);
                    }
                    else
                    {
                        Console.WriteLine("WhatsAppService not found in the context.");
                        await Clients.User(userId).SendAsync("MessageFailed", "Failed to send message. WhatsApp service unavailable.");
                    }
                }
                else if (conversation.Source == "GatewayApi")
                { 
                    // TO DO GatewayApi connection
                }
                else
                {
                    Console.WriteLine("Unknown conversation source.");
                    await Clients.User(userId).SendAsync("MessageFailed", "Failed to send message. Unknown conversation source.");
                }
            }
            else
            {
                Console.WriteLine($"No conversation found for customer {customerId}.");
                await Clients.User(userId).SendAsync("MessageFailed", "Failed to send message. Conversation not found.");
            }
        }
        else
        {
            Console.WriteLine("Company ID not found for user.");
            await Clients.User(userId).SendAsync("MessageFailed", "Failed to send message. Company ID not found.");
        }
    }


    public async Task NotifyMessageDelivered(string assignedUserId, string messageContent, string senderId)
    {
        if (!string.IsNullOrEmpty(assignedUserId))
        {
            Console.WriteLine($"Notifying user {assignedUserId} that message was delivered.");

            await Clients.User(assignedUserId).SendAsync("MessageDelivered", senderId, messageContent);
        }
    }
    public async Task CloseAndAnonymizeChat(string customerId)
    {
        var archivingService = Context.GetHttpContext()?.RequestServices.GetService<ChatArchivingService>();

        if (archivingService != null)
        {
            await archivingService.ArchiveConversationAsync(customerId);
            Console.WriteLine($"Chat with customer {customerId} has been archived and closed.");
        }
        else
        {
            Console.WriteLine("Archiving service not found.");
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
