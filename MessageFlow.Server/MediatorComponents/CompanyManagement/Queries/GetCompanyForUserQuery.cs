using MediatR;
using MessageFlow.Shared.DTOs;

namespace MessageFlow.Server.MediatorComponents.CompanyManagement.Queries
{
    public record GetCompanyForUserQuery : IRequest<CompanyDTO?>;
}
