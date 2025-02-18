using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;
using MessageFlow.Server.Data;
using MessageFlow.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace MessageFlow.Server.Tests
{
    public class TestDatabaseSeeder
    {
        public static async Task Seed(ApplicationDbContext context)
        {
            // Ensure the database is created
            context.Database.EnsureCreated();

            using var transaction = context.Database.BeginTransaction(); // Begin a transaction

            // Seed Companies with IDENTITY_INSERT ON
            context.Database.ExecuteSqlRaw("SET IDENTITY_INSERT [Companies] ON");

            var company0 = new Company { Id = 1, CompanyName = "HeadCompany", AccountNumber = "0000" }; // HeadCompany
            var company1 = new Company { Id = 2, CompanyName = "Company A", AccountNumber = "COMP-A123" }; // Company A
            var company2 = new Company { Id = 3, CompanyName = "Company B", AccountNumber = "COMP-B123" }; // Company B
            context.Companies.AddRange(company0, company1, company2);
            context.SaveChanges();
            context.Database.ExecuteSqlRaw("SET IDENTITY_INSERT [Companies] OFF");

            // Seed Teams with IDENTITY_INSERT ON
            context.Database.ExecuteSqlRaw("SET IDENTITY_INSERT [Teams] ON");
            var team1 = new Team { Id = 1, TeamName = "Development Team", CompanyId = company1.Id }; // Team 1 for Company A
            var team2 = new Team { Id = 2, TeamName = "Support Team", CompanyId = company2.Id };     // Team 2 for Company B
            context.Teams.AddRange(team1, team2);
            context.SaveChanges();
            context.Database.ExecuteSqlRaw("SET IDENTITY_INSERT [Teams] OFF");

            transaction.Commit(); // Commit the transaction

            // Seed Roles (if not already present)
            if (!context.Roles.Any())
            {
                context.Roles.AddRange(
                    new IdentityRole { Id = "1", Name = "SuperAdmin", NormalizedName = "SUPERADMIN" },
                    new IdentityRole { Id = "2", Name = "Admin", NormalizedName = "ADMIN" },
                    new IdentityRole { Id = "3", Name = "Manager", NormalizedName = "MANAGER" },
                    new IdentityRole { Id = "4", Name = "Agent", NormalizedName = "AGENT" }
                );

                // Save roles to ensure they exist before assigning to users
                await context.SaveChangesAsync();
            }

            // Seed Users
            var passwordHasher = new PasswordHasher<ApplicationUser>();

            var superAdmin = new ApplicationUser
            {
                Id = "1",
                UserName = "superadmin@headcompany.com",
                Email = "superadmin@headcompany.com",
                NormalizedUserName = "SUPERADMIN@HEADCOMPANY.COM",
                NormalizedEmail = "SUPERADMIN@HEADCOMPANY.COM",
                EmailConfirmed = true,
                LockoutEnabled = false,
                CompanyId = company0.Id
            };
            superAdmin.PasswordHash = passwordHasher.HashPassword(superAdmin, "SuperAdmin@123");

            var agentHeadCompany = new ApplicationUser
            {
                Id = "2",
                UserName = "agent@headcompany.com",
                Email = "agent@headcompany.com",
                NormalizedUserName = "AGENT@HEADCOMPANY.COM",
                NormalizedEmail = "AGENT@HEADCOMPANY.COM",
                EmailConfirmed = true,
                LockoutEnabled = false,
                CompanyId = company0.Id
            };
            agentHeadCompany.PasswordHash = passwordHasher.HashPassword(agentHeadCompany, "Agent@123");

            var adminCompanyA = new ApplicationUser
            {
                Id = "3",
                UserName = "admin@companya.com",
                Email = "admin@companya.com",
                NormalizedUserName = "ADMIN@COMPANYA.COM",
                NormalizedEmail = "ADMIN@COMPANYA.COM",
                EmailConfirmed = true,
                LockoutEnabled = false,
                CompanyId = company1.Id // Belongs to Company A
            };
            adminCompanyA.PasswordHash = passwordHasher.HashPassword(adminCompanyA, "Admin@123");

            var managerCompanyA = new ApplicationUser
            {
                Id = "4",
                UserName = "manager@companya.com",
                Email = "manager@companya.com",
                NormalizedUserName = "MANAGER@COMPANYA.COM",
                NormalizedEmail = "MANAGER@COMPANYA.COM",
                EmailConfirmed = true,
                LockoutEnabled = false,
                CompanyId = company1.Id // Belongs to Company A
            };
            managerCompanyA.PasswordHash = passwordHasher.HashPassword(managerCompanyA, "Manager@123");

            var agentCompanyB = new ApplicationUser
            {
                Id = "5",
                UserName = "agent@companyb.com",
                Email = "agent@companyb.com",
                NormalizedUserName = "AGENT@COMPANYB.COM",
                NormalizedEmail = "AGENT@COMPANYB.COM",
                EmailConfirmed = true,
                LockoutEnabled = false,
                CompanyId = company2.Id // Belongs to Company B
            };
            agentCompanyB.PasswordHash = passwordHasher.HashPassword(agentCompanyB, "Agent@123");
                   
            context.Users.AddRange(superAdmin, agentHeadCompany, adminCompanyA, managerCompanyA, agentCompanyB);

            // Save users
            context.SaveChanges();

            // Assign roles to users
            context.UserRoles.AddRange(
                new IdentityUserRole<string> { UserId = superAdmin.Id, RoleId = "1" }, // SuperAdmin
                new IdentityUserRole<string> { UserId = agentHeadCompany.Id, RoleId = "4" }, // Agent
                new IdentityUserRole<string> { UserId = adminCompanyA.Id, RoleId = "2" },     // Admin
                new IdentityUserRole<string> { UserId = managerCompanyA.Id, RoleId = "3" },   // Manager
                new IdentityUserRole<string> { UserId = agentCompanyB.Id, RoleId = "4" }     // Agent
            );

            // Save user-role assignments
            context.SaveChanges();
        }
    }
}
