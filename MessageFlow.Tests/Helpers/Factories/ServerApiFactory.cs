using AutoMapper;
using MessageFlow.AzureServices.Interfaces;
using MessageFlow.DataAccess.Configurations;
using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Services;
using MessageFlow.Server;
using MessageFlow.Tests.Helpers.Stubs;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net.Http.Headers;


namespace MessageFlow.Tests.Helpers.Factories
{
    public class ServerApiFactory : WebApplicationFactory<Program>
    {
        private readonly string _dbName = Guid.NewGuid().ToString();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Test");

            builder.ConfigureServices(async (services) =>
            {
                // Configure In-Memory database
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                TestDataSeeder.ConfigureInMemoryDb<ApplicationDbContext>(services, _dbName);

                // Replace AzureBlobStorageService with the fake one
                services.RemoveAll(typeof(IAzureBlobStorageService));
                services.AddScoped<IAzureBlobStorageService, FakeAzureBlobStorageService>();

                // Replace Azure DocumentProcessingService with the fake one
                services.RemoveAll(typeof(IDocumentProcessingService));
                services.AddScoped<IDocumentProcessingService, FakeDocumentProcessingService>();

                // Replace Azure Search service with the fake one
                services.RemoveAll(typeof(IAzureSearchService));
                services.AddScoped<IAzureSearchService, FakeAzureSearchService>();

                var sp = services.BuildServiceProvider();

                using var scope = sp.CreateScope();
                var scopedServices = scope.ServiceProvider;
                var context = scopedServices.GetRequiredService<ApplicationDbContext>();
                var userManager = scopedServices.GetRequiredService<UserManager<ApplicationUser>>();

                context.Database.EnsureCreated();
                await TestDataSeeder.SeedAsync(scopedServices.GetRequiredService<IUnitOfWork>(), scopedServices.GetRequiredService<IMapper>(), userManager, scopedServices.GetRequiredService<RoleManager<IdentityRole>>());
            });
        }

        public HttpClient CreateClientWithSuperAdminAuth()
        {
            var client = CreateClient();
            var token = JwtTestHelper.GenerateTestJwt("SuperAdmin", "1");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        public HttpClient CreateUnauthenticatedClient()
        {
            var client = CreateClient();
            // No authorization header set, making the client unauthenticated.
            client.DefaultRequestHeaders.Authorization = null;
            return client;
        }
    }
}