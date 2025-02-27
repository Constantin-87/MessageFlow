using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.Authorization;
using MessageFlow.Client.Services;
using Microsoft.Extensions.DependencyInjection;

//using Blazorise;
//using Blazorise.Bootstrap5;
//using Blazorise.Icons.Bootstrap;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Add HttpClient service
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Add authorization services
builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();

// Register PersistentAuthenticationStateProvider
builder.Services.AddSingleton<AuthenticationStateProvider, PersistentAuthenticationStateProvider>();

// Register AuthHttpHandler to attach JWT to every API request
builder.Services.AddScoped<AuthHttpHandler>();

var identityApiUrl = builder.Configuration["MessageFlow-Identity-Uri"];
// Configure HTTP Client for Identity API (Login, Logout, Session)
builder.Services.AddHttpClient("IdentityAPI", client =>
{
    client.BaseAddress = new Uri(identityApiUrl);
}).AddHttpMessageHandler<AuthHttpHandler>();

var serverApiUrl = builder.Configuration["MessageFlow-Server-Uri"];
// Configure HTTP Client for Server API (Protected Endpoints)
builder.Services.AddHttpClient("ServerAPI", client =>
{
    client.BaseAddress = new Uri(serverApiUrl);
}).AddHttpMessageHandler<AuthHttpHandler>();



await builder.Build().RunAsync();
