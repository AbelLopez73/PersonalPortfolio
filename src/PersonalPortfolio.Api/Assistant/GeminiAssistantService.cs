using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PersonalPortfolio.Api.Assistant;

public sealed class GeminiAssistantService(HttpClient httpClient, IConfiguration configuration) : IAssistantService
{
    private readonly string _model = configuration["Gemini:Model"] ?? "gemini-3.1-flash-lite";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<AssistantModelReply> ReplyAsync(
        IReadOnlyList<AssistantChatMessage> conversation,
        CancellationToken cancellationToken)
    {
        var request = new GeminiRequest(
            new GeminiContent(null, [new GeminiPart(AssistantPrompt.System)]),
            conversation
                .Select(message => new GeminiContent(
                    message.Role == "assistant" ? "model" : "user",
                    [new GeminiPart(message.Content)]))
                .ToList(),
            new GeminiGenerationConfig("application/json"));

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
        var json = result?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text?.Trim();

        if (string.IsNullOrWhiteSpace(json))
        {
            throw new HttpRequestException("Gemini returned an empty response.");
        }

        try
        {
            var reply = JsonSerializer.Deserialize<AssistantModelReply>(json, JsonOptions);
            if (reply is null || string.IsNullOrWhiteSpace(reply.Reply))
            {
                throw new JsonException("Gemini returned an incomplete assistant response.");
            }

            return reply with { Reply = reply.Reply.Trim() };
        }
        catch (JsonException exception)
        {
            throw new HttpRequestException("Gemini returned an invalid assistant response.", exception);
        }
    }

    private sealed record GeminiRequest(
        [property: JsonPropertyName("systemInstruction")] GeminiContent SystemInstruction,
        [property: JsonPropertyName("contents")] List<GeminiContent> Contents,
        [property: JsonPropertyName("generationConfig")] GeminiGenerationConfig GenerationConfig);

    private sealed record GeminiGenerationConfig(
        [property: JsonPropertyName("responseMimeType")] string ResponseMimeType);

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
