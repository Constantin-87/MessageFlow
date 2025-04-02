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

// Retrieve Connection String from Key Vault
var connectionString = builder.Configuration.GetConnectionString("DBConnectionString");
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Database connection string is missing from Azure Key Vault.");
}

// Register DbContext (ensures Identity uses the same DbContext)
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

// Register Unit of Work (from DataAccess)
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Register AutoMapper from Infrastructure
builder.Services.AddAutoMapper(typeof(MappingProfile));

builder.Services.AddIdentityServices();

builder.Services.AddMediatorHandlers();

builder.Services.AddJwtAuthentication(builder.Configuration);

//// Configure JWT Authentication
//var jwtSecretFromVault = builder.Configuration["JsonWebToken-Key"];
//if (string.IsNullOrEmpty(jwtSecretFromVault))
//    throw new InvalidOperationException("JWT key is missing from Azure Key Vault.");

//var key = Encoding.UTF8.GetBytes(jwtSecretFromVault);

//builder.Services.AddAuthentication(options =>
//{
//    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
//    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
//})
//.AddJwtBearer(options =>
//{
//    options.RequireHttpsMetadata = true;
//    options.SaveToken = true;
//    options.TokenValidationParameters = new TokenValidationParameters
//    {
//        ValidateIssuerSigningKey = true,
//        IssuerSigningKey = new SymmetricSecurityKey(key),
//        ValidateIssuer = false,
//        ValidateAudience = false,
//        RoleClaimType = ClaimTypes.Role,
//        NameClaimType = ClaimTypes.NameIdentifier
//    };
//});

builder.Services.AddCorsPolicy();

//// Define allowed origins
//var allowedOrigins = new[] { "https://localhost:5003", "http://localhost:5004", "https://localhost:7164", "http://localhost:5002" };

//builder.Services.AddCors(options =>
//{
//    options.AddPolicy("AllowBlazorWasm", builder =>
//    {
//        builder.SetIsOriginAllowed(origin => new Uri(origin).Host == "localhost") // Allows local development
//               .WithOrigins(allowedOrigins) // Explicitly allow Blazor WebAssembly
//               .WithHeaders("Content-Type", "Authorization")
//                .WithMethods("GET", "POST", "PUT", "DELETE"); 
//    });
//});

// Prevents Json searlization null errors == To be removed when we adjust the DTO's 
//builder.Services.AddControllers()
//    .AddJsonOptions(options =>
//    {
//        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
//    });


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//builder.Services.AddScoped<AuthService>();

//builder.Services.AddScoped<IUserManagementService, UserManagementService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHsts();
app.UseHttpsRedirection();
app.UseRouting(); //Ensure routing is before authentication
app.UseCors("AllowBlazorWasm");
app.UseAuthentication();
app.UseAuthorization();
app.UseEndpoints(endpoints =>
{
    _ = endpoints.MapControllers();
});


app.Run();
