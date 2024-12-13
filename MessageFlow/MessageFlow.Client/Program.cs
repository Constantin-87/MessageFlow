using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.Authorization;
using MessageFlow.Client.Services;
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

//builder.Services.AddBlazorise(options =>
//{
//    options.Immediate = true;
//    options.ProductToken = "<your-product-token>";
//})
//.AddBootstrap5Providers()
//.AddBootstrapIcons();



await builder.Build().RunAsync();
