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
            services.AddScoped<IRequestHandler<LoginCommand, (bool, string, string, string, ApplicationUserDTO?)>, LoginCommandHandler>();
            services.AddScoped<IRequestHandler<LogoutCommand, bool>, LogoutCommandHandler>();
            services.AddScoped<IRequestHandler<RefreshTokenCommand, (bool, string, string, string)>, RefreshTokenCommandHandler>();
            services.AddScoped<IRequestHandler<RevokeRefreshTokenCommand, bool>, RevokeRefreshTokenCommandHandler>();
            services.AddScoped<IRequestHandler<UpdateLastActivityCommand, bool>, UpdateLastActivityCommandHandler>();
            services.AddScoped<IRequestHandler<ValidateSessionQuery, (bool, ApplicationUser?)>, ValidateSessionQueryHandler>();
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