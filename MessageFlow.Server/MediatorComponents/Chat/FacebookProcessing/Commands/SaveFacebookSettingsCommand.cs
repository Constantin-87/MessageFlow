using MediatR;
using MessageFlow.Server.DataTransferObjects.Client;

namespace MessageFlow.Server.MediatorComponents.Chat.FacebookProcessing.Commands
{
    public record SaveFacebookSettingsCommand(string CompanyId, FacebookSettingsDTO FacebookSettingsDto) : IRequest<(bool success, string errorMessage)>;
}
