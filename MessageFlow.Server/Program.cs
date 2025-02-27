using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MessageFlow.Server.Components.Accounts.Services;
using MessageFlow.DataAccess.Models;
using MessageFlow.Server.Middleware;
using MessageFlow.Server.Components;
using MessageFlow.Server.Components.Chat.Services;
using MessageFlow.Server.Configuration;
using Azure.Identity;
using Azure.Core;
using MessageFlow.AzureServices.Services;
using MessageFlow.Server.Mappings;
using MessageFlow.DataAccess.Implementations;
using MessageFlow.DataAccess.Repositories;
using MessageFlow.DataAccess.Configurations;
using MessageFlow.DataAccess.Services;
using MessageFlow.Shared.Interfaces;
using MessageFlow.Infrastructure.Mediator.Interfaces;
using MessageFlow.Infrastructure.Mediator;
using MessageFlow.Infrastructure.Mediator.Handlers.Chat;
using MessageFlow.Infrastructure.Mediator.Handlers;
using MessageFlow.Infrastructure.Mediator.Commands.Chat;

var builder = WebApplication.CreateBuilder(args);
var environment = builder.Environment.EnvironmentName;

builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();


var keyVaultUrl = builder.Configuration["AzureKeyVaultURL"];

if (!string.IsNullOrEmpty(keyVaultUrl))
{
    // Choose credential based on environment
    TokenCredential credential = builder.Environment.IsDevelopment()
        ? new InteractiveBrowserCredential() // For local development
        : new DefaultAzureCredential(); // For production

    builder.Configuration.AddAzureKeyVault(new Uri(keyVaultUrl), credential);
}

// ✅ Load secrets securely from Azure Key Vault
var globalSettings = new GlobalChannelSettings
{
    AppId = builder.Configuration["meta-app-id"],
    AppSecret = builder.Configuration["meta-app-secret"],
    FacebookWebhookVerifyToken = builder.Configuration["facebook-webhook-verify-token"],
    WhatsAppWebhookVerifyToken = builder.Configuration["whatsapp-webhook-verify-token"]
};

// ✅ Validate Secrets Before Starting
void ValidateConfig(GlobalChannelSettings settings)
{
    if (string.IsNullOrEmpty(settings.AppId) ||
        string.IsNullOrEmpty(settings.AppSecret) ||
        string.IsNullOrEmpty(settings.FacebookWebhookVerifyToken) ||
        string.IsNullOrEmpty(settings.WhatsAppWebhookVerifyToken))
    {
        throw new InvalidOperationException("Critical configuration values are missing. Application startup aborted.");
    }
}

ValidateConfig(globalSettings);

builder.Services.Configure<GlobalChannelSettings>(options =>
{
    options.AppId = globalSettings.AppId;
    options.AppSecret = globalSettings.AppSecret;
    options.FacebookWebhookVerifyToken = globalSettings.FacebookWebhookVerifyToken;
    options.WhatsAppWebhookVerifyToken = globalSettings.WhatsAppWebhookVerifyToken;
});

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddControllers();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, PersistingRevalidatingAuthenticationStateProvider>();
builder.Services.AddScoped<IUserManagementService, UserManagementService>();
builder.Services.AddScoped<CompanyManagementService>();
builder.Services.AddScoped<TeamsManagementService>();
builder.Services.AddScoped<ChatArchivingService>();
builder.Services.AddScoped<IMessageProcessingService, MessageProcessingService>();
builder.Services.AddScoped<IFacebookService, FacebookService>();
builder.Services.AddScoped<IWhatsAppService, WhatsAppService>();
builder.Services.AddScoped<DocumentProcessingService>();
builder.Services.AddScoped<AIChatBotService>();
builder.Services.AddScoped<AzureSearchQueryService>();
builder.Services.AddScoped<AzureBlobStorageService>();
builder.Services.AddAutoMapper(typeof(MappingProfile));

builder.Services.AddScoped<IMediator, Mediator>();
builder.Services.AddScoped<IRequestHandler<SendFacebookMessageCommand, bool>, SendFacebookMessageCommandHandler>();
builder.Services.AddScoped<IRequestHandler<SendWhatsAppMessageCommand, bool>, SendWhatsAppMessageCommandHandler>();
builder.Services.AddScoped<IRequestHandler<ProcessMessageCommand, bool>, ProcessMessageCommandHandler>();
builder.Services.AddScoped<IRequestHandler<ProcessMessageStatusUpdateCommand, bool>, ProcessMessageStatusUpdateCommandHandler>();



var searchServiceEndpoint = builder.Configuration["azure-ai-search-url"];
var searchServiceApiKey = builder.Configuration["azure-ai-search-key"];

if (string.IsNullOrEmpty(searchServiceEndpoint) || string.IsNullOrEmpty(searchServiceApiKey))
{
    throw new InvalidOperationException("Azure Search configuration is missing.");
}

builder.Services.AddScoped<AzureSearchService>(provider =>
{
    var blobStorageService = provider.GetRequiredService<AzureBlobStorageService>();
    return new AzureSearchService(searchServiceEndpoint, searchServiceApiKey, blobStorageService);
});


builder.Services.AddScoped<AzureSearchQueryService>();




builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<UpdateLastActivityFilter>();
});

// Email sender (no-op in this case)
builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();


builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddRoles<IdentityRole>()
    .AddApiEndpoints();


builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Accounts/Login"; // Redirect unauthorized users to this login page
    options.AccessDeniedPath = "/Accounts/AccessDenied"; // Handle access denied situations
});

builder.Services.AddAuthorization();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DBConnectionString");

    if (string.IsNullOrEmpty(connectionString))
    {
        Console.WriteLine("Error: No valid connection string found.");
        throw new InvalidOperationException("No valid connection string found.");
    }

    options.UseSqlServer(connectionString, b => b.MigrationsAssembly("MessageFlow.DataAccess"));
});

builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DBConnectionString");
    options.UseSqlServer(connectionString);
}, ServiceLifetime.Scoped);


builder.Services.AddScoped<ICompanyRepository, CompanyRepository>();
builder.Services.AddScoped<ICompanyEmailRepository, CompanyEmailRepository>();
builder.Services.AddScoped<ICompanyPhoneNumberRepository, CompanyPhoneNumberRepository>();
builder.Services.AddScoped<ITeamRepository, TeamRepository>();


builder.Services.AddScoped<IConversationRepository, ConversationRepository>();
builder.Services.AddScoped<IMessageRepository, MessageRepository>();
builder.Services.AddScoped<IArchivedConversationRepository, ArchivedConversationRepository>();
builder.Services.AddScoped<IArchivedMessageRepository, ArchivedMessageRepository>();

builder.Services.AddScoped<IFacebookSettingsRepository, FacebookSettingsRepository>();
builder.Services.AddScoped<IWhatsAppSettingsRepository, WhatsAppSettingsRepository>();
builder.Services.AddScoped<IPhoneNumberInfoRepository, PhoneNumberInfoRepository>();



builder.Services.AddScoped<IProcessedPretrainDataRepository, ProcessedPretrainDataRepository>();
builder.Services.AddScoped<IPretrainDataFileRepository, PretrainDataFileRepository>();

builder.Services.AddScoped<IApplicationUserRepository, ApplicationUserRepository>();

builder.Services.AddScoped<IDbContextFactoryService, DbContextFactoryService>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped(typeof(GenericRepository<>), typeof(GenericRepository<>));




builder.Services.AddAntiforgery();

builder.Logging.AddDebug();

builder.Services.Configure<IdentityOptions>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 6;
    options.Password.RequiredUniqueChars = 1;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.AllowedUserNameCharacters =
    "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    options.User.RequireUniqueEmail = true;
});

// Get the configured URLs from appsettings.json
var urls = builder.Configuration.GetSection("WebHostUrls").Get<string[]>();

if (urls != null && urls.Length > 0)
{
    builder.WebHost.UseUrls(urls);
}

builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
}).AddJsonProtocol(options =>
{
    options.PayloadSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve;
});

// Register HttpClient for server-side components
builder.Services.AddScoped<HttpClient>(sp =>
{
    var client = new HttpClient
    {
        BaseAddress = new Uri(builder.Configuration.GetValue<string>("ServerBaseAddress"))
    };
    return client;
});

var app = builder.Build();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<InactivityLogoutMiddleware>();

// Map SignalR hub
app.MapHub<ChatHub>("/chatHub");

app.MapIdentityApi<ApplicationUser>();

app.UseAntiforgery();

app.MapControllers();


// Map Razor Components (Blazor Server)
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(MessageFlow.Client._Imports).Assembly);

// Seed DataBase!!
//using (var scope = app.Services.CreateScope())
//{
//    var services = scope.ServiceProvider;
//    await DatabaseSeeder.SeedSuperAdminAsync(services);
//}

app.Run();
