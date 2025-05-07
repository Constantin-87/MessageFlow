using MessageFlow.DataAccess.Configurations;
using MessageFlow.DataAccess.Implementations;
using MessageFlow.DataAccess.Models;
using MessageFlow.Tests.Helpers.Factories;

namespace MessageFlow.Tests.UnitTests.DataAccess.Repositories;

public class MessageRepositoryTests
{
    private ApplicationDbContext CreateContext() => UnitTestFactory.CreateInMemoryDbContext();

    [Fact]
    public async Task AddEntityAsync_AddsSuccessfully()
    {
        using var context = CreateContext();
        var repo = new MessageRepository(context);

        var message = new Message
        {
            Id = "m1",
            ConversationId = "c1",
            ProviderMessageId = "pm1",
            Content = "Hello"
        };

        await repo.AddEntityAsync(message);
        await context.SaveChangesAsync();

        var saved = await context.Messages.FindAsync("m1");
        Assert.NotNull(saved);
        Assert.Equal("Hello", saved!.Content);
    }

    [Fact]
    public async Task UpdateEntityAsync_UpdatesSuccessfully()
    {
        using var context = CreateContext();
        var msg = new Message
        {
            Id = "m2",
            ConversationId = "c2",
            ProviderMessageId = "pm2",
            Content = "Original"
        };
        context.Messages.Add(msg);
        await context.SaveChangesAsync();

        var repo = new MessageRepository(context);
        msg.Content = "Updated";
        await repo.UpdateEntityAsync(msg);
        await context.SaveChangesAsync();

        var updated = await context.Messages.FindAsync("m2");
        Assert.Equal("Updated", updated!.Content);
    }

    [Fact]
    public async Task GetMessageByIdAsync_ReturnsMessageWithConversation()
    {
        using var context = CreateContext();
        var conv = new Conversation { Id = "c3", SenderId = "s1", CompanyId = "comp" };
        var msg = new Message { Id = "m3", ConversationId = "c3", Content = "X", Conversation = conv };

        context.Conversations.Add(conv);
        context.Messages.Add(msg);
        await context.SaveChangesAsync();

        var repo = new MessageRepository(context);
        var result = await repo.GetMessageByIdAsync("m3");

        Assert.NotNull(result);
        Assert.Equal("c3", result!.ConversationId);
        Assert.NotNull(result.Conversation);
    }

    [Fact]
    public async Task GetMessagesByConversationIdAsync_ReturnsOrderedLimitedList()
    {
        using var context = CreateContext();
        var cId = "c4";
        var m1 = new Message { Id = "m4", ConversationId = cId, SentAt = DateTime.UtcNow.AddMinutes(-1) };
        var m2 = new Message { Id = "m5", ConversationId = cId, SentAt = DateTime.UtcNow };
        var m3 = new Message { Id = "m6", ConversationId = cId, SentAt = DateTime.UtcNow.AddMinutes(-2) };

        context.Messages.AddRange(m1, m2, m3);
        await context.SaveChangesAsync();

        var repo = new MessageRepository(context);
        var result = await repo.GetMessagesByConversationIdAsync(cId, 2);

        Assert.Equal(2, result.Count);
        Assert.Equal("m5", result[0].Id); // Most recent
    }

    [Fact]
    public async Task GetUnreadMessagesBeforeTimestampAsync_ReturnsFilteredMessages()
    {
        using var context = CreateContext();
        var convId = "c5";
        var now = DateTime.UtcNow;
        var msgs = new[]
        {
            new Message { Id = "m7", ConversationId = convId, Status = "sent", SentAt = now.AddMinutes(-2) },
            new Message { Id = "m8", ConversationId = convId, Status = "read", SentAt = now.AddMinutes(-3) },
            new Message { Id = "m9", ConversationId = convId, Status = "sent", SentAt = now.AddMinutes(-1) },
            new Message { Id = "m10", ConversationId = convId, Status = "sent", SentAt = now.AddMinutes(1) }
        };

        context.Messages.AddRange(msgs);
        await context.SaveChangesAsync();

        var repo = new MessageRepository(context);
        var result = await repo.GetUnreadMessagesBeforeTimestampAsync(convId, now);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, x => x.Id == "m7");
        Assert.Contains(result, x => x.Id == "m9");
    }

    [Fact]
    public async Task GetMessageByProviderIdAsync_ReturnsCorrectMessage()
    {
        using var context = CreateContext();
        context.Messages.Add(new Message
        {
            Id = "m11",
            ConversationId = "c6",
            ProviderMessageId = "provider123",
            Content = "Hello"
        });
        await context.SaveChangesAsync();

        var repo = new MessageRepository(context);
        var result = await repo.GetMessageByProviderIdAsync("provider123");

        Assert.NotNull(result);
        Assert.Equal("m11", result!.Id);
    }
}