using System.Text.Json;
using MessageFlow.Infrastructure.Mediator.Interfaces;

namespace MessageFlow.Server.MediatorComponents.Chat.Commands
{
    public class ProcessMessageStatusUpdateCommand : IRequest<bool>
    {
        public JsonElement StatusElement { get; }
        public string Platform { get; }

        public ProcessMessageStatusUpdateCommand(JsonElement statusElement, string platform)
        {
            StatusElement = statusElement;
            Platform = platform;
        }
    }
}
