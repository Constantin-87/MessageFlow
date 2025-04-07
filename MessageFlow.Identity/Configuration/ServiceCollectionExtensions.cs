using Microsoft.Extensions.DependencyInjection;
using MessageFlow.Identity.Services;
using MessageFlow.Identity.Services.Interfaces;
using MessageFlow.Identity.MediatorComponents.Commands;
using MessageFlow.Identity.MediatorComponents.CommandHandlers;
using MessageFlow.Identity.MediatorComponents.Queries;
using MessageFlow.Identity.MediatorComponents.QueryHandlers;
using MessageFlow.Shared.DTOs;
using MessageFlow.DataAccess.Models;
using MediatR;

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

        public static IServiceCollection AddCorsPolicy(this IServiceCollection services)
        {
            var allowedOrigins = new[] { "https://localhost:5003", "http://localhost:5004", "https://localhost:7164", "http://localhost:5002" };

            services.AddCors(options =>
            {
                options.AddPolicy("AllowBlazorWasm", builder =>
                {
                    builder.SetIsOriginAllowed(origin => new Uri(origin).Host == "localhost")
                           .WithOrigins(allowedOrigins)
                           .WithHeaders("Content-Type", "Authorization")
                           .WithMethods("GET", "POST", "PUT", "DELETE");
                });
            });

            return services;
        }

    }
}
