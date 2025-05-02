using MessageFlow.AzureServices.Interfaces;
using MessageFlow.AzureServices.Services;
using MediatR;
using MessageFlow.Server.Authorization;
using MessageFlow.Server.MediatR.CompanyManagement.CommandHandlers;
using MessageFlow.Server.MediatR.CompanyManagement.Commands;
using MessageFlow.Server.MediatR.CompanyManagement.Queries;
using MessageFlow.Server.MediatR.CompanyManagement.QueryHandlers;
using MessageFlow.Server.MediatR.TeamManagement.CommandHandlers;
using MessageFlow.Server.MediatR.TeamManagement.Commands;
using MessageFlow.Server.MediatR.TeamManagement.Queries;
using MessageFlow.Server.MediatR.TeamManagement.QueryHandlers;
using MessageFlow.Server.MediatR.UserManagement.CommandHandlers;
using MessageFlow.Server.MediatR.UserManagement.Commands;
using MessageFlow.Server.MediatR.UserManagement.Queries;
using MessageFlow.Server.MediatR.UserManagement.QueryHandlers;
using MessageFlow.Shared.DTOs;
using MessageFlow.Server.MediatR.Chat.GeneralProcessing.CommandHandlers;
using MessageFlow.Server.MediatR.Chat.GeneralProcessing.Commands;
using MessageFlow.Server.MediatR.Chat.FacebookProcessing.CommandHandlers;
using MessageFlow.Server.MediatR.Chat.FacebookProcessing.Commands;
using MessageFlow.Server.MediatR.Chat.FacebookProcessing.Queries;
using MessageFlow.Server.MediatR.Chat.FacebookProcessing.QueryHandlers;
using MessageFlow.Server.MediatR.Chat.WhatsappProcessing.CommandHandlers;
using MessageFlow.Server.MediatR.Chat.WhatsappProcessing.Commands;
using MessageFlow.Server.MediatR.Chat.WhatsappProcessing.Queries;
using MessageFlow.Server.MediatR.Chat.WhatsappProcessing.QueryHandlers;
using MessageFlow.AzureServices.Helpers.Interfaces;
using MessageFlow.AzureServices.Helpers;
using MessageFlow.Server.Helpers.Interfaces;
using MessageFlow.Server.Helpers;
using System.Text.Json;

namespace MessageFlow.Server.Configuration
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<IDocumentProcessingService, DocumentProcessingService>();
            services.AddScoped<IAzureSearchQueryService, AzureSearchQueryService>();
            services.AddScoped<IAzureBlobStorageService, AzureBlobStorageService>();
            services.AddScoped<IAuthorizationHelper, AuthorizationHelper>();
            services.AddScoped<ICompanyDataHelper, CompanyDataHelper>();
            services.AddScoped<IMessageSenderHelper, MessageSenderHelper>();

            return services;
        }

        public static IServiceCollection AddMediatorHandlers(this IServiceCollection services)
        {
            // TeamManagement Queries
            services.AddScoped<IRequestHandler<GetTeamsForCompanyQuery, List<TeamDTO>>, GetTeamsForCompanyHandler>();
            services.AddScoped<IRequestHandler<GetUsersForTeamQuery, List<ApplicationUserDTO>>, GetUsersForTeamHandler>();

            // TeamManagement Commands
            services.AddScoped<IRequestHandler<AddTeamToCompanyCommand, (bool, string)>, AddTeamToCompanyHandler>();
            services.AddScoped<IRequestHandler<UpdateTeamCommand, (bool, string)>, UpdateTeamHandler>();
            services.AddScoped<IRequestHandler<DeleteTeamByIdCommand, (bool, string)>, DeleteTeamByIdHandler>();
            services.AddScoped<IRequestHandler<DeleteTeamsByCompanyCommand, bool>, DeleteTeamsByCompanyHandler>();

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

            // Chat.GeneralProcessing Commands
            services.AddScoped<IRequestHandler<ProcessMessageCommand, Unit>, ProcessMessageHandler>();
            services.AddScoped<IRequestHandler<ProcessMessageStatusUpdateCommand, bool>, ProcessMessageStatusUpdateHandler>();
            services.AddScoped<IRequestHandler<AddMessageToConversationCommand, Unit>, AddMessageToConversationHandler>();
            services.AddScoped<IRequestHandler<CreateAndAssignToAICommand, Unit>, CreateAndAssignToAIHandler>();
            services.AddScoped<IRequestHandler<HandleAIConversationCommand, Unit>, HandleAIConversationHandler>();
            services.AddScoped<IRequestHandler<EscalateCompanyTeamCommand, Unit>, EscalateCompanyTeamHandler>();
            services.AddScoped<IRequestHandler<SendAIResponseCommand, Unit>, SendAIResponseHandler>();
            services.AddScoped<IRequestHandler<AddUserToGroupsCommand, Unit>, AddUserToGroupsHandler>();
            services.AddScoped<IRequestHandler<LoadUserConversationsCommand, Unit>, LoadUserConversationsHandler>();
            services.AddScoped<IRequestHandler<BroadcastTeamMembersCommand, Unit>, BroadcastTeamMembersHandler>();
            services.AddScoped<IRequestHandler<BroadcastUserDisconnectedCommand, Unit>, BroadcastUserDisconnectedHandler>();
            services.AddScoped<IRequestHandler<AssignConversationToUserCommand, (bool, string)>, AssignConversationToUserHandler>();
            services.AddScoped<IRequestHandler<SendMessageToCustomerCommand, (bool, string)>, SendMessageToCustomerHandler>();
            services.AddScoped<IRequestHandler<ArchiveConversationCommand, (bool, string)>, ArchiveConversationHandler>();

            // Chat.FacebookProcessing Commands
            services.AddScoped<IRequestHandler<SendMessageToFacebookCommand, bool>, SendMessageToFacebookHandler>();
            services.AddScoped<IRequestHandler<SaveFacebookSettingsCommand, (bool, string)>, SaveFacebookSettingsHandler>();
            services.AddScoped<IRequestHandler<ProcessFacebookWebhookEventCommand, Unit>, ProcessFacebookWebhookEventHandler>();
            services.AddScoped<IRequestHandler<HandleFacebookReadEventCommand, Unit>, HandleFacebookReadEventHandler>();

            // Chat.FacebookProcessing Queries
            services.AddScoped<IRequestHandler<GetFacebookSettingsQuery, FacebookSettingsDTO?>, GetFacebookSettingsHandler>();

            // Chat.WhatsAppProcessing Commands
            services.AddScoped<IRequestHandler<SaveWhatsAppCoreSettingsCommand, (bool, string)>, SaveWhatsAppCoreSettingsHandler>();
            services.AddScoped<IRequestHandler<SaveWhatsAppPhoneNumbersCommand, (bool, string)>, SaveWhatsAppPhoneNumbersHandler>();

            services.AddScoped<IRequestHandler<ProcessIncomingWAMessageCommand, Unit>, ProcessIncomingWAMessageHandler>();
            services.AddScoped<IRequestHandler<SendMessageToWhatsAppCommand, Unit>, SendMessageToWhatsAppHandler>();

            // Chat.WhatsAppProcessing Queries
            services.AddScoped<IRequestHandler<GetWhatsAppSettingsQuery, WhatsAppSettingsDTO?>, GetWhatsAppSettingsHandler>();

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