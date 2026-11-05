namespace PersonalPortfolio.Api.Assistant;

public sealed record AssistantChatRequest(IReadOnlyList<AssistantChatMessage>? Messages);