using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using PersonalPortfolio.Api.Assistant;

namespace PersonalPortfolio.Api.Tests;

public sealed class GeminiAssistantServiceTests
{
    [Fact]
    public async Task SendsConversationWithApiKeyHeaderAndReturnsReply()
    {
        var handler = new StubHandler(HttpStatusCode.OK,
            "{\"candidates\":[{\"content\":{\"parts\":[{\"text\":\" Hello! \"}]}}]}");
        var service = CreateService(handler);

        var reply = await service.ReplyAsync(
            [new("user", "Hi"), new("assistant", "Hey"), new("user", "Skills?")],
            CancellationToken.None);

        Assert.Equal("Hello!", reply);
        Assert.Equal("test-key", handler.ApiKey);
        Assert.Contains("gemini-3.1-flash-lite:generateContent", handler.RequestUri);
        Assert.Contains("\"role\":\"model\"", handler.Body);
        Assert.Contains("systemInstruction", handler.Body);
    }

    [Fact]
    public async Task ThrowsWhenGeminiReturnsAnError()
    {
        var service = CreateService(new StubHandler(HttpStatusCode.TooManyRequests, "{}"));

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            service.ReplyAsync([new("user", "Hi")], CancellationToken.None));
    }

    [Fact]
    public async Task ThrowsWhenGeminiReturnsNoText()
    {
        var service = CreateService(new StubHandler(HttpStatusCode.OK, "{\"candidates\":[]}"));

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            service.ReplyAsync([new("user", "Hi")], CancellationToken.None));
    }

    private static GeminiAssistantService CreateService(StubHandler handler)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Gemini:ApiKey"] = "test-key" })
            .Build();
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://generativelanguage.googleapis.com") };
        return new GeminiAssistantService(client, configuration);
    }

    private sealed class StubHandler(HttpStatusCode status, string json) : HttpMessageHandler
    {
        public string? ApiKey { get; private set; }
        public string RequestUri { get; private set; } = "";
        public string Body { get; private set; } = "";

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            ApiKey = request.Headers.GetValues("x-goog-api-key").Single();
            RequestUri = request.RequestUri!.ToString();
            Body = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(status)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            };
        }
    }
}
