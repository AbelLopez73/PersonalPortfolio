using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using PersonalPortfolio.Api.Assistant;

namespace PersonalPortfolio.Api.Tests;

public sealed class AssistantQuestionNotifierTests
{
    [Theory]
    [InlineData("Development", null, "[Development]")]
    [InlineData("Production", "Pruebas", "[Pruebas]")]
    [InlineData("Production", "Producción", "[Producción]")]
    public async Task IncludesConfiguredEnvironmentLabel(
        string environmentName,
        string? configuredLabel,
        string expectedLabel)
    {
        var handler = new RecordingHandler();
        var settings = new Dictionary<string, string?>
        {
            ["TelegramNotifications:BotToken"] = "123456:server-only-token",
            ["TelegramNotifications:OwnerChatId"] = "123456789",
            ["TelegramNotifications:EnvironmentLabel"] = configuredLabel,
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();
        var notifier = new AssistantQuestionNotifier(
            new RecordingHttpClientFactory(handler),
            configuration,
            new TestHostEnvironment(environmentName),
            NullLogger<AssistantQuestionNotifier>.Instance);
        var conversationId = Guid.NewGuid();

        await notifier.NotifyAsync(conversationId, "Test question", "Test answer", CancellationToken.None);

        using var payload = JsonDocument.Parse(Assert.Single(handler.RequestBodies));
        var text = payload.RootElement.GetProperty("text").GetString();
        Assert.StartsWith(expectedLabel, text);
        Assert.Contains(conversationId.ToString(), text);
        Assert.Contains("Test question", text);
        Assert.Contains("Test answer", text);
    }

    [Fact]
    public async Task SplitsLongAssistantRepliesWithoutDroppingConversationText()
    {
        var handler = new RecordingHandler();
        var notifier = CreateNotifier(handler);
        var conversationId = Guid.NewGuid();
        const string answer = "zzzzzz";
        var longAnswer = string.Concat(Enumerable.Repeat(answer, 1000));

        await notifier.NotifyAsync(conversationId, "Question", longAnswer, CancellationToken.None);

        Assert.Equal(2, handler.RequestBodies.Count);
        var texts = handler.RequestBodies.Select(body =>
        {
            using var payload = JsonDocument.Parse(body);
            return payload.RootElement.GetProperty("text").GetString()!;
        }).ToArray();
        Assert.All(texts, text => Assert.Contains(conversationId.ToString(), text));
        Assert.All(texts, text => Assert.InRange(text.Length, 0, 4096));
        Assert.Equal(longAnswer.Length, texts.Sum(text => text.Count(character => character == 'z')));
    }

    [Fact]
    public async Task CopiesVisitorAndOwnerMessagesWithWebsiteChatId()
    {
        var handler = new RecordingHandler();
        var notifier = CreateNotifier(handler);
        var chatId = Guid.NewGuid();

        await notifier.NotifyContactChatMessageAsync(chatId, "visitor", "I need help.", CancellationToken.None);
        await notifier.NotifyContactChatMessageAsync(chatId, "Abel", "I can help.", CancellationToken.None);

        Assert.Equal(2, handler.RequestBodies.Count);
        AssertTelegramTextContains(handler.RequestBodies[0], chatId, "visitor", "I need help.");
        AssertTelegramTextContains(handler.RequestBodies[1], chatId, "Abel", "I can help.");
    }

    [Fact]
    public async Task LinksWebsiteChatRequestToAssistantConversation()
    {
        var handler = new RecordingHandler();
        var notifier = CreateNotifier(handler);
        var chatId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();

        await notifier.NotifyContactRequestAsync(
            "Visitor",
            chatId,
            conversationId,
            "Visitor: Hello",
            CancellationToken.None);

        using var payload = JsonDocument.Parse(Assert.Single(handler.RequestBodies));
        var text = payload.RootElement.GetProperty("text").GetString();
        Assert.Contains(chatId.ToString(), text);
        Assert.Contains(conversationId.ToString(), text);
        Assert.Contains("Visitor: Hello", text);
    }

    private static void AssertTelegramTextContains(
        string requestBody,
        Guid chatId,
        string sender,
        string content)
    {
        using var payload = JsonDocument.Parse(requestBody);
        var text = payload.RootElement.GetProperty("text").GetString();
        Assert.Contains(chatId.ToString(), text);
        Assert.Contains(sender, text);
        Assert.Contains(content, text);
    }

    private static AssistantQuestionNotifier CreateNotifier(RecordingHandler handler)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["TelegramNotifications:BotToken"] = "123456:server-only-token",
                ["TelegramNotifications:OwnerChatId"] = "123456789",
            })
            .Build();
        return new AssistantQuestionNotifier(
            new RecordingHttpClientFactory(handler),
            configuration,
            new TestHostEnvironment("Test"),
            NullLogger<AssistantQuestionNotifier>.Instance);
    }

    private sealed class RecordingHttpClientFactory(RecordingHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public List<string> RequestBodies { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestBodies.Add(await request.Content!.ReadAsStringAsync(cancellationToken));
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"ok\":true}", Encoding.UTF8, "application/json"),
            };
        }
    }

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "PersonalPortfolio.Api.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
