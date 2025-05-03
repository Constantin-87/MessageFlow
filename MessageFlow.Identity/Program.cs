using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MessageFlow.DataAccess.Configurations;
using MessageFlow.DataAccess.Services;
using MessageFlow.DataAccess.Models;
using MessageFlow.Infrastructure.Mappings;
using MessageFlow.DataAccess.Implementations;
using MessageFlow.DataAccess.Repositories;
using Azure.Core;
using Azure.Identity;
using MessageFlow.Identity.Configuration;
using MessageFlow.Identity.MediatR.CommandHandlers;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
var environment = builder.Environment.EnvironmentName;
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// Load Azure Key Vault secrets
var keyVaultUrl = builder.Configuration["AzureKeyVaultURL"];
if (!string.IsNullOrEmpty(keyVaultUrl))
{
    TokenCredential credential = builder.Environment.IsDevelopment()
        ? new InteractiveBrowserCredential()
        : new DefaultAzureCredential();

    builder.Configuration.AddAzureKeyVault(new Uri(keyVaultUrl), credential);
}

var aiConnectionString = builder.Configuration["AppInsights--ConnectionString"];
if (!string.IsNullOrEmpty(aiConnectionString))
{
    builder.Services.AddApplicationInsightsTelemetry(options =>
    {
        options.ConnectionString = aiConnectionString;
    });
}

// Retrieve Connection String from Key Vault
var connectionString = builder.Configuration.GetConnectionString("DBConnectionString");
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Database connection string is missing from Azure Key Vault.");
}

// Register DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Register Identity with UoW's DbContext
builder.Services.AddScoped<IApplicationUserRepository>(sp =>
    new ApplicationUserRepository(
        sp.GetRequiredService<IUnitOfWork>().Context
    ));
builder.Services.Configure<IdentityOptions>(options =>
{
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
});
// Register Unit of Work
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Register AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile));

builder.Services.AddIdentityServices();

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(LoginCommandHandler).Assembly);
});

builder.Services.AddMediatorHandlers();

builder.Services.AddJwtAuthentication(builder.Configuration);

builder.Services.AddCorsPolicy(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.File(
        "logs/identity-log-.txt",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] {Message:lj}{NewLine}{Exception}"
    )
    .CreateLogger();

builder.Host.UseSerilog();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHsts();
app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("AllowBlazorWasm");
app.UseAuthentication();
app.UseAuthorization();
app.UseEndpoints(endpoints =>
{
    _ = endpoints.MapControllers();
});

app.Run();