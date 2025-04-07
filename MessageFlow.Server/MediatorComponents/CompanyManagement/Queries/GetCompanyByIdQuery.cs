using MessageFlow.Shared.DTOs;
using MediatR;

namespace MessageFlow.Server.MediatorComponents.CompanyManagement.Queries
{
    public record GetCompanyByIdQuery(string CompanyId) : IRequest<CompanyDTO?>;
}
