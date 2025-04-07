using MediatR;
using System.Text.Json;

namespace MessageFlow.Server.MediatorComponents.Chat.FacebookProcessing.Commands
{
    public record ProcessFacebookWebhookEventCommand(JsonElement Entry) : IRequest<Unit>;
}
