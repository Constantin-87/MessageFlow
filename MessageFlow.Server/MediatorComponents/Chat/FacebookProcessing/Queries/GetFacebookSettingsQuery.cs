using MediatR;
using MessageFlow.Shared.DTOs;

namespace MessageFlow.Server.MediatorComponents.Chat.FacebookProcessing.Queries
{
    public record GetFacebookSettingsQuery(string CompanyId) : IRequest<FacebookSettingsDTO?>;
}
