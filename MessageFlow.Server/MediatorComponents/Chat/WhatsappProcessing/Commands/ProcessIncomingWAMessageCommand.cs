using MediatR;
using System.Text.Json;

namespace MessageFlow.Server.MediatorComponents.Chat.WhatsappProcessing.Commands
{
    public record ProcessIncomingWAMessageCommand(string BusinessAccountId, JsonElement Changes) : IRequest<Unit>;
}
