using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace MessageFlow.DataAccess.Configurations
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            // Load Configuration from appsettings & environment variables
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            // Retrieve Azure Key Vault URL
            string keyVaultUrl = configuration["AzureKeyVaultURL"];
            string connectionString = configuration.GetConnectionString("DBConnectionString");

            if (string.IsNullOrEmpty(connectionString))
            {
                if (!string.IsNullOrEmpty(keyVaultUrl))
                {
                    try
                    {
                        var client = new SecretClient(new Uri(keyVaultUrl), new DefaultAzureCredential());
                        var secret = client.GetSecret("azure-database-connection-string");
                        connectionString = secret.Value.Value;
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException("Failed to retrieve connection string from Azure Key Vault.", ex);
                    }
                }
                else
                {
                    throw new InvalidOperationException("Azure Key Vault URL is not configured.");
                }
            }

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("Database connection string is missing.");
            }

            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseSqlServer(connectionString);

            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}
