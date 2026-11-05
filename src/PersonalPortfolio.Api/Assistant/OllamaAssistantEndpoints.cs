namespace PersonalPortfolio.Api.Assistant;

public static class OllamaAssistantEndpoints
{
    public static void MapOllamaAssistantEndpoints(this WebApplication app)
    {
        app.MapPost("/api/assistant/chat", async (
            AssistantChatRequest? request,
            OllamaAssistantService assistant,
            CancellationToken cancellationToken) =>
        {
            var messages = request?.Messages;
            if (messages is null || messages.Count is < 1 or > 16 ||
                messages[^1].Role != "user" ||
                messages.Any(message =>
                    message.Role is not ("user" or "assistant") ||
                    string.IsNullOrWhiteSpace(message.Content) ||
                    message.Content.Length > 2000))
            {
                return Results.BadRequest(new { error = "Provide 1 to 16 valid chat messages; the latest message must be from the user." });
            }

            try
            {
                var reply = await assistant.ReplyAsync(messages, cancellationToken);
                return Results.Ok(new AssistantChatResponse(reply));
            }
            catch (HttpRequestException)
            {
                return Results.Problem(
                    title: "The Ollama assistant is unavailable.",
                    detail: "Check that Ollama is running and the configured model is installed.",
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        })
            .AllowAnonymous()
            .WithName("ChatWithOllamaAssistant")
            .WithSummary("Send a message to the portfolio assistant powered by Ollama")
            .Produces<AssistantChatResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
    }
}