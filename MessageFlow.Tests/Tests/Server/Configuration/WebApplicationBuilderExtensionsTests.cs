//using Microsoft.AspNetCore.Builder;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Hosting;
//using MessageFlow.Server.Configuration;
//using MessageFlow.DataAccess.Models;
//using Microsoft.AspNetCore.Http;
//using Microsoft.Extensions.Options;
//using Microsoft.AspNetCore.Identity;
//using Microsoft.Extensions.Configuration;

//namespace MessageFlow.Tests.Tests.Configuration
//{
//    public class WebApplicationBuilderExtensionsTests
//    {
//        [Fact]
//        public void ConfigureApp_RegistersCriticalServices_AndValidatesConfig()
//        {
//            // Arrange
//            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
//            {
//                EnvironmentName = Environments.Development,
//                ApplicationName = "MessageFlow.Tests"
//            });

//            // Clear sources and mock configuration
//            builder.Configuration.Sources.Clear();
//            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string>
//            {
//                { "AzureKeyVaultURL", "" },
//                { "meta-app-id", "fake-app-id" },
//                { "meta-app-secret", "fake-secret" },
//                { "facebook-webhook-verify-token", "fb-token" },
//                { "whatsapp-webhook-verify-token", "wa-token" },
//                { "MessageFlow-Identity-Uri", "https://localhost" },
//                { "JwtSettings:Key", "test-key" },
//                { "JwtSettings:Issuer", "test-issuer" },
//                { "JwtSettings:Audience", "test-audience" }
//            });

//            // Act
//            builder.ConfigureApp();
//            var provider = builder.Services.BuildServiceProvider();

//            // Assert
//            Assert.NotNull(provider.GetService<IHttpContextAccessor>());
//            Assert.NotNull(provider.GetService<UserManager<ApplicationUser>>());

//            var settings = provider.GetRequiredService<IOptions<GlobalChannelSettings>>().Value;
//            Assert.Equal("fake-app-id", settings.AppId);
//            Assert.Equal("fb-token", settings.FacebookWebhookVerifyToken);
//        }



//    }
//}