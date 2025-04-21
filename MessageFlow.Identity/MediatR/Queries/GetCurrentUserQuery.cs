using MediatR;
using MessageFlow.Shared.DTOs;

namespace MessageFlow.Identity.MediatR.Queries
{
    public record GetCurrentUserQuery(string UserId) : IRequest<ApplicationUserDTO?>;
}