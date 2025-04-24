using MediatR;
using MessageFlow.Shared.DTOs;

namespace MessageFlow.Server.MediatR.CompanyManagement.Queries
{
    public record GetCompanyForUserQuery : IRequest<CompanyDTO?>;
}