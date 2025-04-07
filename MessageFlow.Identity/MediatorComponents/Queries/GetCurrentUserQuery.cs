using MediatR;
using MessageFlow.Shared.DTOs;

namespace MessageFlow.Identity.MediatorComponents.Queries
{
    public record GetCurrentUserQuery(string UserId) : IRequest<ApplicationUserDTO?>;
}
