using System.Security.Claims;
using MessageFlow.DataAccess.Models;
using MediatR;

namespace MessageFlow.Identity.MediatR.Queries
{
    public record ValidateSessionQuery(ClaimsPrincipal User) : IRequest<(bool Success, ApplicationUser? User)>;
}