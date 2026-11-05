namespace PersonalPortfolio.Api.Assistant;

public interface IAssistantService
{
    Task<string> ReplyAsync(
        IReadOnlyList<AssistantChatMessage> conversation,
        CancellationToken cancellationToken);
}
