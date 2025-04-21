using System.Text.Json;
using MediatR;

namespace MessageFlow.Server.MediatR.Chat.GeneralProcessing.Commands
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