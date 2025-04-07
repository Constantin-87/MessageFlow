using MediatR;
using System.Text.Json;

namespace MessageFlow.Server.MediatorComponents.Chat.FacebookProcessing.Commands
{
    public record HandleFacebookReadEventCommand(JsonElement Read, string SenderId, string RecipientId) : IRequest<Unit>;
}