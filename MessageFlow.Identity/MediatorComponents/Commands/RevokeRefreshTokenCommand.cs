using MediatR;

namespace MessageFlow.Identity.MediatorComponents.Commands
{
    public record RevokeRefreshTokenCommand(string UserId) : IRequest<bool>;
}
