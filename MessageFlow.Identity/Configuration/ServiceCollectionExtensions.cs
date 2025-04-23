using MessageFlow.Identity.Services;
using MessageFlow.Identity.Services.Interfaces;
using MessageFlow.Identity.MediatR.Commands;
using MessageFlow.Identity.MediatR.CommandHandlers;
using MessageFlow.Identity.MediatR.Queries;
using MessageFlow.Identity.MediatR.QueryHandlers;
using MessageFlow.Shared.DTOs;
using MessageFlow.DataAccess.Models;
using MediatR;
using System.Text.Json;

namespace MessageFlow.Identity.Configuration
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddIdentityServices(this IServiceCollection services)
        {
            services.AddScoped<ITokenService, TokenService>();
            return services;
        }

        public static IServiceCollection AddMediatorHandlers(this IServiceCollection services)
        {
            // Login
            services.AddScoped<IRequestHandler<LoginCommand, (bool, string, string, string, ApplicationUserDTO?)>, LoginCommandHandler>();

            // Logout
            services.AddScoped<IRequestHandler<LogoutCommand, bool>, LogoutCommandHandler>();

            // Refresh Token
            services.AddScoped<IRequestHandler<RefreshTokenCommand, (bool, string, string, string)>, RefreshTokenCommandHandler>();

            // Revoke Refresh Token
            services.AddScoped<IRequestHandler<RevokeRefreshTokenCommand, bool>, RevokeRefreshTokenCommandHandler>();

            // Update Last Activity
            services.AddScoped<IRequestHandler<UpdateLastActivityCommand, bool>, UpdateLastActivityCommandHandler>();

            // Validate Session
            services.AddScoped<IRequestHandler<ValidateSessionQuery, (bool, ApplicationUser?)>, ValidateSessionQueryHandler>();

            // Get Current User
            services.AddScoped<IRequestHandler<GetCurrentUserQuery, ApplicationUserDTO?>, GetCurrentUserQueryHandler>();

            return services;
        }

        public static IServiceCollection AddCorsPolicy(this IServiceCollection services, IConfiguration config)
        {
            var json = config["Cors-AllowedOrigins"];
            var allowedOrigins = JsonSerializer.Deserialize<string[]>(json ?? "[]");

            services.AddCors(options =>
            {
                options.AddPolicy("AllowBlazorWasm", builder =>
                {
                    builder.SetIsOriginAllowed(origin =>
                            allowedOrigins.Any(allowed => origin.Contains(allowed)))
                           .WithHeaders("Content-Type", "Authorization", "x-requested-with", "x-signalr-user-agent")
                           .WithMethods("GET", "POST", "PUT", "DELETE");
                });
            });

            return services;
        }
    }
}
