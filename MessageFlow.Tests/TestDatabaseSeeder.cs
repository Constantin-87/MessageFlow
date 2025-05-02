using Microsoft.AspNetCore.Identity;
using MessageFlow.DataAccess.Models;
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
            // Seed Roles (Using RoleManager)
            var roles = new List<string> { "SuperAdmin", "Admin", "AgentManager", "Agent" };

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
                // HeadCompany (Id = "1")
                new TeamDTO { Id = "1", TeamName = "HQ Dev Team", TeamDescription = "Handles internal development", CompanyId = "1" },
                new TeamDTO { Id = "2", TeamName = "HQ Support Team", TeamDescription = "Handles internal support", CompanyId = "1" },
                new TeamDTO { Id = "3", TeamName = "HQ Ops Team", TeamDescription = "Manages operations", CompanyId = "1" },

                // Company A (Id = "2")
                new TeamDTO { Id = "4", TeamName = "A Dev Team", TeamDescription = "Develops products", CompanyId = "2" },
                new TeamDTO { Id = "5", TeamName = "A Support Team", TeamDescription = "Customer support", CompanyId = "2" },
                new TeamDTO { Id = "6", TeamName = "A Marketing Team", TeamDescription = "Marketing and outreach", CompanyId = "2" },

                // Company B (Id = "3")
                new TeamDTO { Id = "7", TeamName = "B Hardware Team", TeamDescription = "Builds hardware", CompanyId = "3" },
                new TeamDTO { Id = "8", TeamName = "B QA Team", TeamDescription = "Tests products", CompanyId = "3" },
                new TeamDTO { Id = "9", TeamName = "B Sales Team", TeamDescription = "Sales and distribution", CompanyId = "3" }
            };

            foreach (var teamDTO in teams)
            {
                var teamEntity = mapper.Map<Team>(teamDTO);  // Map DTO to entity
                await unitOfWork.Teams.AddEntityAsync(teamEntity); // Use repository to add
            }
            await unitOfWork.SaveChangesAsync();

            // Seed Users (Using UserManager and assign roles)
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

            // Assign users to teams
            var companyATeam = await unitOfWork.Teams.GetTeamByIdAsync("4");
            var manager = await userManager.FindByEmailAsync("manager@companya.com");
            companyATeam.Users = new List<ApplicationUser> { manager };

            var companyBTeam = await unitOfWork.Teams.GetTeamByIdAsync("7");
            var agent = await userManager.FindByEmailAsync("agent@companyb.com");
            companyBTeam.Users = new List<ApplicationUser> { agent };

            await unitOfWork.SaveChangesAsync();

        }
    }
}
