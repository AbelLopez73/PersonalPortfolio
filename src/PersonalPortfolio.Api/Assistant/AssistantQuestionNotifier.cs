using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace PersonalPortfolio.Api.Assistant;

public sealed class AssistantQuestionNotifier(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    IHostEnvironment hostEnvironment,
    ILogger<AssistantQuestionNotifier> logger)
{
    private const int MaxQuestionLength = 1000;
    private const int MaxAnswerLength = 2500;

    /// <summary>Best-effort Telegram copy of a question and its answer; it never throws so chat replies are not affected.</summary>
    public async Task NotifyAsync(string question, string? answer, CancellationToken cancellationToken)
    {
        var token = configuration["TelegramNotifications:BotToken"];
        var chatId = configuration["TelegramNotifications:OwnerChatId"];
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(chatId))
        {
            return;
        }

        var environmentLabel = configuration["TelegramNotifications:EnvironmentLabel"]
            ?? hostEnvironment.EnvironmentName;
        var answerText = answer is null ? "(assistant unavailable)" : Truncate(answer, MaxAnswerLength);
        var text = $"[{environmentLabel}]\nQuestion:\n{Truncate(question, MaxQuestionLength)}\n\nAnswer:\n{answerText}";
        try
        {
            using var response = await httpClientFactory
                .CreateClient("TelegramBot")
                .PostAsJsonAsync(
                    $"https://api.telegram.org/bot{Uri.EscapeDataString(token)}/sendMessage",
                    new TelegramMessage(chatId, text),
                    cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Telegram rejected an assistant question alert with status {StatusCode}.",
                    (int)response.StatusCode);
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException)
        {
            logger.LogWarning("Telegram assistant question alert could not be delivered.");
        }
    }

    public async Task NotifyContactRequestAsync(
        string visitorName,
        Guid conversationId,
        string conversationSummary,
        CancellationToken cancellationToken)
    {
        var token = configuration["TelegramNotifications:BotToken"];
        var chatId = configuration["TelegramNotifications:OwnerChatId"];
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(chatId))
        {
            return;
        }

        var environmentLabel = configuration["TelegramNotifications:EnvironmentLabel"]
            ?? hostEnvironment.EnvironmentName;
        var text = $"[{environmentLabel}]\nNew website chat request\nName: {Truncate(visitorName, 100)}\nChat ID: {conversationId}\n\nContext:\n{Truncate(conversationSummary, MaxQuestionLength)}";
        try
        {
            using var response = await httpClientFactory
                .CreateClient("TelegramBot")
                .PostAsJsonAsync(
                    $"https://api.telegram.org/bot{Uri.EscapeDataString(token)}/sendMessage",
                    new TelegramMessage(chatId, text),
                    cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Telegram rejected a website chat request alert with status {StatusCode}.",
                    (int)response.StatusCode);
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Telegram website chat request alert timed out.");
        }
        catch (HttpRequestException)
        {
            logger.LogWarning("Telegram website chat request alert could not reach the provider.");
        }
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length > maxLength ? value[..maxLength] : value;

    private sealed record TelegramMessage(
        [property: JsonPropertyName("chat_id")] string ChatId,
        [property: JsonPropertyName("text")] string Text);
}
