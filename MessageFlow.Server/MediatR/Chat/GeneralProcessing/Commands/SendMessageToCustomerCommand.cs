using MediatR;
using MessageFlow.Shared.DTOs;

namespace MessageFlow.Server.MediatR.Chat.GeneralProcessing.Commands
{
    public record SendMessageToCustomerCommand(MessageDTO MessageDto) : IRequest<(bool Success, string ErrorMessage)>;
}