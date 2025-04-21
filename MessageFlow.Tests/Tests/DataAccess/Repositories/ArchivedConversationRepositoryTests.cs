using MessageFlow.DataAccess.Configurations;
using MessageFlow.DataAccess.Implementations;
using MessageFlow.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace MessageFlow.Tests.Tests.DataAccess.Repositories;

public class ArchivedConversationRepositoryTests
{
    private ApplicationDbContext CreateContext() => TestDbContextFactory.CreateDbContext();

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