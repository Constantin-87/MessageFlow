using Azure.Core;
using Azure.Identity;
using MessageFlow.AzureServices.Interfaces;
using MessageFlow.AzureServices.Services;
using MessageFlow.Infrastructure.Mediator.Interfaces;
using MessageFlow.Server.Authorization;
using MessageFlow.Server.Chat.Services;
using MessageFlow.Server.MediatorComponents.Chat.CommandHandlers;
using MessageFlow.Server.MediatorComponents.Chat.Commands;
using MessageFlow.Server.MediatorComponents.CompanyManagement.CommandHandlers;
using MessageFlow.Server.MediatorComponents.CompanyManagement.Commands;
using MessageFlow.Server.MediatorComponents.CompanyManagement.Queries;
using MessageFlow.Server.MediatorComponents.CompanyManagement.QueryHandlers;
using MessageFlow.Server.MediatorComponents.TeamManagement.CommandHandlers;
using MessageFlow.Server.MediatorComponents.TeamManagement.Commands;
using MessageFlow.Server.MediatorComponents.TeamManagement.Queries;
using MessageFlow.Server.MediatorComponents.TeamManagement.QueryHandlers;
using MessageFlow.Server.MediatorComponents.UserManagement.CommandHandlers;
using MessageFlow.Server.MediatorComponents.UserManagement.Commands;
using MessageFlow.Server.MediatorComponents.UserManagement.Queries;
using MessageFlow.Server.MediatorComponents.UserManagement.QueryHandlers;
using MessageFlow.Server.Services;
using MessageFlow.Server.Services.Interfaces;
using MessageFlow.Shared.DTOs;

namespace MessageFlow.Server.Configuration
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            //services.AddScoped<CompanyManagementService>();
            services.AddScoped<ChatArchivingService>();
            services.AddScoped<IMessageProcessingService, MessageProcessingService>();
            services.AddScoped<IFacebookService, FacebookService>();
            services.AddScoped<IWhatsAppService, WhatsAppService>();
            services.AddScoped<IDocumentProcessingService, DocumentProcessingService>();
            services.AddScoped<AIChatBotService>();
            services.AddScoped<AzureSearchQueryService>();
            services.AddScoped<IAzureBlobStorageService, AzureBlobStorageService>();
            services.AddScoped<IAuthorizationHelper, AuthorizationHelper>();

            return services;
        }


        public static IServiceCollection AddMediatorHandlers(this IServiceCollection services)
        {
            services.AddScoped<IMediator, Infrastructure.Mediator.Mediator>();

            // TeamManagement Queries
            services.AddScoped<IRequestHandler<GetTeamsForCompanyQuery, List<TeamDTO>>, GetTeamsForCompanyHandler>();
            services.AddScoped<IRequestHandler<GetUsersForTeamQuery, List<ApplicationUserDTO>>, GetUsersForTeamHandler>();

            // TeamManagement Commands
            services.AddScoped<IRequestHandler<AddTeamToCompanyCommand, (bool, string)>, AddTeamToCompanyHandler>();
            services.AddScoped<IRequestHandler<UpdateTeamCommand, (bool, string)>, UpdateTeamHandler>();
            services.AddScoped<IRequestHandler<DeleteTeamByIdCommand, (bool, string)>, DeleteTeamByIdHandler>();
            services.AddScoped<IRequestHandler<DeleteTeamsByCompanyCommand, bool>, DeleteTeamsByCompanyHandler>();

            // Chat Commands
            services.AddScoped<IRequestHandler<SendFacebookMessageCommand, bool>, SendFacebookMessageHandler>();
            services.AddScoped<IRequestHandler<SendWhatsAppMessageCommand, bool>, SendWhatsAppMessageHandler>();
            services.AddScoped<IRequestHandler<ProcessMessageCommand, bool>, ProcessMessageHandler>();
            services.AddScoped<IRequestHandler<ProcessMessageStatusUpdateCommand, bool>, ProcessMessageStatusUpdateHandler>();

            // CompanyManagement Queries
            services.AddScoped<IRequestHandler<GetAllCompaniesQuery, List<CompanyDTO>>, GetAllCompaniesQueryHandler>();
            services.AddScoped<IRequestHandler<GetCompanyByIdQuery, CompanyDTO?>, GetCompanyByIdQueryHandler>();
            services.AddScoped<IRequestHandler<GetCompanyForUserQuery, CompanyDTO?>, GetCompanyForUserQueryHandler>();
            services.AddScoped<IRequestHandler<GetCompanyMetadataQuery, (bool, string, string)>, GetCompanyMetadataQueryHandler>();
            services.AddScoped<IRequestHandler<GetCompanyPretrainingFilesQuery, (bool, List<ProcessedPretrainDataDTO>, string)>, GetCompanyPretrainingFilesQueryHandler>();

            // CompanyManagement Commands
            services.AddScoped<IRequestHandler<CreateCompanyCommand, (bool, string)>, CreateCompanyCommandHandler>();
            services.AddScoped<IRequestHandler<UpdateCompanyDetailsCommand, (bool, string)>, UpdateCompanyDetailsCommandHandler>();
            services.AddScoped<IRequestHandler<UpdateCompanyEmailsCommand, (bool, string)>, UpdateCompanyEmailsCommandHandler>();
            services.AddScoped<IRequestHandler<UpdateCompanyPhoneNumbersCommand, (bool, string)>, UpdateCompanyPhoneNumbersCommandHandler>();
            services.AddScoped<IRequestHandler<DeleteCompanyCommand, (bool, string)>, DeleteCompanyCommandHandler>();
            services.AddScoped<IRequestHandler<GenerateCompanyMetadataCommand, (bool, string)>, GenerateCompanyMetadataCommandHandler>();
            services.AddScoped<IRequestHandler<DeleteCompanyMetadataCommand, (bool, string)>, DeleteCompanyMetadataCommandHandler>();
            services.AddScoped<IRequestHandler<UploadCompanyFilesCommand, (bool, string)>, UploadCompanyFilesCommandHandler>();
            services.AddScoped<IRequestHandler<DeleteCompanyFileCommand, bool>, DeleteCompanyFileCommandHandler>();
            services.AddScoped<IRequestHandler<CreateSearchIndexCommand, (bool, string)>, CreateSearchIndexCommandHandler>();

            // UserManagement Queries
            services.AddScoped<IRequestHandler<GetAllUsersQuery, List<ApplicationUserDTO>>, GetAllUsersHandler>();
            services.AddScoped<IRequestHandler<GetUserByIdQuery, ApplicationUserDTO?>, GetUserByIdHandler>();
            services.AddScoped<IRequestHandler<GetAvailableRolesQuery, List<string>>, GetAvailableRolesHandler>();
            services.AddScoped<IRequestHandler<GetUsersForCompanyQuery, List<ApplicationUserDTO>>, GetUsersForCompanyHandler>();
            services.AddScoped<IRequestHandler<GetUsersByIdsQuery, List<ApplicationUserDTO>>, GetUsersByIdsHandler>();

            // UserManagement Commands
            services.AddScoped<IRequestHandler<CreateUserCommand, (bool, string)>, CreateUserHandler>();
            services.AddScoped<IRequestHandler<UpdateUserCommand, (bool, string)>, UpdateUserHandler>();
            services.AddScoped<IRequestHandler<DeleteUserCommand, bool>, DeleteUserHandler>();
            services.AddScoped<IRequestHandler<DeleteUsersByCompanyCommand, bool>, DeleteUsersByCompanyHandler>();

            return services;
        }

        public static IServiceCollection AddCorsPolicy(this IServiceCollection services)
        {
            var allowedOrigins = new[] { "https://localhost:5003", "http://localhost:5004", "https://localhost:7043", "http://localhost:5027" };
            services.AddCors(options =>
            {
                options.AddPolicy("AllowBlazorWasm", builder =>
                {
                    builder.SetIsOriginAllowed(origin => new Uri(origin).Host == "localhost")
                           .WithOrigins(allowedOrigins)
                           .WithHeaders("Content-Type", "Authorization", "x-requested-with", "x-signalr-user-agent")
                           .WithMethods("GET", "POST", "PUT", "DELETE");
                });
            });

            return services;
        }

    }

}
