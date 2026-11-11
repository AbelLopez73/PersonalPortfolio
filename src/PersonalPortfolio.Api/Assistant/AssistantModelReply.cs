namespace PersonalPortfolio.Api.Assistant;

public sealed record AssistantModelReply(
    string Reply,
    bool StartHumanChat,
    string? VisitorName);
