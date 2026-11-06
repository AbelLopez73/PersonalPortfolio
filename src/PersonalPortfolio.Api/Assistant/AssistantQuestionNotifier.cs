using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace PersonalPortfolio.Api.Assistant;

public sealed class AssistantQuestionNotifier(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<AssistantQuestionNotifier> logger)
{
    private const int MaxLength = 500;

    /// <summary>Best-effort Telegram alert; it never throws so chat replies are not affected.</summary>
    public async Task NotifyAsync(string question, CancellationToken cancellationToken)
    {
        var token = configuration["TelegramNotifications:BotToken"];
        var chatId = configuration["TelegramNotifications:OwnerChatId"];
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(chatId))
        {
            return;
        }

        var text = question.Length > MaxLength ? question[..MaxLength] : question;
        try
        {
            using var response = await httpClientFactory
                .CreateClient("TelegramBot")
                .PostAsJsonAsync(
                    $"https://api.telegram.org/bot{Uri.EscapeDataString(token)}/sendMessage",
                    new TelegramMessage(chatId, $"New assistant question:\n\n{text}"),
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

    private sealed record TelegramMessage(
        [property: JsonPropertyName("chat_id")] string ChatId,
        [property: JsonPropertyName("text")] string Text);
}
