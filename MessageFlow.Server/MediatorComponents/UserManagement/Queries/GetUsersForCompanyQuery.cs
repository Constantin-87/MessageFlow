using MediatR;
using MessageFlow.Shared.DTOs;

namespace MessageFlow.Server.MediatorComponents.UserManagement.Queries
{
    public class GetUsersForCompanyQuery : IRequest<List<ApplicationUserDTO>>
    {
        public string CompanyId { get; }

        public GetUsersForCompanyQuery(string companyId)
        {
            CompanyId = companyId;
        }
    }
}
