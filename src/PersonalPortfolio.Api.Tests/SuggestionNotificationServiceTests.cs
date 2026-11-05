using System.Net;
using System.Text;
using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using PersonalPortfolio.Api.Admin;
using PersonalPortfolio.Api.Notifications;

namespace PersonalPortfolio.Api.Tests;

public sealed class SuggestionNotificationServiceTests
{
    private const string PrivateTopic = "abcdefghijklmnopqrstuvwxyz0123456789";

    [Fact]
    public async Task OnlineOwnerGetsFullSilentTelegramMessageWithoutOfflinePush()
    {
        var telegram = new RecordingHandler(_ => JsonResponse("{\"ok\":true}"));
        var ntfy = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var service = CreateService(true, telegram, ntfy);

        var result = await service.NotifyOwnerAsync(
            new SuggestionNotification("Visitor", "visitor@example.com", "A full suggestion"),
            CancellationToken.None);

        Assert.True(result.OwnerWasOnline);
        Assert.True(result.TelegramDelivered);
        Assert.False(result.OfflinePushAttempted);
        Assert.Empty(ntfy.RequestBodies);
        Assert.Contains("A full suggestion", Assert.Single(telegram.RequestBodies));
        Assert.Contains("\"disable_notification\":true", telegram.RequestBodies[0]);
    }

    [Fact]
    public async Task OfflineOwnerGetsFullTelegramMessageAndOnlyRedactedShortPushPreview()
    {
        var telegram = new RecordingHandler(_ => JsonResponse("{\"ok\":true}"));
        var ntfy = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var service = CreateService(false, telegram, ntfy);
        var fullMessage = "abel@example.com can call +1 (863) 555-0199 about this project";

        var result = await service.NotifyOwnerAsync(
            new SuggestionNotification("Visitor", "visitor@example.com", fullMessage),
            CancellationToken.None);

        Assert.False(result.OwnerWasOnline);
        Assert.True(result.TelegramDelivered);
        Assert.True(result.OfflinePushAttempted);
        Assert.True(result.OfflinePushDelivered);
        using var telegramPayload = JsonDocument.Parse(telegram.RequestBodies[0]);
        Assert.Contains(fullMessage, telegramPayload.RootElement.GetProperty("text").GetString());

        var preview = Assert.Single(ntfy.RequestBodies);
        Assert.DoesNotContain("abel@example.com", preview);
        Assert.DoesNotContain("863", preview);
        Assert.InRange(new StringInfo(preview).LengthInTextElements, 0, 30);
        Assert.Equal($"https://ntfy.sh/{PrivateTopic}", ntfy.RequestUris[0]);
    }

    [Fact]
    public async Task NtfyFailureDoesNotChangeTelegramDeliveryResult()
    {
        var telegram = new RecordingHandler(_ => JsonResponse("{\"ok\":true}"));
        var ntfy = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        var service = CreateService(false, telegram, ntfy);

        var result = await service.NotifyOwnerAsync(
            new SuggestionNotification("Visitor", "visitor@example.com", "New suggestion"),
            CancellationToken.None);

        Assert.True(result.TelegramDelivered);
        Assert.True(result.OfflinePushAttempted);
        Assert.False(result.OfflinePushDelivered);
    }

    [Fact]
    public async Task TelegramFailureDoesNotPreventOfflinePushAttempt()
    {
        var telegram = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        var ntfy = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var service = CreateService(false, telegram, ntfy);

        var result = await service.NotifyOwnerAsync(
            new SuggestionNotification("Visitor", "visitor@example.com", "New suggestion"),
            CancellationToken.None);

        Assert.False(result.TelegramDelivered);
        Assert.True(result.OfflinePushAttempted);
        Assert.True(result.OfflinePushDelivered);
    }

    [Fact]
    public async Task WeakNtfyTopicDoesNotReceiveAnyNotification()
    {
        var telegram = new RecordingHandler(_ => JsonResponse("{\"ok\":true}"));
        var ntfy = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var service = CreateService(false, telegram, ntfy, "guessable");

        var result = await service.NotifyOwnerAsync(
            new SuggestionNotification("Visitor", "visitor@example.com", "New suggestion"),
            CancellationToken.None);

        Assert.True(result.TelegramDelivered);
        Assert.False(result.OfflinePushAttempted);
        Assert.Empty(ntfy.RequestBodies);
    }

    private static SuggestionNotificationService CreateService(
        bool ownerOnline,
        RecordingHandler telegram,
        RecordingHandler ntfy,
        string topic = PrivateTopic)
    {
        var settings = new Dictionary<string, string?>
        {
            ["TelegramNotifications:BotToken"] = "123456:server-only-token",
            ["TelegramNotifications:OwnerChatId"] = "123456789",
            ["Notifications:NtfyTopic"] = topic,
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();
        var presence = new AdminPresenceTracker();
        if (ownerOnline)
        {
            presence.Connected("owner-phone");
        }

        var factory = new RecordingHttpClientFactory(telegram, ntfy);
        return new SuggestionNotificationService(
            factory,
            configuration,
            presence,
            NullLogger<SuggestionNotificationService>.Instance);
    }

    private static HttpResponseMessage JsonResponse(string json) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json"),
    };

    private sealed class RecordingHttpClientFactory(
        RecordingHandler telegram,
        RecordingHandler ntfy) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(
            name switch
            {
                "TelegramBot" => telegram,
                "Ntfy" => ntfy,
                _ => throw new ArgumentOutOfRangeException(nameof(name), name, "Unknown test client."),
            },
            disposeHandler: false);
    }

    private sealed class RecordingHandler(
        Func<HttpRequestMessage, HttpResponseMessage> createResponse) : HttpMessageHandler
    {
        public List<string> RequestBodies { get; } = [];
        public List<string> RequestUris { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestUris.Add(request.RequestUri?.ToString() ?? string.Empty);
            RequestBodies.Add(request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken));
            return createResponse(request);
        }
    }
}
