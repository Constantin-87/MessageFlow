using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using MessageFlow.DataAccess.Services;
using MessageFlow.Shared.DTOs;
using System.Security.Claims;
using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using MediatR;
using MessageFlow.Server.MediatorComponents.Chat.GeneralProcessing.Commands;

[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Agent,Manager,Admin,SuperAdmin")]
public class ChatHub : Hub
{
    // Track online users with company and team info
    public static ConcurrentDictionary<string, ApplicationUserDTO> OnlineUsers = new();

    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IMediator _mediator;

    public ChatHub(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IMediator mediator
        )
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _mediator = mediator;
    }

    public override async Task OnConnectedAsync()
    {
        try
        {
            if (!IsAuthorized(Context.User))
            {
                Console.WriteLine("Unauthorized connection attempt.");
                Context.Abort();
                return;
            }

            var userId = Context.UserIdentifier;

            if (string.IsNullOrEmpty(userId))
            {
                Console.WriteLine("Missing userId. Aborting connection.");
                Context.Abort();
                return;
            }

            // Retrieve the ApplicationUser entity from repository
            var applicationUser = await _unitOfWork.ApplicationUsers.GetUserByIdAsync(userId);

            if (applicationUser == null || string.IsNullOrEmpty(applicationUser.CompanyId))
            {
                Console.WriteLine($"User {userId} does not exist or does not have a valid CompanyId. Aborting connection.");
                Context.Abort();
                return;
            }

            // Map to DTO
            var applicationUserDto = _mapper.Map<ApplicationUserDTO>(applicationUser);

            if (applicationUserDto == null || string.IsNullOrEmpty(applicationUserDto.CompanyId))
            {
                Console.WriteLine($"User {userId} does not have a valid CompanyId. Aborting connection.");
                Context.Abort();
                return;
            }

            //await AddUserToGroups(applicationUserDto);
            await _mediator.Send(new AddUserToGroupsCommand(applicationUserDto, Context.ConnectionId));

            //await LoadUserConversations(applicationUserDto.Id, applicationUserDto.CompanyId);
            await _mediator.Send(new LoadUserConversationsCommand(applicationUserDto.Id, applicationUserDto.CompanyId, Clients.Caller));

            //await BroadcastTeamMembers(applicationUserDto.CompanyId);
            await _mediator.Send(new BroadcastTeamMembersCommand(applicationUserDto.CompanyId));
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
                await _mediator.Send(new BroadcastUserDisconnectedCommand(companyId, Context.ConnectionId));
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
        var userId = Context.UserIdentifier;
        var result = await _mediator.Send(new AssignConversationToUserCommand(conversationId, userId));

        if (!result.Success)
            Console.WriteLine($"{result.ErrorMessage}");
    }

    public async Task SendMessageToCustomer(MessageDTO messageDto)
    {
        var result = await _mediator.Send(new SendMessageToCustomerCommand(messageDto));

        if (!result.Success)
            Console.WriteLine($"{result.ErrorMessage}");
    }
   
    public async Task CloseAndAnonymizeChat(string customerId)
    {
        var result = await _mediator.Send(new CloseAndAnonymizeChatCommand(customerId));

        if (!result.Success)
            Console.WriteLine(result.ErrorMessage);
        else
            Console.WriteLine(result.ErrorMessage); 
    }

    private string GetQueryValue(string key)
    {
        return Context.GetHttpContext()?.Request.Query[key].ToString() ?? string.Empty;
    }

    private bool IsAuthorized(ClaimsPrincipal? user)
    {
        return user?.IsInRole("Agent") == true || user?.IsInRole("Manager") == true ||
               user?.IsInRole("Admin") == true || user?.IsInRole("SuperAdmin") == true;
    }
}
