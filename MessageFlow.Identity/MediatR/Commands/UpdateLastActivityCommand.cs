using MediatR;

namespace MessageFlow.Identity.MediatR.Commands
{
    public record UpdateLastActivityCommand(string UserId) : IRequest<bool>;
}