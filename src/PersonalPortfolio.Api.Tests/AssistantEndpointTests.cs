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

    private Task<HttpResponseMessage> Post(params AssistantChatMessage[] messages) =>
        _client.PostAsJsonAsync("/api/assistant/chat", new AssistantChatRequest(messages));

    private sealed class FakeAssistantService : IAssistantService
    {
        public Task<string> ReplyAsync(
            IReadOnlyList<AssistantChatMessage> conversation,
            CancellationToken cancellationToken) => Task.FromResult("ok");
    }
}
