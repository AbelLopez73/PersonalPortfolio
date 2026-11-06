using System.Net.Http.Json;

namespace PersonalPortfolio.Api.Assistant;

public sealed class OllamaAssistantService(HttpClient httpClient, IConfiguration configuration) : IAssistantService
{
    private readonly string _model = configuration["Ollama:Model"] ?? "deepseek-r1:8b";

    public async Task<string> ReplyAsync(
        IReadOnlyList<AssistantChatMessage> conversation,
        CancellationToken cancellationToken)
    {
        var messages = new List<AssistantChatMessage>(conversation.Count + 1)
        {
            new("system", AssistantPrompt.System)
        };
        messages.AddRange(conversation);

        using var response = await httpClient.PostAsJsonAsync(
            "/api/chat",
            new { model = _model, messages, stream = false },
            cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<OllamaChatResponse>(
            cancellationToken: cancellationToken);
        var reply = result?.Message?.Content?.Trim();

        if (string.IsNullOrWhiteSpace(reply))
        {
            throw new HttpRequestException("Ollama returned an empty response.");
        }

        return reply;
    }

    private sealed record OllamaChatResponse(OllamaMessage? Message);

    private sealed record OllamaMessage(string? Content);
}
