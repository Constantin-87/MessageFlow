//using MessageFlow.Client.Components;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using MessageFlow.DataAccess.Services;
using MessageFlow.DataAccess.Models;
using MessageFlow.Shared.DTOs;
using System.Security.Claims;
using AutoMapper;
using MessageFlow.Server.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using MessageFlow.Server.Chat.Services;

[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Agent,Manager,Admin,SuperAdmin")]
public class ChatHub : Hub
{
    // Track online users with company and team info
    private static ConcurrentDictionary<string, ApplicationUserDTO> OnlineUsers = new();

    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IFacebookService _facebookService;
    private readonly IWhatsAppService _whatsAppService;

    public ChatHub(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IFacebookService facebookService,
        IWhatsAppService whatsAppService)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _facebookService = facebookService ?? throw new ArgumentNullException(nameof(facebookService));
        _whatsAppService = whatsAppService ?? throw new ArgumentNullException(nameof(whatsAppService));
    }

    public override async Task OnConnectedAsync()
    {
        try
        {
            if (!IsAuthorized(Context.User))
            {
                Console.WriteLine("❌ Unauthorized connection attempt.");
                Context.Abort();
                return;
            }

            var userId = Context.UserIdentifier;

            if (string.IsNullOrEmpty(userId))
            {
                Console.WriteLine("❌ Missing userId. Aborting connection.");
                Context.Abort();
                return;
            }

            // ✅ Retrieve the ApplicationUser entity from repository
            var applicationUser = await _unitOfWork.ApplicationUsers.GetUserByIdAsync(userId);

            if (applicationUser == null || string.IsNullOrEmpty(applicationUser.CompanyId))
            {
                Console.WriteLine($"❌ User {userId} does not exist or does not have a valid CompanyId. Aborting connection.");
                Context.Abort();
                return;
            }

            // ✅ Map to DTO
            var applicationUserDto = _mapper.Map<ApplicationUserDTO>(applicationUser);

            if (applicationUserDto == null || string.IsNullOrEmpty(applicationUserDto.CompanyId))
            {
                Console.WriteLine($"❌ User {userId} does not have a valid CompanyId. Aborting connection.");
                Context.Abort();
                return;
            }

            await AddUserToGroups(applicationUserDto);
            await LoadUserConversations(applicationUserDto.Id, applicationUserDto.CompanyId);
            await BroadcastTeamMembers(applicationUserDto.CompanyId);
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

            if (OnlineUsers.TryRemove(Context.ConnectionId, out var userInfo) && userInfo != null)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Company_{userInfo.CompanyId}");
                foreach (var teamId in userInfo.TeamIds)
                {
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Team_{teamId}");
                }
            }

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
            var conversation = await _unitOfWork.Conversations.GetConversationByIdAsync(conversationId);

            if (conversation != null)
            {
                var userId = Context.UserIdentifier;
                conversation.AssignedUserId = userId;
                conversation.IsAssigned = true;

                _unitOfWork.Conversations.UpdateEntityAsync(conversation);
                await _unitOfWork.SaveChangesAsync();

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

    public async Task SendMessageToCustomer(MessageDTO messageDto)
    {
        try
        {
            if (messageDto == null)
            {
                Console.WriteLine("Message is null. Aborting.");
                return;
            }

            var conversation = await _unitOfWork.Conversations.GetConversationByIdAsync(messageDto.ConversationId);
            if (conversation == null)
            {
                Console.WriteLine($"Conversation {messageDto.ConversationId} not found.");
                return;
            }

            var message = _mapper.Map<Message>(messageDto);
            await _unitOfWork.Messages.AddEntityAsync(message);
            await _unitOfWork.SaveChangesAsync();

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

    private async Task AddUserToGroups(ApplicationUserDTO applicationUserDto)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Company_{applicationUserDto.CompanyId}");

        foreach (var teamId in applicationUserDto.TeamIds)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Team_{teamId}"); // ✅ Add to team-based group
        }

        OnlineUsers[Context.ConnectionId] = applicationUserDto;

        Console.WriteLine($"Added user {applicationUserDto.Id} to group Company_{applicationUserDto.CompanyId}");
    }

    private async Task LoadUserConversations(string userId, string companyId)
    {
        var assignedConversations = await _unitOfWork.Conversations.GetAssignedConversationsAsync(userId, companyId);
        var newConversations = await _unitOfWork.Conversations.GetUnassignedConversationsAsync(companyId);

        var assignedConversationsDto = _mapper.Map<List<ConversationDTO>>(assignedConversations);
        var newConversationsDto = _mapper.Map<List<ConversationDTO>>(newConversations);

        await Clients.Caller.SendAsync("LoadAssignedConversations", assignedConversationsDto);
        await Clients.Caller.SendAsync("LoadNewConversations", newConversationsDto);

        Console.WriteLine("Loaded user conversations.");
    }

    private async Task NotifyConversationAssignment(Conversation conversation, string userId)
    {
        conversation.Messages = conversation.Messages.OrderBy(m => m.SentAt).ToList();
        var conversationDto = _mapper.Map<ConversationDTO>(conversation);
        await Clients.User(userId).SendAsync("AssignConversation", conversationDto);
        await Clients.Group($"Company_{conversation.CompanyId}").SendAsync("RemoveNewConversation", conversationDto);
    }

    private async Task SendMessageToProvider(Conversation conversation, Message message)
    {
        switch (conversation.Source)
        {
            case "Facebook":
                await _facebookService.SendMessageToFacebookAsync(conversation.SenderId, message.Content, conversation.CompanyId, message.Id);               
                break;

            case "WhatsApp":
                await _whatsAppService.SendMessageToWhatsAppAsync(conversation.SenderId, message.Content, conversation.CompanyId, message.Id);
                break;

            default:
                Console.WriteLine($"Unknown source {conversation.Source} for conversation {conversation.Id}");
                break;
        }
    }

    private async Task BroadcastTeamMembers(string companyId)
    {

        //var teamMembers = OnlineUsers.Values
        //    .Where(user => user.CompanyId == companyId)
        //    .Select(user => new
        //    {
        //        Name = user.UserName,
        //        Status = "Online",  // Or whatever you need
        //        Team = string.Join(", ", user.TeamIds) // Adjust to match client expectations
        //    });

        var teamMembers = OnlineUsers.Values
            .Where(user => user.CompanyId == companyId);

        //var teamMembers = OnlineUsers.Values
        //.Where(user => user.CompanyId == companyId)
        //.Select(user => new ApplicationUserDTO
        //{
        //    Id = user.Id,
        //    UserName = user.UserName,
        //    CompanyId = user.CompanyId,
        //    Role = user.Role,
        //    LockoutEnabled = user.LockoutEnabled,
        //    TeamIds = user.TeamIds, // Keep Teams data
        //    LastActivity = DateTime.UtcNow
        //});

        foreach (var member in teamMembers)
        {
            await Clients.Group($"Company_{companyId}").SendAsync("AddTeamMember", member);
        }
    }

    //private string GetClaimValue(string claimType)
    //{
    //    return Context.User?.Claims.FirstOrDefault(c => c.Type == claimType)?.Value ?? string.Empty;
    //}

    private string GetQueryValue(string key)
    {
        return Context.GetHttpContext()?.Request.Query[key].ToString() ?? string.Empty;
    }

    //private List<Team> ParseTeams(string teamsJson)
    //{
    //    var teams = new List<Team>();

    //    if (!string.IsNullOrWhiteSpace(teamsJson))
    //    {
    //        var teamEntries = teamsJson.Split(';', StringSplitOptions.RemoveEmptyEntries);

    //        foreach (var entry in teamEntries)
    //        {
    //            var parts = entry.Split(':', 2);
    //            if (parts.Length == 2 && int.TryParse(parts[0], out var teamId))
    //            {
    //                teams.Add(new Team { TeamId = teamId, TeamName = parts[1].Trim() });
    //            }
    //        }
    //    }

    //    return teams;
    //}

    private bool IsAuthorized(ClaimsPrincipal? user)
    {
        return user?.IsInRole("Agent") == true || user?.IsInRole("Manager") == true ||
               user?.IsInRole("Admin") == true || user?.IsInRole("SuperAdmin") == true;
    }

    private async Task BroadcastUserDisconnected(string companyId)
    {
        if (OnlineUsers.TryGetValue(Context.ConnectionId, out var userInfo))
        {
            var disconnectedUser = new ApplicationUserDTO
            {
                Id = userInfo.Id,
                UserName = userInfo.UserName,
                CompanyId = userInfo.CompanyId,
                Role = userInfo.Role,
                LockoutEnabled = userInfo.LockoutEnabled,
                TeamIds = userInfo.TeamIds,
                LastActivity = DateTime.UtcNow
            };

            // Broadcast the team member removal to all clients in the same company
            await Clients.Group($"Company_{companyId}").SendAsync("RemoveTeamMember", disconnectedUser);
        }
    }

    //public class Team
    //{
    //    public int TeamId { get; set; }
    //    public string TeamName { get; set; } = string.Empty;
    //}
}
