using MediatR;
using MessageFlow.Server.MediatorComponents.Chat.GeneralProcessing.Commands;
using MessageFlow.Server.Services;

namespace MessageFlow.Server.MediatorComponents.Chat.GeneralProcessing.CommandHandlers;

public class CloseAndAnonymizeChatHandler : IRequestHandler<CloseAndAnonymizeChatCommand, (bool Success, string ErrorMessage)>
{
    private readonly ChatArchivingService _chatArchivingService;

    public CloseAndAnonymizeChatHandler(ChatArchivingService chatArchivingService)
    {
        _chatArchivingService = chatArchivingService;
    }

    public async Task<(bool Success, string ErrorMessage)> Handle(CloseAndAnonymizeChatCommand request, CancellationToken cancellationToken)
    {
        try
        {
            await _chatArchivingService.ArchiveConversationAsync(request.CustomerId);
            return (true, $"Chat with customer {request.CustomerId} archived and closed.");
        }
        catch (Exception ex)
        {
            return (false, $"Error archiving chat: {ex.Message}");
        }
    }
}
