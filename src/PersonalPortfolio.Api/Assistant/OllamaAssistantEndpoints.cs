namespace PersonalPortfolio.Api.Assistant;

public static class OllamaAssistantEndpoints
{
    private const int MaxUserMessageLength = 2000;
    private const int MaxAssistantMessageLength = 8000;

    public static void MapOllamaAssistantEndpoints(this WebApplication app)
    {
        app.MapPost("/api/assistant/chat", async (
            AssistantChatRequest? request,
            IAssistantService assistant,
            AssistantQuestionNotifier notifier,
            CancellationToken cancellationToken) =>
        {
            var messages = request?.Messages;
            if (messages is null || messages.Count is < 1 or > 16 ||
                messages[^1].Role != "user" ||
                messages.Any(message =>
                    message.Role is not ("user" or "assistant") ||
                    string.IsNullOrWhiteSpace(message.Content) ||
                    message.Content.Length > (message.Role == "user" ? MaxUserMessageLength : MaxAssistantMessageLength)))
            {
                return Results.BadRequest(new { error = "Provide 1 to 16 valid chat messages; the latest message must be from the user." });
            }

            try
            {
                await notifier.NotifyAsync(messages[^1].Content, cancellationToken);
                var reply = await assistant.ReplyAsync(messages, cancellationToken);
                return Results.Ok(new AssistantChatResponse(reply));
            }
            catch (HttpRequestException)
            {
                return Results.Problem(
                    title: "The assistant is unavailable.",
                    detail: "The configured AI provider (Gemini or Ollama) could not be reached.",
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        })
            .AllowAnonymous()
            .RequireRateLimiting("assistant")
            .WithName("ChatWithAssistant")
            .WithSummary("Send a message to the portfolio assistant (Gemini on Azure, Ollama locally)")
            .Produces<AssistantChatResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
    }
}