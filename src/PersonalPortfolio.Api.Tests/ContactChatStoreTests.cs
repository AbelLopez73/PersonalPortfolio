using PersonalPortfolio.Api.Assistant;

namespace PersonalPortfolio.Api.Tests;

public sealed class ContactChatStoreTests
{
    [Fact]
    public void AllowsOneActiveChatAndPromotesNextVisitorAfterOwnerEndsIt()
    {
        var timeProvider = new TestTimeProvider();
        var store = new ContactChatStore(timeProvider);
        store.SetOwnerOnline(true);
        var assistantConversationId = Guid.NewGuid();

        var first = store.Create("First visitor", assistantConversationId, [new("user", "I want to talk")]);
        var second = store.Create("Second visitor", Guid.NewGuid(), [new("user", "I need help")]);

        Assert.Equal("Active", first.Session.Status);
        Assert.Equal(assistantConversationId, first.Session.AssistantConversationId);
        Assert.Equal("Waiting", second.Session.Status);
        Assert.Equal(1, second.Session.QueuePosition);
        Assert.Null(store.AddOwnerMessage(second.Session.Id, "Not active yet"));

        Assert.True(store.Close(first.Session.Id));

        var promoted = store.GetForVisitor(second.Token);
        Assert.NotNull(promoted);
        Assert.Equal("Active", promoted.Status);
        Assert.Null(promoted.QueuePosition);
        Assert.NotNull(store.AddOwnerMessage(second.Session.Id, "I can help now"));
    }

    [Fact]
    public void QueuedVisitorExpiresAfterOneHourAndDoesNotBlockNextChat()
    {
        var timeProvider = new TestTimeProvider();
        var store = new ContactChatStore(timeProvider);
        store.SetOwnerOnline(true);
        var first = store.Create("Active visitor", Guid.NewGuid(), [new("user", "Hello")]);
        var waiting = store.Create("Waiting visitor", Guid.NewGuid(), [new("user", "Hello")]);

        timeProvider.Advance(ContactChatStore.WaitLimit.Add(TimeSpan.FromSeconds(1)));

        var expired = store.GetForVisitor(waiting.Token);
        Assert.NotNull(expired);
        Assert.Equal("Expired", expired.Status);

        store.SetOwnerOnline(true);
        var ownerChats = store.ListForOwner();
        Assert.Contains(ownerChats, chat => chat.Id == first.Session.Id && chat.Status == "Active");
        Assert.Contains(ownerChats, chat => chat.Id == waiting.Session.Id && chat.Status == "Expired");
    }

    [Fact]
    public void OwnerGoingOfflineReturnsActiveVisitorToWaitingQueue()
    {
        var store = new ContactChatStore(new TestTimeProvider());
        store.SetOwnerOnline(true);
        var access = store.Create("Visitor", Guid.NewGuid(), [new("user", "Hello")]);

        store.SetOwnerOnline(false);

        var session = store.GetForVisitor(access.Token);
        Assert.NotNull(session);
        Assert.Equal("Waiting", session.Status);
        Assert.Equal(1, session.QueuePosition);
    }

    private sealed class TestTimeProvider : TimeProvider
    {
        private DateTimeOffset _now = new(2026, 11, 10, 12, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan duration) => _now += duration;
    }
}
