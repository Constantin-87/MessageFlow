using Azure.Core;
using Azure.Identity;
using MessageFlow.DataAccess.Configurations;
using MessageFlow.DataAccess.Models;
using MessageFlow.Infrastructure.Mappings;
using MessageFlow.Server.MediatR.Chat.GeneralProcessing.CommandHandlers;
using MessageFlow.Server.Middleware;
using Microsoft.AspNetCore.Identity;
using Serilog;

namespace MessageFlow.Server.Configuration
{
    public static class WebApplicationBuilderExtensions
    {       

        public static void ConfigureApp(this WebApplicationBuilder builder)
        {
            ConfigureConfiguration(builder);
            ConfigureKeyVault(builder);
            var globalSettings = LoadAndValidateSettings(builder);
            ConfigureApplicationInsights(builder);
            ConfigureLogging(builder);
            ConfigureServices(builder, globalSettings);
        }

        private static void ConfigureConfiguration(WebApplicationBuilder builder)
        {
            builder.Configuration
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();
        }

        private static void ConfigureKeyVault(WebApplicationBuilder builder)
        {
            var keyVaultUrl = builder.Configuration["AzureKeyVaultURL"];
            if (string.IsNullOrEmpty(keyVaultUrl) || builder.Environment.IsEnvironment("Test"))
                return;

            TokenCredential credential = builder.Environment.IsDevelopment()
                ? new InteractiveBrowserCredential()
                : new DefaultAzureCredential();

            var keyVaultConfig = new ConfigurationBuilder()
                .AddAzureKeyVault(new Uri(keyVaultUrl), credential)
                .Build();

            builder.Configuration.AddAzureKeyVault(new Uri(keyVaultUrl), credential);
        }

        private static GlobalChannelSettings LoadAndValidateSettings(WebApplicationBuilder builder)
        {
            var settings = new GlobalChannelSettings
            {
                AppId = builder.Configuration["meta-app-id"],
                AppSecret = builder.Configuration["meta-app-secret"],
                FacebookWebhookVerifyToken = builder.Configuration["facebook-webhook-verify-token"],
                WhatsAppWebhookVerifyToken = builder.Configuration["whatsapp-webhook-verify-token"]
            };

            if (string.IsNullOrEmpty(settings.AppId) ||
                string.IsNullOrEmpty(settings.AppSecret) ||
                string.IsNullOrEmpty(settings.FacebookWebhookVerifyToken) ||
                string.IsNullOrEmpty(settings.WhatsAppWebhookVerifyToken))
            {
                throw new InvalidOperationException("Configuration values are missing. Application startup aborted.");
            }

            return settings;
        }

        private static void ConfigureApplicationInsights(WebApplicationBuilder builder)
        {
            var connStr = builder.Configuration["AppInsights:ConnectionString"];
            if (!builder.Environment.IsEnvironment("Test") && !string.IsNullOrEmpty(connStr))
            {
                builder.Services.AddApplicationInsightsTelemetry(options => options.ConnectionString = connStr);
            }
        }

        private static void ConfigureLogging(WebApplicationBuilder builder)
        {
            var loggerConfig = new LoggerConfiguration()
                .ReadFrom.Configuration(builder.Configuration)
                .WriteTo.File(
                    "logs/server-log-.txt",
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] {Message:lj}{NewLine}{Exception}"
                );

            var aiConnStr = builder.Configuration["AppInsights:ConnectionString"];
            if (!string.IsNullOrEmpty(aiConnStr))
            {
                loggerConfig.WriteTo.ApplicationInsights(aiConnStr, TelemetryConverter.Traces);
            }

            Log.Logger = loggerConfig.CreateLogger();
            builder.Host.UseSerilog();
        }

        private static void ConfigureServices(WebApplicationBuilder builder, GlobalChannelSettings settings)
        {
            var services = builder.Services;

            services.Configure<GlobalChannelSettings>(opts =>
            {
                opts.AppId = settings.AppId;
                opts.AppSecret = settings.AppSecret;
                opts.FacebookWebhookVerifyToken = settings.FacebookWebhookVerifyToken;
                opts.WhatsAppWebhookVerifyToken = settings.WhatsAppWebhookVerifyToken;
            });

            services.AddHttpContextAccessor();
            services.AddJwtAuthentication(builder.Configuration);

            services.AddControllers().AddJsonOptions(opts =>
            {
                opts.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
                opts.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
            });

            services.AddApplicationServices();
            services.AddAutoMapper(typeof(MappingProfile));

            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ProcessMessageHandler).Assembly));
            services.AddMediatorHandlers();

            services.AddCorsPolicy(builder.Configuration);
            services.AddAzureServices(builder.Configuration);
            services.AddRepositoriesAndDataAccess(builder.Configuration);

            services.AddAuthorization();

            services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequiredUniqueChars = 1;

                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

            services.AddHttpClient("IdentityAPI", client =>
            {
                client.BaseAddress = new Uri(builder.Configuration["MessageFlow-Identity-Uri"]);
            });

            services.AddSignalR(opts => opts.EnableDetailedErrors = true)
                .AddJsonProtocol(opts =>
                {
                    opts.PayloadSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve;
                });
        }

        public static void ConfigurePipelineAsync(this WebApplication app)
        {
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
        }
    }
}