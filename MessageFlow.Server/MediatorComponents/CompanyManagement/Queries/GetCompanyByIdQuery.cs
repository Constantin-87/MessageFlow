using MessageFlow.Shared.DTOs;
using MessageFlow.Infrastructure.Mediator.Interfaces;

namespace MessageFlow.Server.MediatorComponents.CompanyManagement.Queries
{
    public record GetCompanyByIdQuery(string CompanyId) : IRequest<CompanyDTO?>;
}
