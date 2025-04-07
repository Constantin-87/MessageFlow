using MediatR;
using System.Text.Json;

namespace MessageFlow.Server.MediatorComponents.Chat.FacebookProcessing.Commands
{
    public record ProcessIncomingFBMessageCommand(string PageId, JsonElement EventData) : IRequest<Unit>;
}