using Microsoft.AspNetCore.Identity;
using MessageFlow.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using MessageFlow.DataAccess.Services;
using MessageFlow.Shared.DTOs;
using AutoMapper;

namespace MessageFlow.Tests
{
    public class TestDatabaseSeeder
    {
        public static async Task Seed(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            // 🚀 1. Seed Roles (Using RoleManager)
            var roles = new List<string> { "SuperAdmin", "Admin", "Manager", "Agent" };

            foreach (var roleName in roles)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole
                    {
                        Name = roleName,
                        NormalizedName = roleName.ToUpper()
                    });
                }
            }

            // Seed Companies with all required fields
            var companies = new List<CompanyDTO>
            {
                new CompanyDTO
                {
                    Id = "1",
                    CompanyName = "HeadCompany",
                    AccountNumber = "0000",
                    Description = "Main company managing operations",
                    IndustryType = "Technology",
                    WebsiteUrl = "https://headcompany.com"
                },
                new CompanyDTO
                {
                    Id = "2",
                    CompanyName = "Company A",
                    AccountNumber = "COMP-A123",
                    Description = "Leading provider of tech solutions",
                    IndustryType = "Software",
                    WebsiteUrl = "https://companya.com"
                },
                new CompanyDTO
                {
                    Id = "3",
                    CompanyName = "Company B",
                    AccountNumber = "COMP-B123",
                    Description = "Specializes in hardware manufacturing",
                    IndustryType = "Hardware",
                    WebsiteUrl = "https://companyb.com"
                }
            };

            // Map and add companies
            foreach (var company in companies)
            {
                var companyEntity = mapper.Map<Company>(company);
                await unitOfWork.Companies.AddEntityAsync(companyEntity);
            }
            await unitOfWork.SaveChangesAsync();


            // Seed Teams
            var teams = new List<TeamDTO>
            {
                new TeamDTO { Id = "1", TeamName = "Development Team", CompanyId = "2" }, // Company A
                new TeamDTO { Id = "2", TeamName = "Support Team", CompanyId = "3" }      // Company B
            };
            foreach (var teamDTO in teams)
            {
                var teamEntity = mapper.Map<Team>(teamDTO);  // Map DTO to entity
                await unitOfWork.Teams.AddEntityAsync(teamEntity); // Use repository to add
            }
            await unitOfWork.SaveChangesAsync();

            // 🚀 4. Seed Users (Using UserManager and assign roles)
            var usersToSeed = new List<(ApplicationUser User, string Password, string Role)>
            {
                (new ApplicationUser { Id = "1", UserName = "superadmin@headcompany.com", Email = "superadmin@headcompany.com", CompanyId = "1" }, "SuperAdmin@123", "SuperAdmin"),
                (new ApplicationUser { Id = "2", UserName = "agent@headcompany.com", Email = "agent@headcompany.com", CompanyId = "1" }, "Agent@123", "Agent"),
                (new ApplicationUser { Id = "3", UserName = "admin@companya.com", Email = "admin@companya.com", CompanyId = "2" }, "Admin@123", "Admin"),
                (new ApplicationUser { Id = "4", UserName = "manager@companya.com", Email = "manager@companya.com", CompanyId = "2" }, "Manager@123", "Manager"),
                (new ApplicationUser { Id = "5", UserName = "agent@companyb.com", Email = "agent@companyb.com", CompanyId = "3" }, "Agent@123", "Agent")
            };

            foreach (var (user, password, role) in usersToSeed)
            {
                var userToProcess = await userManager.FindByEmailAsync(user.Email) ?? user;

                // Create the user if it doesn't exist
                if (userToProcess.Id == user.Id)  // Means user was not found and is newly created
                {
                    var result = await userManager.CreateAsync(userToProcess, password);
                    if (!result.Succeeded)
                    {
                        throw new Exception($"Failed to create user {userToProcess.Email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    }
                }

                // Assign role if not already assigned
                var userRoles = await userManager.GetRolesAsync(userToProcess);
                if (!userRoles.Contains(role))
                {
                    var roleAssignResult = await userManager.AddToRoleAsync(userToProcess, role);
                    if (!roleAssignResult.Succeeded)
                    {
                        throw new Exception($"Failed to assign role {role} to {userToProcess.Email}: {string.Join(", ", roleAssignResult.Errors.Select(e => e.Description))}");
                    }
                }
            }

            await unitOfWork.SaveChangesAsync();


            //// 🚀 Assign roles to users using ApplicationUserRepository
            //var userRolesToAssign = new List<(string UserId, string RoleName)>
            //{
            //    ("1", "SuperAdmin"),
            //    ("2", "Agent"),
            //    ("3", "Admin"),
            //    ("4", "Manager"),
            //    ("5", "Agent")
            //};

            //foreach (var (userId, roleName) in userRolesToAssign)
            //{
            //    var existingRoles = await unitOfWork.ApplicationUsers.GetRoleForUserAsync(userId);

            //    if (!existingRoles.Contains(roleName))
            //    {
            //        // Create new IdentityUserRole and add it directly to the context
            //        var userRole = new IdentityUserRole<string>
            //        {
            //            UserId = userId,
            //            RoleId = (await Context.Roles.FirstOrDefaultAsync(r => r.Name == roleName))?.Id
            //                     ?? throw new Exception($"Role '{roleName}' not found.")
            //        };

            //        Context.UserRoles.Add(userRole);  // Add the user-role mapping
            //    }
            //}

            //await unitOfWork.SaveChangesAsync();  // Save user-role assignments


            //// Seed Users
            //var passwordHasher = new PasswordHasher<ApplicationUser>();

            //var superAdmin = new ApplicationUser
            //{
            //    Id = "1",
            //    UserName = "superadmin@headcompany.com",
            //    Email = "superadmin@headcompany.com",
            //    NormalizedUserName = "SUPERADMIN@HEADCOMPANY.COM",
            //    NormalizedEmail = "SUPERADMIN@HEADCOMPANY.COM",
            //    EmailConfirmed = true,
            //    LockoutEnabled = false,
            //    CompanyId = company0.Id
            //};
            //superAdmin.PasswordHash = passwordHasher.HashPassword(superAdmin, "SuperAdmin@123");

            //var agentHeadCompany = new ApplicationUser
            //{
            //    Id = "2",
            //    UserName = "agent@headcompany.com",
            //    Email = "agent@headcompany.com",
            //    NormalizedUserName = "AGENT@HEADCOMPANY.COM",
            //    NormalizedEmail = "AGENT@HEADCOMPANY.COM",
            //    EmailConfirmed = true,
            //    LockoutEnabled = false,
            //    CompanyId = company0.Id
            //};
            //agentHeadCompany.PasswordHash = passwordHasher.HashPassword(agentHeadCompany, "Agent@123");

            //var adminCompanyA = new ApplicationUser
            //{
            //    Id = "3",
            //    UserName = "admin@companya.com",
            //    Email = "admin@companya.com",
            //    NormalizedUserName = "ADMIN@COMPANYA.COM",
            //    NormalizedEmail = "ADMIN@COMPANYA.COM",
            //    EmailConfirmed = true,
            //    LockoutEnabled = false,
            //    CompanyId = company1.Id // Belongs to Company A
            //};
            //adminCompanyA.PasswordHash = passwordHasher.HashPassword(adminCompanyA, "Admin@123");

            //var managerCompanyA = new ApplicationUser
            //{
            //    Id = "4",
            //    UserName = "manager@companya.com",
            //    Email = "manager@companya.com",
            //    NormalizedUserName = "MANAGER@COMPANYA.COM",
            //    NormalizedEmail = "MANAGER@COMPANYA.COM",
            //    EmailConfirmed = true,
            //    LockoutEnabled = false,
            //    CompanyId = company1.Id // Belongs to Company A
            //};
            //managerCompanyA.PasswordHash = passwordHasher.HashPassword(managerCompanyA, "Manager@123");

            //var agentCompanyB = new ApplicationUser
            //{
            //    Id = "5",
            //    UserName = "agent@companyb.com",
            //    Email = "agent@companyb.com",
            //    NormalizedUserName = "AGENT@COMPANYB.COM",
            //    NormalizedEmail = "AGENT@COMPANYB.COM",
            //    EmailConfirmed = true,
            //    LockoutEnabled = false,
            //    CompanyId = company2.Id // Belongs to Company B
            //};
            //agentCompanyB.PasswordHash = passwordHasher.HashPassword(agentCompanyB, "Agent@123");

            //context.Users.AddRange(superAdmin, agentHeadCompany, adminCompanyA, managerCompanyA, agentCompanyB);

            //// Save users
            //context.SaveChanges();

            //// Assign roles to users
            //context.UserRoles.AddRange(
            //    new IdentityUserRole<string> { UserId = superAdmin.Id, RoleId = "1" }, // SuperAdmin
            //    new IdentityUserRole<string> { UserId = agentHeadCompany.Id, RoleId = "4" }, // Agent
            //    new IdentityUserRole<string> { UserId = adminCompanyA.Id, RoleId = "2" },     // Admin
            //    new IdentityUserRole<string> { UserId = managerCompanyA.Id, RoleId = "3" },   // Manager
            //    new IdentityUserRole<string> { UserId = agentCompanyB.Id, RoleId = "4" }     // Agent
            //);

            //// Save user-role assignments
            //context.SaveChanges();
        }
    }
}
