using MessageFlow.DataAccess.Configurations;
using MessageFlow.DataAccess.Implementations;
using MessageFlow.DataAccess.Models;
using Microsoft.EntityFrameworkCore;

namespace MessageFlow.Tests.Tests.DataAccess.Repositories;

public class CompanyEmailRepositoryTests
{
    private ApplicationDbContext CreateContext() => TestDbContextFactory.CreateDbContext();

    [Fact]
    public async Task UpdateEmailsAsync_AddsAndRemovesCorrectly()
    {
        using var context = CreateContext();
        var repo = new CompanyEmailRepository(context);

        var companyId = "comp-1";
        var existingEmail = new CompanyEmail
        {
            Id = "e1",
            EmailAddress = "old@test.com",
            Description = "Old Email",
            CompanyId = companyId
        };
        context.CompanyEmails.Add(existingEmail);
        await context.SaveChangesAsync();

        var updatedList = new List<CompanyEmail>
        {
            new CompanyEmail
            {
                Id = "e2",
                EmailAddress = "new@test.com",
                Description = "New Email",
                CompanyId = companyId
            }
        };

        await repo.UpdateEmailsAsync(companyId, updatedList);
        await context.SaveChangesAsync();

        var emails = await context.CompanyEmails.Where(e => e.CompanyId == companyId).ToListAsync();
        Assert.Single(emails);
        Assert.Equal("e2", emails[0].Id);
        Assert.Equal("new@test.com", emails[0].EmailAddress);
    }

    [Fact]
    public async Task UpdateEmailsAsync_UpdatesExistingEmail()
    {
        using var context = CreateContext();
        var repo = new CompanyEmailRepository(context);

        var companyId = "comp-2";
        var existingEmail = new CompanyEmail
        {
            Id = "e3",
            EmailAddress = "original@test.com",
            Description = "Original",
            CompanyId = companyId
        };
        context.CompanyEmails.Add(existingEmail);
        await context.SaveChangesAsync();

        var modified = new CompanyEmail
        {
            Id = "e3",
            EmailAddress = "updated@test.com",
            Description = "Updated",
            CompanyId = companyId
        };

        await repo.UpdateEmailsAsync(companyId, new List<CompanyEmail> { modified });
        await context.SaveChangesAsync();

        var updated = await context.CompanyEmails.FindAsync("e3");
        Assert.Equal("updated@test.com", updated!.EmailAddress);
        Assert.Equal("Updated", updated.Description);
    }
}