using MediatR;
using MessageFlow.Identity.Models;

namespace MessageFlow.Identity.MediatR.Commands
{
    public record LoginCommand(LoginRequest LoginData) : IRequest<LoginResultDTO>;
}