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

        await notifier.NotifyAsync("Test question", "Test answer", CancellationToken.None);

        using var payload = JsonDocument.Parse(handler.RequestBody);
        var text = payload.RootElement.GetProperty("text").GetString();
        Assert.StartsWith(expectedLabel, text);
        Assert.Contains("Test question", text);
        Assert.Contains("Test answer", text);
    }

    private sealed class RecordingHttpClientFactory(RecordingHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public string RequestBody { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestBody = await request.Content!.ReadAsStringAsync(cancellationToken);
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
