using MessageFlow.Infrastructure.Mediator.Interfaces;

namespace MessageFlow.Server.MediatorComponents.UserManagement.Commands
{
    public class DeleteUsersByCompanyCommand : IRequest<bool>
    {
        public string CompanyId { get; }

        public DeleteUsersByCompanyCommand(string companyId)
        {
            CompanyId = companyId;
        }
    }
}
