using MediatR;
using System.Text.Json;

namespace MessageFlow.Server.MediatR.Chat.FacebookProcessing.Commands
{
    public record ProcessFacebookWebhookEventCommand(JsonElement Entry) : IRequest<Unit>;
}