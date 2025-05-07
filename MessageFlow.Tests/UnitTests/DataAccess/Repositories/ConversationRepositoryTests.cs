using MessageFlow.DataAccess.Configurations;
using MessageFlow.DataAccess.Implementations;
using MessageFlow.DataAccess.Models;
using MessageFlow.Tests.Helpers.Factories;

namespace MessageFlow.Tests.UnitTests.DataAccess.Repositories;

public class ConversationRepositoryTests
{
    private ApplicationDbContext CreateContext() => UnitTestFactory.CreateInMemoryDbContext();

    [Fact]
    public async Task AddUpdateRemove_WorksCorrectly()
    {
        using var context = CreateContext();
        var repo = new ConversationRepository(context);

        var convo = new Conversation
        {
            Id = "c1",
            SenderId = "s1",
            SenderUsername = "user",
            AssignedUserId = "u1",
            AssignedTeamId = "t1",
            CompanyId = "comp",
            Source = "whatsapp"
        };

        await repo.AddEntityAsync(convo);
        await context.SaveChangesAsync();

        var fromDb = await repo.GetConversationByIdAsync("c1");
        Assert.NotNull(fromDb);

        convo.SenderUsername = "updated";
        await repo.UpdateEntityAsync(convo);
        await context.SaveChangesAsync();

        var updated = await repo.GetConversationByIdAsync("c1");
        Assert.Equal("updated", updated!.SenderUsername);

        await repo.RemoveEntityAsync(convo);
        await context.SaveChangesAsync();

        var removed = await repo.GetConversationByIdAsync("c1");
        Assert.Null(removed);
    }

    [Fact]
    public async Task GetConversationByIdAsync_ReturnsWithMessages()
    {
        using var context = CreateContext();
        var convo = new Conversation
        {
            Id = "c2",
            SenderId = "s2",
            CompanyId = "co",
            Messages = new List<Message> { new() { Id = "m1", Content = "Hi", ConversationId = "c2" } }
        };

        context.Conversations.Add(convo);
        await context.SaveChangesAsync();

        var repo = new ConversationRepository(context);
        var result = await repo.GetConversationByIdAsync("c2");

        Assert.NotNull(result);
        Assert.Single(result!.Messages);
    }

    [Fact]
    public async Task GetAssignedConversationsAsync_ReturnsExpected()
    {
        using var context = CreateContext();
        context.Conversations.AddRange(
            new Conversation { Id = "c1", AssignedUserId = "u1", CompanyId = "co", IsAssigned = true },
            new Conversation { Id = "c2", AssignedUserId = "u2", CompanyId = "co", IsAssigned = true },
            new Conversation { Id = "c3", AssignedUserId = "u1", CompanyId = "co", IsAssigned = true }
        );
        await context.SaveChangesAsync();

        var repo = new ConversationRepository(context);
        var result = await repo.GetAssignedConversationsAsync("u1", "co");

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetUnassignedConversationsAsync_ReturnsExpected()
    {
        using var context = CreateContext();
        context.Conversations.AddRange(
            new Conversation { Id = "c1", CompanyId = "c", IsAssigned = false },
            new Conversation { Id = "c2", CompanyId = "c", IsAssigned = true },
            new Conversation { Id = "c3", CompanyId = "c", IsAssigned = false }
        );
        await context.SaveChangesAsync();

        var repo = new ConversationRepository(context);
        var result = await repo.GetUnassignedConversationsAsync("c");

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetConversationBySenderIdAsync_ReturnsExpected()
    {
        using var context = CreateContext();
        context.Conversations.Add(new Conversation { Id = "x", SenderId = "sender", CompanyId = "c" });
        await context.SaveChangesAsync();

        var repo = new ConversationRepository(context);
        var result = await repo.GetConversationBySenderIdAsync("sender");

        Assert.NotNull(result);
        Assert.Equal("sender", result!.SenderId);
    }

    [Fact]
    public async Task GetConversationBySenderAndCompanyAsync_ReturnsExpected()
    {
        using var context = CreateContext();
        context.Conversations.Add(new Conversation { Id = "x", SenderId = "s", CompanyId = "co" });
        await context.SaveChangesAsync();

        var repo = new ConversationRepository(context);
        var result = await repo.GetConversationBySenderAndCompanyAsync("s", "co");

        Assert.NotNull(result);
        Assert.Equal("s", result!.SenderId);
        Assert.Equal("co", result.CompanyId);
    }
}