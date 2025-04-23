using MediatR;
using MessageFlow.Shared.DTOs;

namespace MessageFlow.Server.MediatR.Chat.FacebookProcessing.Commands
{
    public record SaveFacebookSettingsCommand(string CompanyId, FacebookSettingsDTO FacebookSettingsDto) : IRequest<(bool success, string errorMessage)>;
}
