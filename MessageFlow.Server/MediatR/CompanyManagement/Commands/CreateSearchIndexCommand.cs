using MediatR;

namespace MessageFlow.Server.MediatR.CompanyManagement.Commands
{
    public record CreateSearchIndexCommand(string CompanyId) : IRequest<(bool success, string errorMessage)>;
}
