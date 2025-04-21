using MediatR;

namespace MessageFlow.Identity.MediatR.Commands
{
    public record RefreshTokenCommand(string AccessToken, string RefreshToken)
        : IRequest<(bool Success, string NewAccessToken, string NewRefreshToken, string ErrorMessage)>;
}
