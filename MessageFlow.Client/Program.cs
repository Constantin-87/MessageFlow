using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.Authorization;
using MessageFlow.Client.Services;
using MessageFlow.Client;
using System.Net.Http.Json;
using MessageFlow.Client.Models;

//using Blazorise;
//using Blazorise.Bootstrap5;
//using Blazorise.Icons.Bootstrap;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");

// Load configuration from appsettings.json inside wwwroot
using var client = new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };
var config = await client.GetFromJsonAsync<AppConfig>("appsettings.json");

if (config == null)
    throw new InvalidOperationException("Failed to load configuration from appsettings.json");

builder.Configuration["MessageFlow-Server-Uri"] = config.ServerApiUrl;
builder.Configuration["MessageFlow-Identity-Uri"] = config.IdentityApiUrl;

// Add default HttpClient for general use (optional, but fine)
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Add authorization services
builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();

// Register PersistentAuthenticationStateProvider
builder.Services.AddSingleton<AuthenticationStateProvider, PersistentAuthenticationStateProvider>();

// Register AuthHttpHandler to attach JWT to every API request
builder.Services.AddScoped<AuthHttpHandler>();


// Configure HTTP Client for Identity API (Login, Logout, Session)
var identityApiUrl = builder.Configuration["MessageFlow-Identity-Uri"];
if (string.IsNullOrEmpty(identityApiUrl))
{
    throw new InvalidOperationException("ERROR: 'MessageFlow-Identity-Uri' is missing in configuration.");
}
builder.Services.AddHttpClient("IdentityAPI", client =>
{
    client.BaseAddress = new Uri(identityApiUrl);
}).AddHttpMessageHandler<AuthHttpHandler>();


// Configure HTTP Client for Server API (Protected Endpoints)
var serverApiUrl = builder.Configuration["MessageFlow-Server-Uri"];
if (string.IsNullOrEmpty(serverApiUrl))
    throw new InvalidOperationException("ERROR: 'MessageFlow-Server-Uri' is missing in configuration.");

builder.Services.AddHttpClient("ServerAPI", client =>
{
    client.BaseAddress = new Uri(serverApiUrl);
}).AddHttpMessageHandler<AuthHttpHandler>();

// Register Services
//builder.Services.AddScoped<UserActivityService>();
builder.Services.AddScoped<UserManagementService>();
builder.Services.AddScoped<CompanyManagementService>();
builder.Services.AddHttpClient<TeamsManagementService>("ServerAPI");
builder.Services.AddHttpClient<ChannelService>("ServerAPI");
builder.Services.AddScoped<AuthService>();

await builder.Build().RunAsync();
