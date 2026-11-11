using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using PersonalPortfolio.Api.Assistant;
using PersonalPortfolio.Api.Controllers;

namespace PersonalPortfolio.Api.Tests;

public sealed class AssistantEndpointTests(
    WebApplicationFactory<WeatherForecastController> factory)
    : IClassFixture<WebApplicationFactory<WeatherForecastController>>
{
    private readonly HttpClient _client = factory
        .WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.AddSingleton<IAssistantService, FakeAssistantService>()))
        .CreateClient();

    [Fact]
    public async Task FollowUpAcceptsLongAssistantReplies()
    {
        var response = await Post(
            new AssistantChatMessage("user", "Tell me about Abel"),
            new AssistantChatMessage("assistant", new string('a', 3000)),
            new AssistantChatMessage("user", "And his C# experience?"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task RejectsLongUserMessages()
    {
        var response = await Post(new AssistantChatMessage("user", new string('a', 2001)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task StartsWebsiteChatOnlyAfterNameAndExplicitConsent()
    {
        var response = await Post(
            new AssistantChatMessage("user", "My name is Avery"),
            new AssistantChatMessage("assistant", "Do you agree to share your name and conversation with Abel?"),
            new AssistantChatMessage("user", "Yes, I agree"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<AssistantChatResponse>();
        Assert.NotNull(result);
        Assert.True(result.ContactChatStarted);
        Assert.NotNull(result.ContactChatId);

        var cookie = Assert.Single(response.Headers.GetValues("Set-Cookie"));
        Assert.Contains("HttpOnly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(ContactChatEndpoints.VisitorCookieName, cookie);
    }

    [Fact]
    public async Task DoesNotStartWebsiteChatWithoutExplicitConsent()
    {
        var response = await Post(
            new AssistantChatMessage("user", "My name is Avery"),
            new AssistantChatMessage("assistant", "Do you agree to share your name and conversation with Abel?"),
            new AssistantChatMessage("user", "Maybe later"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<AssistantChatResponse>();
        Assert.NotNull(result);
        Assert.False(result.ContactChatStarted);
        Assert.False(response.Headers.Contains("Set-Cookie"));
    }

    private Task<HttpResponseMessage> Post(params AssistantChatMessage[] messages) =>
        _client.PostAsJsonAsync("/api/assistant/chat", new AssistantChatRequest(messages));

    private sealed class FakeAssistantService : IAssistantService
    {
        public Task<AssistantModelReply> ReplyAsync(
            IReadOnlyList<AssistantChatMessage> conversation,
            CancellationToken cancellationToken)
        {
            var startChat = conversation[^1].Content.StartsWith("yes", StringComparison.OrdinalIgnoreCase);
            return Task.FromResult(new AssistantModelReply(
                "ok",
                startChat,
                startChat ? "Avery" : null));
        }
    }
}
