using MessageFlow.DataAccess.Configurations;
using MessageFlow.DataAccess.Implementations;
using MessageFlow.DataAccess.Models;

namespace MessageFlow.Tests.Tests.DataAccess.Repositories;
public class CompanyRepositoryTests
{
    private ApplicationDbContext CreateContext() => TestDbContextFactory.CreateDbContext();

    [Fact]
    public async Task GetCompanyWithDetailsByIdAsync_ReturnsCompanyWithRelations()
    {
        using var context = CreateContext();

        var company = new Company
        {
            Id = "c1",
            CompanyName = "TestCo",
            AccountNumber = "123",
            Description = "TestDesc",
            IndustryType = "Tech",
            WebsiteUrl = "http://test.com",
            Users = new List<ApplicationUser> { new() { Id = "u1", CompanyId = "c1" } },
            CompanyEmails = new List<CompanyEmail> { new() { Id = "e1", EmailAddress = "a@test.com", Description = "Main", CompanyId = "c1" } },
            CompanyPhoneNumbers = new List<CompanyPhoneNumber> { new() { Id = "p1", PhoneNumber = "123456", Description = "Main line", CompanyId = "c1" }}
        };

        context.Companies.Add(company);
        await context.SaveChangesAsync();

        var repo = new CompanyRepository(context);
        var result = await repo.GetCompanyWithDetailsByIdAsync("c1");

        Assert.NotNull(result);
        Assert.Equal("c1", result!.Id);
        Assert.Single(result.Users!);
        Assert.Single(result.CompanyEmails!);
        Assert.Single(result.CompanyPhoneNumbers!);
    }

    [Fact]
    public async Task GetAllCompaniesWithUserCountAsync_ReturnsCorrectUserCounts()
    {
        using var context = CreateContext();

        var c1 = new Company
        {
            Id = "c1",
            CompanyName = "One",
            AccountNumber = "A1",
            Description = "D1",
            IndustryType = "IT",
            WebsiteUrl = "url1"
        };
        var c2 = new Company
        {
            Id = "c2",
            CompanyName = "Two",
            AccountNumber = "A2",
            Description = "D2",
            IndustryType = "Finance",
            WebsiteUrl = "url2"
        };

        context.Companies.AddRange(c1, c2);
        context.Users.AddRange(
            new ApplicationUser { Id = "u1", CompanyId = "c1" },
            new ApplicationUser { Id = "u2", CompanyId = "c1" },
            new ApplicationUser { Id = "u3", CompanyId = "c2" }
        );

        await context.SaveChangesAsync();

        var repo = new CompanyRepository(context);
        var companies = await repo.GetAllCompaniesWithUserCountAsync();

        Assert.Equal(2, companies.Count);
        var comp1 = companies.First(x => x.Id == "c1");
        var comp2 = companies.First(x => x.Id == "c2");

        Assert.Equal(2, comp1.TotalUsers);
        Assert.Equal(1, comp2.TotalUsers);
    }

    [Fact]
    public async Task AddUpdateRemove_WorksCorrectly()
    {
        using var context = CreateContext();
        var repo = new CompanyRepository(context);

        var company = new Company
        {
            Id = "x",
            CompanyName = "Test",
            AccountNumber = "acc",
            Description = "desc",
            IndustryType = "type",
            WebsiteUrl = "url"
        };

        await repo.AddEntityAsync(company);
        await context.SaveChangesAsync();

        var added = await repo.GetByIdStringAsync("x");
        Assert.NotNull(added);

        company.Description = "updated";
        await repo.UpdateEntityAsync(company);
        await context.SaveChangesAsync();

        var updated = await repo.GetByIdStringAsync("x");
        Assert.Equal("updated", updated!.Description);

        await repo.RemoveEntityAsync(company);
        await context.SaveChangesAsync();

        var deleted = await repo.GetByIdStringAsync("x");
        Assert.Null(deleted);
    }
}