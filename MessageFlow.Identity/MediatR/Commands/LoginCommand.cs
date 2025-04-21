using MediatR;
using MessageFlow.Shared.DTOs;

namespace MessageFlow.Identity.MediatR.Commands
{
    public record LoginCommand(string Username, string Password)
        : IRequest<(bool Success, string Token, string RefreshToken, string ErrorMessage, ApplicationUserDTO? User)>;
}