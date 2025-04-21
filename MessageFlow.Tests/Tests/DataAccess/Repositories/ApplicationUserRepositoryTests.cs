using MessageFlow.DataAccess.Configurations;
using MessageFlow.DataAccess.Implementations;
using MessageFlow.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace MessageFlow.Tests.Tests.DataAccess.Repositories;

public class ApplicationUserRepositoryTests
{
    private ApplicationDbContext CreateContext() => TestDbContextFactory.CreateDbContext();

    [Fact]
    public async Task GetUserByIdAsync_ReturnsUserWithCompanyAndTeams()
    {
        using var context = CreateContext();

        var company = new Company
        {
            Id = "c1",
            AccountNumber = "ACC123",
            CompanyName = "TestCo",
            Description = "A test company",
            IndustryType = "IT",
            WebsiteUrl = "https://test.com"
        };

        var team = new Team
        {
            Id = "t1",
            TeamName = "Team A",
            TeamDescription = "First team",
            CompanyId = "c1",
            Company = company
        };

        var user = new ApplicationUser
        {
            Id = "u1",
            UserName = "test@example.com",
            CompanyId = "c1",
            Company = company,
            Teams = new List<Team> { team },
            Email = "test@example.com"
        };

        context.Companies.Add(company);
        context.Teams.Add(team);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var repo = new ApplicationUserRepository(context);
        var result = await repo.GetUserByIdAsync("u1");

        Assert.NotNull(result);
        Assert.Equal("u1", result!.Id);
        Assert.NotNull(result.Company);
        Assert.Single(result.Teams);
        Assert.Equal("c1", result.Company.Id);
    }

    [Fact]
    public async Task GetListOfEntitiesByIdStringAsync_ReturnsExpectedUsers()
    {
        using var context = CreateContext();

        var user1 = new ApplicationUser { Id = "u1", UserName = "user1@test.com", CompanyId = "c1", Email = "user1@test.com" };
        var user2 = new ApplicationUser { Id = "u2", UserName = "user2@test.com", CompanyId = "c1", Email = "user2@test.com" };
        var user3 = new ApplicationUser { Id = "u3", UserName = "user3@test.com", CompanyId = "c1", Email = "user3@test.com" };

        context.Users.AddRange(user1, user2, user3);
        await context.SaveChangesAsync();

        var repo = new ApplicationUserRepository(context);
        var result = await repo.GetListOfEntitiesByIdStringAsync(new[] { "u1", "u3" });

        Assert.Equal(2, result.Count);
        Assert.Contains(result, u => u.Id == "u1");
        Assert.Contains(result, u => u.Id == "u3");
    }
}