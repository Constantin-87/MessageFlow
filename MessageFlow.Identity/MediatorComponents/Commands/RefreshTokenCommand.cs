using MessageFlow.Infrastructure.Mediator.Interfaces;

namespace MessageFlow.Identity.MediatorComponents.Commands
{
    public record RefreshTokenCommand(string AccessToken, string RefreshToken)
        : IRequest<(bool Success, string NewAccessToken, string NewRefreshToken, string ErrorMessage)>;
}
