using MessageFlow.DataAccess.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageFlow.DataAccess.Configurations
{
    public static class DatabaseSeeder
    {
        public static async Task SeedSuperAdminAsync(IServiceProvider serviceProvider)
        {
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var dbContext = serviceProvider.GetRequiredService<ApplicationDbContext>();

            // 1️⃣ Seed Default Company if it doesn't exist
            var defaultCompanyId = "MessageFlow-company-id";
            var existingCompany = await dbContext.Companies.FindAsync(defaultCompanyId);
            if (existingCompany == null)
            {
                var defaultCompany = new Company
                {
                    Id = defaultCompanyId,
                    CompanyName = "MessageFlow",
                    AccountNumber = "ACC-0001",
                    Description = "MessageFlow",
                    IndustryType = "General",
                    WebsiteUrl = "https://defaultcompany.com"
                };

                dbContext.Companies.Add(defaultCompany);
                await dbContext.SaveChangesAsync();  // Save the company first
            }

            // 2️⃣ Seed Roles
            var roles = new[] { "SuperAdmin", "Admin", "Agent", "AgentManager" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // 3️⃣ Seed SuperAdmin
            var superAdminEmail = "superadmin@example.com";
            var existingUser = await userManager.FindByEmailAsync(superAdminEmail);

            if (existingUser == null)
            {
                var superAdmin = new ApplicationUser
                {
                    UserName = "superadmin",
                    Email = superAdminEmail,
                    EmailConfirmed = true,
                    CompanyId = defaultCompanyId, // ✅ Now this ID exists
                    LastActivity = DateTime.UtcNow
                };

                var result = await userManager.CreateAsync(superAdmin, "SuperAdmin@123"); // Default password
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(superAdmin, "SuperAdmin");
                }
                else
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    throw new Exception($"Failed to create SuperAdmin: {errors}");
                }
            }
        }

    }
}
