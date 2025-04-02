using MessageFlow.Infrastructure.Mediator.Interfaces;
using System.Security.Claims;

namespace MessageFlow.Identity.MediatorComponents.Commands
{
    public record LogoutCommand(ClaimsPrincipal User) : IRequest<bool>;
}
