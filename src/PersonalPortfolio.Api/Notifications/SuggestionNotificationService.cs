using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.SignalR;
using PersonalPortfolio.Api.Admin;

namespace PersonalPortfolio.Api.Notifications;

public sealed partial class SuggestionNotificationService(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    IAdminPresenceTracker presence,
    ILogger<SuggestionNotificationService> logger) : ISuggestionNotificationService
{
    private const int PreviewLength = 30;
    private static readonly TimeSpan TopicTimeout = TimeSpan.FromSeconds(2);

    public async Task<SuggestionDeliveryResult> NotifyOwnerAsync(
        SuggestionNotification suggestion,
        CancellationToken cancellationToken)
    {
        var ownerWasOnline = presence.IsOwnerOnline;
        var telegramDelivered = await SendTelegramAsync(
            suggestion,
            ownerWasOnline,
            cancellationToken);

        if (ownerWasOnline)
        {
            return new SuggestionDeliveryResult(true, telegramDelivered, false, false);
        }

        var topic = configuration["Notifications:NtfyTopic"];
        if (!IsPrivateTopicConfigured(topic))
        {
            return new SuggestionDeliveryResult(false, telegramDelivered, false, false);
        }

        var offlinePushDelivered = await SendOfflinePushAsync(
            topic!,
            suggestion.Message,
            cancellationToken);

        return new SuggestionDeliveryResult(false, telegramDelivered, true, offlinePushDelivered);
    }

    private async Task<bool> SendTelegramAsync(
        SuggestionNotification suggestion,
        bool ownerWasOnline,
        CancellationToken cancellationToken)
    {
        var token = configuration["TelegramNotifications:BotToken"];
        var ownerChatId = configuration["TelegramNotifications:OwnerChatId"];
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(ownerChatId))
        {
            return false;
        }

        var message = $"New portfolio suggestion\nName: {suggestion.Name}\nEmail: {suggestion.Email}\n\n{suggestion.Message}";
        var request = new TelegramMessageRequest(ownerChatId, message, ownerWasOnline);

        try
        {
            using var response = await httpClientFactory
                .CreateClient("TelegramBot")
                .PostAsJsonAsync(
                    $"https://api.telegram.org/bot{Uri.EscapeDataString(token)}/sendMessage",
                    request,
                    cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Telegram rejected a suggestion notification with status {StatusCode}.",
                    (int)response.StatusCode);
                return false;
            }

            var result = await response.Content.ReadFromJsonAsync<TelegramMessageResponse>(
                cancellationToken: cancellationToken);
            if (result?.Ok != true)
            {
                logger.LogWarning("Telegram returned an unsuccessful suggestion notification response.");
            }

            return result?.Ok == true;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Telegram suggestion notification timed out.");
            return false;
        }
        catch (HttpRequestException)
        {
            logger.LogWarning("Telegram suggestion notification could not reach the provider.");
            return false;
        }
    }

    private async Task<bool> SendOfflinePushAsync(
        string topic,
        string message,
        CancellationToken cancellationToken)
    {
        var preview = CreatePreview(message);
        using var request = new HttpRequestMessage(HttpMethod.Post, $"https://ntfy.sh/{topic}")
        {
            Content = new StringContent(preview, Encoding.UTF8, "text/plain"),
        };
        request.Headers.TryAddWithoutValidation("Title", "New portfolio suggestion");
        request.Headers.TryAddWithoutValidation("Priority", "3");

        var publicSiteUrl = configuration["Notifications:PublicSiteUrl"];
        if (Uri.TryCreate(publicSiteUrl, UriKind.Absolute, out var siteUri) &&
            siteUri.Scheme == Uri.UriSchemeHttps)
        {
            request.Headers.TryAddWithoutValidation("Click", siteUri.ToString());
        }

        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TopicTimeout);
            using var response = await httpClientFactory
                .CreateClient("Ntfy")
                .SendAsync(request, timeout.Token);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("ntfy rejected an offline suggestion preview with status {StatusCode}.",
                    (int)response.StatusCode);
            }

            return response.IsSuccessStatusCode;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("ntfy offline suggestion preview timed out.");
            return false;
        }
        catch (HttpRequestException)
        {
            logger.LogWarning("ntfy offline suggestion preview could not reach the provider.");
            return false;
        }
    }

    private static bool IsPrivateTopicConfigured(string? topic) =>
        topic is { Length: >= 32 and <= 64 } && TopicPattern().IsMatch(topic);

    private static string CreatePreview(string message)
    {
        var redacted = EmailPattern().Replace(message, "[email]");
        redacted = PhonePattern().Replace(redacted, "[phone]");

        var elements = StringInfo.GetTextElementEnumerator(redacted);
        var preview = new StringBuilder();
        var count = 0;
        while (count < PreviewLength && elements.MoveNext())
        {
            preview.Append(elements.GetTextElement());
            count++;
        }

        return preview.ToString();
    }

    [GeneratedRegex("^[A-Za-z0-9_-]{32,64}$", RegexOptions.CultureInvariant)]
    private static partial Regex TopicPattern();

    [GeneratedRegex(@"[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex EmailPattern();

    [GeneratedRegex(@"\+?\d[\d\s().-]{6,}\d", RegexOptions.CultureInvariant)]
    private static partial Regex PhonePattern();

    private sealed record TelegramMessageRequest(
        [property: JsonPropertyName("chat_id")] string ChatId,
        [property: JsonPropertyName("text")] string Text,
        [property: JsonPropertyName("disable_notification")] bool DisableNotification);

    private sealed record TelegramMessageResponse(
        [property: JsonPropertyName("ok")] bool Ok);
}