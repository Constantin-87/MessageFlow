using MediatR;
using System.Text.Json;

namespace MessageFlow.Server.MediatR.Chat.FacebookProcessing.Commands
{
    public record HandleFacebookReadEventCommand(JsonElement Read, string SenderId, string RecipientId) : IRequest<Unit>;
}