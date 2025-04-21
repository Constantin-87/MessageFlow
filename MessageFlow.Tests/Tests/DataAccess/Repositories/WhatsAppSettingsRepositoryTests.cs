using MessageFlow.DataAccess.Configurations;
using MessageFlow.DataAccess.Implementations;
using MessageFlow.DataAccess.Models;

namespace MessageFlow.Tests.Tests.DataAccess.Repositories;

public class WhatsAppSettingsRepositoryTests
{
    private ApplicationDbContext CreateContext() => TestDbContextFactory.CreateDbContext();

    [Fact]
    public async Task GetSettingsByCompanyIdAsync_ReturnsCorrectSettings()
    {
        using var context = CreateContext();
        var settings = new WhatsAppSettingsModel
        {
            CompanyId = "c1",
            AccessToken = "token",
            BusinessAccountId = "biz123",
            PhoneNumbers = new List<PhoneNumberInfo>
            {
                new()
                {
                    PhoneNumber = "123",
                    PhoneNumberId = "ph123",
                    PhoneNumberDesc = "Support"
                }
            }
        };
        context.WhatsAppSettingsModels.Add(settings);
        await context.SaveChangesAsync();

        var repo = new WhatsAppSettingsRepository(context);
        var result = await repo.GetSettingsByCompanyIdAsync("c1");

        Assert.NotNull(result);
        Assert.Equal("c1", result!.CompanyId);
        Assert.Single(result.PhoneNumbers);
    }

    [Fact]
    public async Task GetSettingsByBusinessAccountIdAsync_ReturnsCorrectSettings()
    {
        using var context = CreateContext();
        var settings = new WhatsAppSettingsModel
        {
            CompanyId = "c2",
            AccessToken = "xyz",
            BusinessAccountId = "biz999",
            PhoneNumbers = new List<PhoneNumberInfo>
            {
                new()
                {
                    PhoneNumber = "p2",
                    PhoneNumberId = "999",
                    PhoneNumberDesc = "Sales"
                }
            }
        };
        context.WhatsAppSettingsModels.Add(settings);
        await context.SaveChangesAsync();

        var repo = new WhatsAppSettingsRepository(context);
        var result = await repo.GetSettingsByBusinessAccountIdAsync("biz999");

        Assert.NotNull(result);
        Assert.Equal("biz999", result!.BusinessAccountId);
        Assert.Single(result.PhoneNumbers);
    }

    [Fact]
    public async Task AddUpdate_WorksCorrectly()
    {
        using var context = CreateContext();
        var repo = new WhatsAppSettingsRepository(context);

        var settings = new WhatsAppSettingsModel
        {
            Id = "s1",
            CompanyId = "c3",
            AccessToken = "initial",
            BusinessAccountId = "biz3"
        };

        await repo.AddEntityAsync(settings);
        await context.SaveChangesAsync();

        var inserted = await repo.GetByIdStringAsync("s1");
        Assert.Equal("initial", inserted!.AccessToken);

        settings.AccessToken = "updated";
        await repo.UpdateEntityAsync(settings);
        await context.SaveChangesAsync();

        var updated = await repo.GetByIdStringAsync("s1");
        Assert.Equal("updated", updated!.AccessToken);
    }
}