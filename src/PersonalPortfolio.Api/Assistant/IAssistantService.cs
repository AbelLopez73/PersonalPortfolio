namespace PersonalPortfolio.Api.Assistant;

public interface IAssistantService
{
    Task<AssistantModelReply> ReplyAsync(
        IReadOnlyList<AssistantChatMessage> conversation,
        CancellationToken cancellationToken);
}
