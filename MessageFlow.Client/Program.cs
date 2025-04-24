using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.Authorization;
using MessageFlow.Client.Services;
using MessageFlow.Client;
using System.Net.Http.Json;
using MessageFlow.Client.Models;
using MessageFlow.Client.Services.Authentication;
using System.Text.Json;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");

// Load configuration from appsettings.json inside wwwroot
using var client = new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };
var configRoot = await client.GetFromJsonAsync<JsonElement>("appsettings.json");

var appConfig = new AppConfig
{
    IdentityApiUrl = configRoot.GetProperty("IdentityApiUrl").GetString(),
    ServerApiUrl = configRoot.GetProperty("ServerApiUrl").GetString(),
    SocialLinks = JsonSerializer.Deserialize<SocialLinks>(configRoot.GetProperty("SocialLinks").GetRawText())
};

builder.Services.AddSingleton(appConfig);

// Add authorization services
builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddSingleton<AuthenticationStateProvider, PersistentAuthenticationStateProvider>();

// Register AuthHttpHandler to attach JWT to every API request
builder.Services.AddScoped<AuthHttpHandler>();
builder.Services.AddSingleton<SessionExpiredNotifier>();

// Configure HTTP Client for Identity API
var identityApiUrl = appConfig.IdentityApiUrl;
if (string.IsNullOrEmpty(identityApiUrl))
    throw new InvalidOperationException("ERROR: 'MessageFlow-Identity-Uri' is missing in configuration.");
builder.Services.AddHttpClient("IdentityAPI", client =>
{
    client.BaseAddress = new Uri(identityApiUrl);
}).AddHttpMessageHandler<AuthHttpHandler>();

// Configure HTTP Client for Server API
var serverApiUrl = appConfig.ServerApiUrl;
if (string.IsNullOrEmpty(serverApiUrl))
    throw new InvalidOperationException("ERROR: 'MessageFlow-Server-Uri' is missing in configuration.");
builder.Services.AddHttpClient("ServerAPI", client =>
{
    client.BaseAddress = new Uri(serverApiUrl);
}).AddHttpMessageHandler<AuthHttpHandler>();

// Register Services
builder.Services.AddScoped<UserManagementService>();
builder.Services.AddScoped<CompanyManagementService>();
builder.Services.AddHttpClient<TeamsManagementService>("ServerAPI");
builder.Services.AddHttpClient<ChannelService>("ServerAPI");
builder.Services.AddScoped<AuthService>();
builder.Services.AddSingleton<CurrentUserService>();
builder.Services.AddScoped<UserHeartbeatService>();

await builder.Build().RunAsync();