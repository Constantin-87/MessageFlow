using MediatR;
using System.Text.Json;

namespace MessageFlow.Server.MediatR.Chat.WhatsappProcessing.Commands
{
    public record ProcessIncomingWAMessageCommand(string BusinessAccountId, JsonElement Changes) : IRequest<Unit>;
}
