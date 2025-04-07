using MediatR;

namespace MessageFlow.Identity.MediatorComponents.Commands
{
    public record UpdateLastActivityCommand(string UserId) : IRequest<bool>;
}
