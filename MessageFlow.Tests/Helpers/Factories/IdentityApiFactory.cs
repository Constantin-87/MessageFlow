using AutoMapper;
using MessageFlow.DataAccess.Configurations;
using MessageFlow.DataAccess.Models;
using MessageFlow.DataAccess.Services;
using MessageFlow.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Headers;

namespace MessageFlow.Tests.Helpers.Factories
{
    public class IdentityApiFactory : WebApplicationFactory<Program>
    {
        private readonly string _dbName = Guid.NewGuid().ToString();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Test");

            builder.ConfigureServices(async (services) =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                TestDataSeeder.ConfigureInMemoryDb<ApplicationDbContext>(services, _dbName);

                var sp = services.BuildServiceProvider();

                using var scope = sp.CreateScope();
                var scopedServices = scope.ServiceProvider;
                var context = scopedServices.GetRequiredService<ApplicationDbContext>();
                var userManager = scopedServices.GetRequiredService<UserManager<ApplicationUser>>();

                context.Database.EnsureCreated();
                await TestDataSeeder.SeedAsync(
                scopedServices.GetRequiredService<IUnitOfWork>(),
                scopedServices.GetRequiredService<IMapper>(),
                userManager,
                scopedServices.GetRequiredService<RoleManager<IdentityRole>>()
                );
            });
        }

        public HttpClient CreateClientWithTokenForRole(string role, string testUserId)
        {
            var client = CreateClient();
            var token = JwtTestHelper.GenerateTestJwt(role, testUserId);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }
    }
}