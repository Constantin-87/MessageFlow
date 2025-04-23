using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using MessageFlow.DataAccess.Services;
using MessageFlow.Shared.DTOs;
using System.Security.Claims;
using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using MediatR;
using MessageFlow.Server.MediatR.Chat.GeneralProcessing.Commands;

[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Agent,Manager,Admin,SuperAdmin")]
public class ChatHub : Hub
{
    // Track online users with company and team info
    public static ConcurrentDictionary<string, ApplicationUserDTO> OnlineUsers = new();

    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IMediator _mediator;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IMediator mediator,
        ILogger<ChatHub> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _mediator = mediator;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        try
        {
            if (!IsAuthorized(Context.User))
            {
                _logger.LogWarning("Unauthorized connection attempt. Claims: {Claims}", string.Join(", ", Context.User?.Claims.Select(c => $"{c.Type}={c.Value}") ?? []));
                Context.Abort();
                return;
            }

            var userId = Context.UserIdentifier;

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Missing userId. Aborting connection.");
                Context.Abort();
                return;
            }

            // Retrieve the ApplicationUser entity from repository
            var applicationUser = await _unitOfWork.ApplicationUsers.GetUserByIdAsync(userId);

            if (applicationUser == null || string.IsNullOrEmpty(applicationUser.CompanyId))
            {
                _logger.LogWarning("User {UserId} not found or missing CompanyId. Aborting connection.", userId);
                Context.Abort();
                return;
            }

            // Map to DTO
            var applicationUserDto = _mapper.Map<ApplicationUserDTO>(applicationUser);

            if (applicationUserDto == null || string.IsNullOrEmpty(applicationUserDto.CompanyId))
            {
                _logger.LogWarning("Mapped DTO for user {UserId} is null or missing CompanyId. Aborting connection.", userId);
                Context.Abort();
                return;
            }

            await _mediator.Send(new AddUserToGroupsCommand(applicationUserDto, Context.ConnectionId));

            await _mediator.Send(new LoadUserConversationsCommand(applicationUserDto.Id, applicationUserDto.CompanyId, Clients.Caller));

            await _mediator.Send(new BroadcastTeamMembersCommand(applicationUserDto.CompanyId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in OnConnectedAsync.");
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

            if (OnlineUsers.TryRemove(Context.ConnectionId, out var userInfo) && userInfo?.TeamsDTO != null)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Company_{userInfo.CompanyId}");
                foreach (var team in userInfo.TeamsDTO)
                {
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Team_{team.Id}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in OnDisconnectedAsync.");
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task AssignConversationToUser(string conversationId)
    {
        var userId = Context.UserIdentifier;
        var result = await _mediator.Send(new AssignConversationToUserCommand(conversationId, userId));

        if (!result.Success)
            _logger.LogWarning("AssignConversationToUser failed: {Error}", result.ErrorMessage);
    }

    public async Task SendMessageToCustomer(MessageDTO messageDto)
    {
        var result = await _mediator.Send(new SendMessageToCustomerCommand(messageDto));

        if (!result.Success)
            _logger.LogWarning("SendMessageToCustomer failed: {Error}", result.ErrorMessage);
    }

    public async Task CloseAndAnonymizeChat(string customerId)
    {
        var result = await _mediator.Send(new ArchiveConversationCommand(customerId));

        if (!result.Success)
            _logger.LogWarning("CloseAndAnonymizeChat failed: {Error}", result.ErrorMessage);
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
