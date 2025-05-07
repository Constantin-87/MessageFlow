using MessageFlow.DataAccess.Configurations;
using MessageFlow.DataAccess.Implementations;
using MessageFlow.DataAccess.Models;
using MessageFlow.Tests.Helpers.Factories;
using Microsoft.EntityFrameworkCore;

namespace MessageFlow.Tests.UnitTests.DataAccess.Repositories;

public class ArchivedConversationRepositoryTests
{
    private ApplicationDbContext CreateContext() => UnitTestFactory.CreateInMemoryDbContext();

    [Fact]
    public async Task AddEntityAsync_AddsArchivedConversation()
    {
        using var context = CreateContext();
        var repo = new ArchivedConversationRepository(context);

        var conversation = new ArchivedConversation
        {
            Id = Guid.NewGuid().ToString(),
            CreatedAt = DateTime.UtcNow,
            Source = "Facebook",
            AssignedUserId = "user1",
            CompanyId = "company1"
        };

        await repo.AddEntityAsync(conversation);
        await context.SaveChangesAsync();

        var stored = await context.ArchivedConversations.FirstOrDefaultAsync(c => c.Id == conversation.Id);
        Assert.NotNull(stored);
        Assert.Equal("Facebook", stored!.Source);
        Assert.Equal("user1", stored.AssignedUserId);
    }
}