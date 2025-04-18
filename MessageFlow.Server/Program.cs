using MessageFlow.Server.Configuration;
using Azure.Identity;
using Azure.Core;
using MessageFlow.Infrastructure.Mappings;
using MessageFlow.Server.Middleware;
using MessageFlow.DataAccess.Configurations;
using MessageFlow.DataAccess.Models;
using Microsoft.AspNetCore.Identity;
using MessageFlow.Server.MediatorComponents.Chat.GeneralProcessing.CommandHandlers;

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

// Load secrets securely from Azure Key Vault
var globalSettings = new GlobalChannelSettings
{
    AppId = builder.Configuration["meta-app-id"],
    AppSecret = builder.Configuration["meta-app-secret"],
    FacebookWebhookVerifyToken = builder.Configuration["facebook-webhook-verify-token"],
    WhatsAppWebhookVerifyToken = builder.Configuration["whatsapp-webhook-verify-token"]
};

// Validate Secrets Before Starting
void ValidateConfig(GlobalChannelSettings settings)
{
    if (string.IsNullOrEmpty(settings.AppId) ||
        string.IsNullOrEmpty(settings.AppSecret) ||
        string.IsNullOrEmpty(settings.FacebookWebhookVerifyToken) ||
        string.IsNullOrEmpty(settings.WhatsAppWebhookVerifyToken))
    {
        throw new InvalidOperationException("Configuration values are missing. Application startup aborted.");
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

builder.Services.AddHttpContextAccessor();

builder.Services.AddJwtAuthentication(builder.Configuration);

builder.Services.AddControllers().AddJsonOptions(opts =>
{
    opts.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    opts.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
});

builder.Services.AddApplicationServices();

builder.Services.AddAutoMapper(typeof(MappingProfile));


// Register MediatR
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(ProcessMessageHandler).Assembly);
});

// Query + Command Handlers
builder.Services.AddMediatorHandlers();

builder.Services.AddCorsPolicy();

builder.Services.AddAzureServices(builder.Configuration);

builder.Services.AddRepositoriesAndDataAccess(builder.Configuration);

builder.Services.AddAuthorization();

builder.Services.AddIdentityCore<ApplicationUser>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequiredUniqueChars = 1;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Register HttpClient to call .Identity APIs
builder.Services.AddHttpClient("IdentityAPI", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["MessageFlow-Identity-Uri"]);
});

builder.Logging.AddDebug();

// Get the configured URLs from appsettings
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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseHsts();
app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();
app.UseCors("AllowBlazorWasm");
app.UseAuthentication();
app.UseMiddleware<UserActivityMiddleware>();
app.UseAuthorization();

// Map SignalR hub
app.MapHub<ChatHub>("/chatHub");

app.MapControllers();

// Seed DataBase!!
//using (var scope = app.Services.CreateScope())
//{
//    var services = scope.ServiceProvider;
//    await DatabaseSeeder.SeedSuperAdminAsync(services);
//}

app.Run();
