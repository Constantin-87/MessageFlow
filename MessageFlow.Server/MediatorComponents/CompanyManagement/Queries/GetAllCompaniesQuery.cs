using MessageFlow.Infrastructure.Mediator.Interfaces;
using MessageFlow.Shared.DTOs;

namespace MessageFlow.Server.MediatorComponents.CompanyManagement.Queries
{
    public record GetAllCompaniesQuery : IRequest<List<CompanyDTO>>;
}
