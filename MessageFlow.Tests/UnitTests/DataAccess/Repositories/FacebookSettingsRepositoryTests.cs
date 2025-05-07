using MessageFlow.DataAccess.Configurations;
using MessageFlow.DataAccess.Implementations;
using MessageFlow.DataAccess.Models;
using MessageFlow.Tests.Helpers.Factories;

namespace MessageFlow.Tests.UnitTests.DataAccess.Repositories;
public class FacebookSettingsRepositoryTests
{
    private ApplicationDbContext CreateContext() => UnitTestFactory.CreateInMemoryDbContext();

    [Fact]
    public async Task AddEntityAsync_AddsSuccessfully()
    {
        using var context = CreateContext();
        var repo = new FacebookSettingsRepository(context);

        var settings = new FacebookSettingsModel
        {
            Id = "f1",
            PageId = "page123",
            AccessToken = "token",
            CompanyId = "company1"
        };

        await repo.AddEntityAsync(settings);
        await context.SaveChangesAsync();

        var saved = await context.FacebookSettingsModels.FindAsync("f1");
        Assert.NotNull(saved);
        Assert.Equal("page123", saved!.PageId);
    }

    [Fact]
    public async Task UpdateEntityAsync_UpdatesSuccessfully()
    {
        using var context = CreateContext();
        var settings = new FacebookSettingsModel
        {
            Id = "f2",
            PageId = "original",
            AccessToken = "token",
            CompanyId = "company1"
        };

        context.FacebookSettingsModels.Add(settings);
        await context.SaveChangesAsync();

        var repo = new FacebookSettingsRepository(context);
        settings.PageId = "updated";
        await repo.UpdateEntityAsync(settings);
        await context.SaveChangesAsync();

        var updated = await context.FacebookSettingsModels.FindAsync("f2");
        Assert.Equal("updated", updated!.PageId);
    }

    [Fact]
    public async Task GetSettingsByCompanyIdAsync_ReturnsCorrectEntity()
    {
        using var context = CreateContext();
        context.FacebookSettingsModels.Add(new FacebookSettingsModel
        {
            Id = "f3",
            PageId = "p",
            AccessToken = "t",
            CompanyId = "company3"
        });
        await context.SaveChangesAsync();

        var repo = new FacebookSettingsRepository(context);
        var result = await repo.GetSettingsByCompanyIdAsync("company3");

        Assert.NotNull(result);
        Assert.Equal("f3", result!.Id);
    }

    [Fact]
    public async Task GetSettingsByPageIdAsync_ReturnsCorrectEntity()
    {
        using var context = CreateContext();
        context.FacebookSettingsModels.Add(new FacebookSettingsModel
        {
            Id = "f4",
            PageId = "specialPage",
            AccessToken = "token",
            CompanyId = "company4"
        });
        await context.SaveChangesAsync();

        var repo = new FacebookSettingsRepository(context);
        var result = await repo.GetSettingsByPageIdAsync("specialPage");

        Assert.NotNull(result);
        Assert.Equal("f4", result!.Id);
    }
}