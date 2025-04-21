using MessageFlow.DataAccess.Configurations;
using MessageFlow.DataAccess.Implementations;
using MessageFlow.DataAccess.Models;
using Microsoft.EntityFrameworkCore;

namespace MessageFlow.Tests.Tests.DataAccess.Repositories;

public class CompanyPhoneNumberRepositoryTests
{
    private ApplicationDbContext CreateContext() => TestDbContextFactory.CreateDbContext();

    [Fact]
    public async Task UpdatePhoneNumbersAsync_AddsAndRemovesCorrectly()
    {
        using var context = CreateContext();
        var repo = new CompanyPhoneNumberRepository(context);

        var companyId = "comp-1";
        var existing = new CompanyPhoneNumber
        {
            Id = "p1",
            PhoneNumber = "111-1111",
            Description = "Old",
            CompanyId = companyId
        };
        context.CompanyPhoneNumbers.Add(existing);
        await context.SaveChangesAsync();

        var updatedList = new List<CompanyPhoneNumber>
        {
            new CompanyPhoneNumber
            {
                Id = "p2",
                PhoneNumber = "222-2222",
                Description = "New",
                CompanyId = companyId
            }
        };

        await repo.UpdatePhoneNumbersAsync(companyId, updatedList);
        await context.SaveChangesAsync();

        var result = await context.CompanyPhoneNumbers.Where(p => p.CompanyId == companyId).ToListAsync();
        Assert.Single(result);
        Assert.Equal("p2", result[0].Id);
        Assert.Equal("222-2222", result[0].PhoneNumber);
    }

    [Fact]
    public async Task UpdatePhoneNumbersAsync_UpdatesExistingPhoneNumber()
    {
        using var context = CreateContext();
        var repo = new CompanyPhoneNumberRepository(context);

        var companyId = "comp-2";
        var original = new CompanyPhoneNumber
        {
            Id = "p3",
            PhoneNumber = "333-3333",
            Description = "Original",
            CompanyId = companyId
        };
        context.CompanyPhoneNumbers.Add(original);
        await context.SaveChangesAsync();

        var modified = new CompanyPhoneNumber
        {
            Id = "p3",
            PhoneNumber = "333-9999",
            Description = "Updated",
            CompanyId = companyId
        };

        await repo.UpdatePhoneNumbersAsync(companyId, new List<CompanyPhoneNumber> { modified });
        await context.SaveChangesAsync();

        var updated = await context.CompanyPhoneNumbers.FindAsync("p3");
        Assert.Equal("333-9999", updated!.PhoneNumber);
        Assert.Equal("Updated", updated.Description);
    }
}