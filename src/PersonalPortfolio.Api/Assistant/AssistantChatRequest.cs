namespace PersonalPortfolio.Api.Assistant;

/// <summary>Visitor message history and its stable identifier for Telegram conversation copies.</summary>
public sealed record AssistantChatRequest(
    IReadOnlyList<AssistantChatMessage>? Messages,
    Guid? ConversationId = null);