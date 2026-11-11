using System.Net.Http.Json;
using System.Text.Json;

namespace PersonalPortfolio.Api.Assistant;

public sealed class OllamaAssistantService(HttpClient httpClient, IConfiguration configuration) : IAssistantService
{
    private readonly string _model = configuration["Ollama:Model"] ?? "deepseek-r1:8b";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<AssistantModelReply> ReplyAsync(
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
            new { model = _model, messages, stream = false, format = "json" },
            cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<OllamaChatResponse>(
            cancellationToken: cancellationToken);
        var json = result?.Message?.Content?.Trim();

        if (string.IsNullOrWhiteSpace(json))
        {
            throw new HttpRequestException("Ollama returned an empty response.");
        }

        try
        {
            var reply = JsonSerializer.Deserialize<AssistantModelReply>(json, JsonOptions);
            if (reply is null || string.IsNullOrWhiteSpace(reply.Reply))
            {
                throw new JsonException("Ollama returned an incomplete assistant response.");
            }

            return reply with { Reply = reply.Reply.Trim() };
        }
        catch (JsonException exception)
        {
            throw new HttpRequestException("Ollama returned an invalid assistant response.", exception);
        }
    }

    private sealed record OllamaChatResponse(OllamaMessage? Message);

    private sealed record OllamaMessage(string? Content);
}
