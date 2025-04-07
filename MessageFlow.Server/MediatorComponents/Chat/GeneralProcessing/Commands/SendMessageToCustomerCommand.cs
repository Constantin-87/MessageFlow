using MediatR;
using MessageFlow.Shared.DTOs;

namespace MessageFlow.Server.MediatorComponents.Chat.GeneralProcessing.Commands
{
    public record SendMessageToCustomerCommand(MessageDTO MessageDto) : IRequest<(bool Success, string ErrorMessage)>;
}
