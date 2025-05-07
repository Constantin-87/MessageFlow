using MessageFlow.DataAccess.Configurations;
using MessageFlow.DataAccess.Implementations;
using MessageFlow.DataAccess.Models;
using MessageFlow.Tests.Helpers.Factories;

namespace MessageFlow.Tests.UnitTests.DataAccess.Repositories;

public class TeamRepositoryTests
{
    private ApplicationDbContext CreateContext() => UnitTestFactory.CreateInMemoryDbContext();

    [Fact]
    public async Task GetTeamByIdAsync_ReturnsTeamWithRelations()
    {
        using var context = CreateContext();

        var company = new Company
        {
            Id = "c1",
            CompanyName = "Test",
            AccountNumber = "123",
            Description = "Desc",
            IndustryType = "IT",
            WebsiteUrl = "url"
        };
        var user = new ApplicationUser { Id = "u1", CompanyId = "c1" };
        var team = new Team
        {
            Id = "t1",
            TeamName = "A",
            TeamDescription = "Desc",
            CompanyId = "c1",
            Company = company,
            Users = new List<ApplicationUser> { user }
        };

        context.Companies.Add(company);
        context.Users.Add(user);
        context.Teams.Add(team);
        await context.SaveChangesAsync();

        var repo = new TeamRepository(context);
        var result = await repo.GetTeamByIdAsync("t1");

        Assert.NotNull(result);
        Assert.Equal("t1", result!.Id);
        Assert.NotNull(result.Users);
        Assert.Single(result.Users);
    }

    [Fact]
    public async Task GetTeamsByCompanyIdAsync_ReturnsFiltered()
    {
        using var context = CreateContext();

        var team1 = new Team
        {
            Id = "t1",
            CompanyId = "c1",
            TeamName = "Team1",
            TeamDescription = "desc"
        };
        var team2 = new Team
        {
            Id = "t2",
            CompanyId = "c2",
            TeamName = "Team2",
            TeamDescription = "desc"
        };

        context.Teams.AddRange(team1, team2);
        await context.SaveChangesAsync();

        var repo = new TeamRepository(context);
        var result = await repo.GetTeamsByCompanyIdAsync("c1");

        Assert.Single(result);
        Assert.Equal("t1", result[0].Id);
    }

    [Fact]
    public async Task GetTeamsByUserIdAsync_ReturnsCorrectTeams()
    {
        using var context = CreateContext();

        var user = new ApplicationUser { Id = "u1", CompanyId = "c1" };
        var team = new Team
        {
            Id = "t1",
            CompanyId = "c1",
            TeamName = "Team1",
            TeamDescription = "desc",
            Users = new List<ApplicationUser> { user }
        };

        context.Users.Add(user);
        context.Teams.Add(team);
        await context.SaveChangesAsync();

        var repo = new TeamRepository(context);
        var result = await repo.GetTeamsByUserIdAsync("u1");

        Assert.Single(result);
        Assert.Equal("t1", result[0].Id);
    }

    [Fact]
    public async Task GetUsersByTeamIdAsync_ReturnsUsers()
    {
        using var context = CreateContext();

        var user = new ApplicationUser { Id = "u1", CompanyId = "c1" };
        var team = new Team
        {
            Id = "t1",
            CompanyId = "c1",
            TeamName = "Team",
            TeamDescription = "desc",
            Users = new List<ApplicationUser> { user }
        };

        context.Teams.Add(team);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var repo = new TeamRepository(context);
        var users = await repo.GetUsersByTeamIdAsync("t1");

        Assert.Single(users);
        Assert.Equal("u1", users[0].Id);
    }

    [Fact]
    public async Task RemoveUserFromAllTeamsAsync_RemovesSuccessfully()
    {
        using var context = CreateContext();

        var company = new Company
        {
            Id = "c1",
            CompanyName = "Test",
            AccountNumber = "123",
            Description = "desc",
            IndustryType = "it",
            WebsiteUrl = "test.com"
        };

        var user = new ApplicationUser
        {
            Id = "u1",
            CompanyId = "c1",
            Company = company,
            Teams = new List<Team>()
        };

        var team = new Team
        {
            Id = "t1",
            CompanyId = "c1",
            Company = company,
            TeamName = "Team",
            TeamDescription = "desc",
            Users = new List<ApplicationUser> { user }
        };

        user.Teams.Add(team);

        context.Companies.Add(company);
        context.Users.Add(user);
        context.Teams.Add(team);
        await context.SaveChangesAsync();

        var repo = new TeamRepository(context);
        await repo.RemoveUserFromAllTeamsAsync("u1");

        var updated = await repo.GetTeamByIdAsync("t1");
        Assert.NotNull(updated);
        Assert.Empty(updated.Users);
    }

    [Fact]
    public async Task DeleteTeams_RemovesEntities()
    {
        using var context = CreateContext();

        var team = new Team
        {
            Id = "t1",
            CompanyId = "c1",
            TeamName = "Temp",
            TeamDescription = "Temporary team"
        };

        context.Teams.Add(team);
        await context.SaveChangesAsync();

        var repo = new TeamRepository(context);
        repo.DeleteTeams(new List<Team> { team });
        await context.SaveChangesAsync();

        var result = await repo.GetByIdStringAsync("t1");
        Assert.Null(result);
    }
}