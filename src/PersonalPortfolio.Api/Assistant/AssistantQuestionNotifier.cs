using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace PersonalPortfolio.Api.Assistant;

public sealed class AssistantQuestionNotifier(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    IHostEnvironment hostEnvironment,
    ILogger<AssistantQuestionNotifier> logger)
{
    private const int MaxTelegramChunkLength = 3500;

    /// <summary>Copies an assistant conversation turn to the owner's private Telegram chat.</summary>
    public async Task NotifyAsync(
        Guid conversationId,
        string question,
        string? answer,
        CancellationToken cancellationToken)
    {
        var environmentLabel = configuration["TelegramNotifications:EnvironmentLabel"]
            ?? hostEnvironment.EnvironmentName;
        var body = $"Visitor:\n{question}\n\nAssistant:\n{answer ?? "(assistant unavailable)"}";
        var messages = CreateTelegramMessages(
            $"[{environmentLabel}]\nAssistant conversation {conversationId}",
            body);
        await SendTelegramMessagesAsync(messages, "assistant conversation", cancellationToken);
    }

    public async Task NotifyContactRequestAsync(
        string visitorName,
        Guid chatId,
        Guid assistantConversationId,
        string conversationSummary,
        CancellationToken cancellationToken)
    {
        var environmentLabel = configuration["TelegramNotifications:EnvironmentLabel"]
            ?? hostEnvironment.EnvironmentName;
        var body = $"New website chat request\nName: {Truncate(visitorName, 100)}\nChat ID: {chatId}\nAssistant conversation: {assistantConversationId}\n\nContext:\n{conversationSummary}";
        await SendTelegramMessagesAsync(
            CreateTelegramMessages($"[{environmentLabel}]", body),
            "website chat request",
            cancellationToken);
    }

    public async Task NotifyContactChatMessageAsync(
        Guid chatId,
        string sender,
        string content,
        CancellationToken cancellationToken)
    {
        var environmentLabel = configuration["TelegramNotifications:EnvironmentLabel"]
            ?? hostEnvironment.EnvironmentName;
        var body = $"Website chat {chatId}\n{sender}:\n{content}";
        await SendTelegramMessagesAsync(
            CreateTelegramMessages($"[{environmentLabel}]", body),
            "website chat message",
            cancellationToken);
    }

    private async Task SendTelegramMessagesAsync(
        IReadOnlyList<string> messages,
        string notificationType,
        CancellationToken cancellationToken)
    {
        var token = configuration["TelegramNotifications:BotToken"];
        var ownerChatId = configuration["TelegramNotifications:OwnerChatId"];
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(ownerChatId))
        {
            return;
        }

        var client = httpClientFactory.CreateClient("TelegramBot");
        foreach (var text in messages)
        {
            try
            {
                using var response = await client.PostAsJsonAsync(
                    $"https://api.telegram.org/bot{Uri.EscapeDataString(token)}/sendMessage",
                    new TelegramMessage(ownerChatId, text),
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    logger.LogWarning("Telegram rejected a {NotificationType} copy with status {StatusCode}.",
                        notificationType,
                        (int)response.StatusCode);
                }
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning("Telegram {NotificationType} copy timed out.", notificationType);
            }
            catch (OperationCanceledException)
            {
                logger.LogWarning("Telegram {NotificationType} copy was cancelled.", notificationType);
            }
            catch (HttpRequestException)
            {
                logger.LogWarning("Telegram {NotificationType} copy could not reach the provider.", notificationType);
            }
        }
    }

    private static IReadOnlyList<string> CreateTelegramMessages(string header, string body)
    {
        var chunks = SplitIntoChunks(body);
        return chunks
            .Select((chunk, index) =>
                $"{header}{(chunks.Count > 1 ? $"\nPart {index + 1}/{chunks.Count}" : string.Empty)}\n{chunk}")
            .ToArray();
    }

    private static IReadOnlyList<string> SplitIntoChunks(string text)
    {
        var chunks = new List<string>();
        for (var start = 0; start < text.Length;)
        {
            var end = Math.Min(start + MaxTelegramChunkLength, text.Length);
            if (end < text.Length && char.IsHighSurrogate(text[end - 1]))
            {
                end--;
            }

            chunks.Add(text[start..end]);
            start = end;
        }

        return chunks;
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length > maxLength ? value[..maxLength] : value;

    private sealed record TelegramMessage(
        [property: JsonPropertyName("chat_id")] string ChatId,
        [property: JsonPropertyName("text")] string Text);
}
