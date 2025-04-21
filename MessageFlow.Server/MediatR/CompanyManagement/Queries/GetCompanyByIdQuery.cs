using MessageFlow.Shared.DTOs;
using MediatR;

namespace MessageFlow.Server.MediatR.CompanyManagement.Queries
{
    public record GetCompanyByIdQuery(string CompanyId) : IRequest<CompanyDTO?>;
}
