using System.Text.Json;
using MessageFlow.Infrastructure.Mediator.Interfaces;

namespace MessageFlow.Infrastructure.Mediator.Commands.Chat
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
