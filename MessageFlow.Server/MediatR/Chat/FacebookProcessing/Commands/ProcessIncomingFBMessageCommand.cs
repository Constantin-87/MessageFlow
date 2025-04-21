using MediatR;
using System.Text.Json;

namespace MessageFlow.Server.MediatR.Chat.FacebookProcessing.Commands
{
    public record ProcessIncomingFBMessageCommand(string PageId, JsonElement EventData) : IRequest<Unit>;
}