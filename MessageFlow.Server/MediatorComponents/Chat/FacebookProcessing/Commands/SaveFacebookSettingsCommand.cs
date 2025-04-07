using MediatR;
using MessageFlow.Shared.DTOs;

namespace MessageFlow.Server.MediatorComponents.Chat.FacebookProcessing.Commands
{
    public record SaveFacebookSettingsCommand(string CompanyId, FacebookSettingsDTO FacebookSettingsDto) : IRequest<bool>;
}
