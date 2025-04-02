using System.Security.Claims;
using MessageFlow.DataAccess.Models;
using MessageFlow.Infrastructure.Mediator.Interfaces;

namespace MessageFlow.Identity.MediatorComponents.Queries
{
    public record ValidateSessionQuery(ClaimsPrincipal User) : IRequest<(bool Success, ApplicationUser? User)>;
}
