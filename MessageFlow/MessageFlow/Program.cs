using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MessageFlow.Components;
using MessageFlow.Components.Accounts.Services;
using MessageFlow.Data;
using MessageFlow.Models;
using MessageFlow.Middleware;
using MessageFlow.Components.Chat.Services;

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddScoped<UserManagementService>();
builder.Services.AddScoped<CompanyManagementService>();
builder.Services.AddScoped<TeamsManagementService>();
builder.Services.AddScoped<FacebookService>();
builder.Services.AddScoped<WhatsAppService>();
builder.Services.AddScoped<ChatArchivingService>();
builder.Services.AddScoped<MessageProcessingService>();


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

var environment = builder.Environment.EnvironmentName;
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var connectionString = environment == "Test"
        ? builder.Configuration.GetConnectionString("TestConnection")
        : builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseSqlServer(connectionString);
});

builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
{
    var connectionString = environment == "Test"
        ? builder.Configuration.GetConnectionString("TestConnection")
        : builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseSqlServer(connectionString);
}, ServiceLifetime.Scoped);


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

builder.WebHost.UseUrls("http://*:5002", "https://*:7164");

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

app.Run();
