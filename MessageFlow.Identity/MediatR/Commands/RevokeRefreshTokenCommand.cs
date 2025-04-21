using MediatR;

namespace MessageFlow.Identity.MediatR.Commands
{
    public record RevokeRefreshTokenCommand(string UserId) : IRequest<bool>;
}