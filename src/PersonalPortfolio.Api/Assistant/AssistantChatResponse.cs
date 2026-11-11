namespace PersonalPortfolio.Api.Assistant;

/// <summary>Reply from the portfolio assistant and the status of an optional human-chat handoff.</summary>
public sealed record AssistantChatResponse(
    string Reply,
    bool ContactChatStarted,
    Guid? ContactChatId,
    Guid ConversationId);