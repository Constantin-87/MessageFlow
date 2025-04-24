using MediatR;
using MessageFlow.Shared.DTOs;

namespace MessageFlow.Server.MediatR.CompanyManagement.Queries
{
    public record GetAllCompaniesQuery : IRequest<List<CompanyDTO>>;
}