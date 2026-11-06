using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace PersonalPortfolio.Api.Assistant;

public sealed class GeminiAssistantService(HttpClient httpClient, IConfiguration configuration) : IAssistantService
{
    private readonly string _model = configuration["Gemini:Model"] ?? "gemini-3.1-flash-lite";

    public async Task<string> ReplyAsync(
        IReadOnlyList<AssistantChatMessage> conversation,
        CancellationToken cancellationToken)
    {
        var request = new GeminiRequest(
            new GeminiContent(null, [new GeminiPart(AssistantPrompt.System)]),
            conversation
                .Select(message => new GeminiContent(
                    message.Role == "assistant" ? "model" : "user",
                    [new GeminiPart(message.Content)]))
                .ToList());

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"/v1beta/models/{Uri.EscapeDataString(_model)}:generateContent")
        {
            Content = JsonContent.Create(request),
        };
        httpRequest.Headers.Add("x-goog-api-key", configuration["Gemini:ApiKey"]);

        using var response = await httpClient.SendAsync(httpRequest, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<GeminiResponse>(
            cancellationToken: cancellationToken);
        var reply = result?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text?.Trim();

        if (string.IsNullOrWhiteSpace(reply))
        {
            throw new HttpRequestException("Gemini returned an empty response.");
        }

        return reply;
    }

    private sealed record GeminiRequest(
        [property: JsonPropertyName("systemInstruction")] GeminiContent SystemInstruction,
        [property: JsonPropertyName("contents")] List<GeminiContent> Contents);

    private sealed record GeminiContent(
        [property: JsonPropertyName("role"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Role,
        [property: JsonPropertyName("parts")] List<GeminiPart>? Parts);

    private sealed record GeminiPart(
        [property: JsonPropertyName("text")] string? Text);

    private sealed record GeminiResponse(
        [property: JsonPropertyName("candidates")] List<GeminiCandidate>? Candidates);

    private sealed record GeminiCandidate(
        [property: JsonPropertyName("content")] GeminiContent? Content);
}
