using System.Security.Cryptography;
using System.Text;

namespace PersonalPortfolio.Api.Assistant;

public interface IContactChatStore
{
    void SetOwnerOnline(bool isOnline);

    ContactChatAccess Create(
        string visitorName,
        IReadOnlyList<AssistantChatMessage> initialMessages);

    ContactChatSession? GetForVisitor(string? token);
    ContactChatSession? AddVisitorMessage(string? token, string content);
    IReadOnlyList<ContactChatSummary> ListForOwner();
    ContactChatSession? GetForOwner(Guid id);
    ContactChatSession? AddOwnerMessage(Guid id, string content);
    bool Close(Guid id);
}

/// <summary>One visitor, assistant, or owner message in a website chat.</summary>
public sealed record ContactChatMessage(string Sender, string Content, DateTimeOffset SentAt);

/// <summary>Website chat session returned to the visitor or owner.</summary>
public sealed record ContactChatSession(
    Guid Id,
    string VisitorName,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastActivityAt,
    string Status,
    int? QueuePosition,
    IReadOnlyList<ContactChatMessage> Messages);

/// <summary>Summary of a website chat in the owner's queue.</summary>
public sealed record ContactChatSummary(
    Guid Id,
    string VisitorName,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastActivityAt,
    string Status,
    int? QueuePosition,
    string LastMessage);

public sealed record ContactChatAccess(ContactChatSession Session, string Token);

public sealed class ContactChatQueueFullException()
    : Exception("The contact chat queue is full.");

public sealed class ContactChatStore : IContactChatStore
{
    public const int MaxMessageCount = 200;
    public static readonly TimeSpan WaitLimit = TimeSpan.FromHours(1);
    private static readonly TimeSpan Retention = TimeSpan.FromDays(30);
    private const int MaxActiveConversations = 100;

    private readonly object _gate = new();
    private readonly TimeProvider _timeProvider;
    private readonly Dictionary<Guid, Conversation> _conversations = [];
    private readonly Dictionary<string, Guid> _tokenIndexes = [];
    private Guid? _activeConversationId;
    private bool _ownerOnline;

    public ContactChatStore(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public void SetOwnerOnline(bool isOnline)
    {
        lock (_gate)
        {
            var now = _timeProvider.GetUtcNow();
            ExpireWaiting(now);
            if (_ownerOnline == isOnline)
            {
                PromoteNextWaiting(now);
                return;
            }

            _ownerOnline = isOnline;
            if (!isOnline && _activeConversationId is { } activeId &&
                _conversations.TryGetValue(activeId, out var activeConversation))
            {
                activeConversation.Status = "Waiting";
                activeConversation.QueuedAt = now;
                activeConversation.LastActivityAt = now;
                _activeConversationId = null;
            }

            PromoteNextWaiting(now);
        }
    }

    public ContactChatAccess Create(
        string visitorName,
        IReadOnlyList<AssistantChatMessage> initialMessages)
    {
        lock (_gate)
        {
            var now = _timeProvider.GetUtcNow();
            ExpireWaiting(now);
            PromoteNextWaiting(now);
            if (_conversations.Values.Count(conversation => conversation.Status is "Active" or "Waiting") >=
                MaxActiveConversations)
            {
                throw new ContactChatQueueFullException();
            }

            var id = Guid.NewGuid();
            var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            var tokenIndex = HashToken(token);
            var messages = initialMessages
                .TakeLast(16)
                .Where(message => message.Role is "user" or "assistant")
                .Select(message => new ContactChatMessage(
                    message.Role == "user" ? "visitor" : "assistant",
                    message.Content,
                    now))
                .ToList();

            var status = _ownerOnline && _activeConversationId is null ? "Active" : "Waiting";
            var conversation = new Conversation(id, visitorName, tokenIndex, now, messages, status);
            _conversations.Add(id, conversation);
            _tokenIndexes.Add(tokenIndex, id);
            if (status == "Active")
            {
                _activeConversationId = id;
            }

            return new ContactChatAccess(
                CreateSnapshot(conversation, GetQueuePosition(conversation)),
                token);
        }
    }

    public ContactChatSession? GetForVisitor(string? token)
    {
        lock (_gate)
        {
            var now = _timeProvider.GetUtcNow();
            ExpireWaiting(now);
            PromoteNextWaiting(now);
            return FindByToken(token) is { } conversation
                ? CreateSnapshot(conversation, GetQueuePosition(conversation))
                : null;
        }
    }

    public ContactChatSession? AddVisitorMessage(string? token, string content)
    {
        lock (_gate)
        {
            var now = _timeProvider.GetUtcNow();
            ExpireWaiting(now);
            PromoteNextWaiting(now);
            if (FindByToken(token) is not { Status: "Active" or "Waiting" } conversation ||
                conversation.Messages.Count >= MaxMessageCount)
            {
                return null;
            }

            AddMessage(conversation, "visitor", content, now);
            return CreateSnapshot(conversation, GetQueuePosition(conversation));
        }
    }

    public IReadOnlyList<ContactChatSummary> ListForOwner()
    {
        lock (_gate)
        {
            var now = _timeProvider.GetUtcNow();
            ExpireWaiting(now);
            PromoteNextWaiting(now);
            return _conversations.Values
                .OrderBy(conversation => conversation.Status == "Active" ? 0 : conversation.Status == "Waiting" ? 1 : 2)
                .ThenBy(conversation => conversation.QueuedAt)
                .Select(conversation => new ContactChatSummary(
                    conversation.Id,
                    conversation.VisitorName,
                    conversation.CreatedAt,
                    conversation.LastActivityAt,
                    conversation.Status,
                    GetQueuePosition(conversation),
                    conversation.Messages.LastOrDefault()?.Content ?? string.Empty))
                .ToArray();
        }
    }

    public ContactChatSession? GetForOwner(Guid id)
    {
        lock (_gate)
        {
            var now = _timeProvider.GetUtcNow();
            ExpireWaiting(now);
            PromoteNextWaiting(now);
            return _conversations.TryGetValue(id, out var conversation)
                ? CreateSnapshot(conversation, GetQueuePosition(conversation))
                : null;
        }
    }

    public ContactChatSession? AddOwnerMessage(Guid id, string content)
    {
        lock (_gate)
        {
            var now = _timeProvider.GetUtcNow();
            ExpireWaiting(now);
            PromoteNextWaiting(now);
            if (!_conversations.TryGetValue(id, out var conversation) ||
                conversation.Status != "Active" ||
                conversation.Messages.Count >= MaxMessageCount)
            {
                return null;
            }

            AddMessage(conversation, "owner", content, now);
            return CreateSnapshot(conversation, null);
        }
    }

    public bool Close(Guid id)
    {
        lock (_gate)
        {
            var now = _timeProvider.GetUtcNow();
            ExpireWaiting(now);
            if (!_conversations.TryGetValue(id, out var conversation))
            {
                return false;
            }

            if (conversation.Status == "Active")
            {
                _activeConversationId = null;
            }

            conversation.Status = "Closed";
            conversation.LastActivityAt = now;
            PromoteNextWaiting(now);
            return true;
        }
    }

    private int? GetQueuePosition(Conversation conversation)
    {
        if (conversation.Status == "Active")
        {
            return null;
        }

        if (conversation.Status != "Waiting")
        {
            return null;
        }

        var waiting = _conversations.Values
            .Where(item => item.Status == "Waiting")
            .OrderBy(item => item.QueuedAt)
            .ToArray();
        for (var index = 0; index < waiting.Length; index++)
        {
            if (waiting[index].Id == conversation.Id)
            {
                return index + 1;
            }
        }

        return null;
    }

    private Conversation? FindByToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        return _tokenIndexes.TryGetValue(HashToken(token), out var id) &&
               _conversations.TryGetValue(id, out var conversation)
            ? conversation
            : null;
    }

    private static void AddMessage(
        Conversation conversation,
        string sender,
        string content,
        DateTimeOffset now)
    {
        conversation.Messages.Add(new ContactChatMessage(sender, content, now));
        conversation.LastActivityAt = now;
    }

    private ContactChatSession CreateSnapshot(Conversation conversation, int? queuePosition) =>
        new(
            conversation.Id,
            conversation.VisitorName,
            conversation.CreatedAt,
            conversation.LastActivityAt,
            conversation.Status,
            queuePosition,
            conversation.Messages.ToArray());

    private void ExpireWaiting(DateTimeOffset now)
    {
        foreach (var conversation in _conversations.Values.ToArray())
        {
            if (conversation.Status == "Waiting" && now - conversation.QueuedAt >= WaitLimit)
            {
                conversation.Status = "Expired";
                conversation.LastActivityAt = now;
            }
            else if (conversation.Status is "Closed" or "Expired" &&
                     now - conversation.LastActivityAt >= Retention)
            {
                _conversations.Remove(conversation.Id);
                _tokenIndexes.Remove(conversation.TokenIndex);
            }
        }
    }

    private void PromoteNextWaiting(DateTimeOffset now)
    {
        if (!_ownerOnline || _activeConversationId is not null)
        {
            return;
        }

        var next = _conversations.Values
            .Where(conversation => conversation.Status == "Waiting")
            .OrderBy(conversation => conversation.QueuedAt)
            .FirstOrDefault();
        if (next is null)
        {
            return;
        }

        next.Status = "Active";
        next.LastActivityAt = now;
        _activeConversationId = next.Id;
    }

    private static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private sealed class Conversation(
        Guid id,
        string visitorName,
        string tokenIndex,
        DateTimeOffset createdAt,
        List<ContactChatMessage> messages,
        string status)
    {
        public Guid Id { get; } = id;
        public string VisitorName { get; } = visitorName;
        public string TokenIndex { get; } = tokenIndex;
        public DateTimeOffset CreatedAt { get; } = createdAt;
        public DateTimeOffset QueuedAt { get; set; } = createdAt;
        public DateTimeOffset LastActivityAt { get; set; } = createdAt;
        public List<ContactChatMessage> Messages { get; } = messages;
        public string Status { get; set; } = status;
    }
}
