using MediatR;
using System.Security.Claims;

namespace MessageFlow.Identity.MediatR.Commands
{
    public record LogoutCommand(ClaimsPrincipal User) : IRequest<bool>;
}
