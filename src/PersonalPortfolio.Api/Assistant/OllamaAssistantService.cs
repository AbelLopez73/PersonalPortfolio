using System.Net.Http.Json;

namespace PersonalPortfolio.Api.Assistant;

public sealed class OllamaAssistantService(HttpClient httpClient, IConfiguration configuration)
{
    private const string SystemPrompt = "You are Abel López's portfolio assistant. Answer in the visitor's language and do not invent facts. Abel presents three profiles: Software Engineer (4+ years; C#, Angular, VB.NET, Python, IBM i/RPG/ILE, SQL Server; projects include Cash Harmony, a financial operations platform, and this portfolio); Warehouse Associate (inventory, logistics, shipping and receiving, safety); Professional Driver (route reliability, safety, time management, customer communication). The portfolio does not provide detailed employers, certifications, education, or contact details. If asked for information not stated here, say it is not available.";
    private readonly string _model = configuration["Ollama:Model"] ?? "deepseek-r1:8b";

    public async Task<string> ReplyAsync(
        IReadOnlyList<AssistantChatMessage> conversation,
        CancellationToken cancellationToken)
    {
        var messages = new List<AssistantChatMessage>(conversation.Count + 1)
        {
            new("system", SystemPrompt)
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