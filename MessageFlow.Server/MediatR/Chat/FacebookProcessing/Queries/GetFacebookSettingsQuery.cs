using MediatR;
using MessageFlow.Shared.DTOs;

namespace MessageFlow.Server.MediatR.Chat.FacebookProcessing.Queries
{
    public record GetFacebookSettingsQuery(string CompanyId) : IRequest<FacebookSettingsDTO?>;
}