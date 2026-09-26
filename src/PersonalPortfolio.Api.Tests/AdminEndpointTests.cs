using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using PersonalPortfolio.Api.Admin;
using PersonalPortfolio.Api.Controllers;

namespace PersonalPortfolio.Api.Tests;

public sealed class AdminEndpointTests(
    WebApplicationFactory<WeatherForecastController> factory)
    : IClassFixture<WebApplicationFactory<WeatherForecastController>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task SessionReportsLoginDisabledAndOwnerOfflineWithoutCredentials()
    {
        var response = await _client.GetAsync("/api/admin/session");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var session = await response.Content.ReadFromJsonAsync<AdminSessionResponse>();
        Assert.NotNull(session);
        Assert.False(session.LoginEnabled);
        Assert.False(session.IsAdmin);
        Assert.False(session.IsConnected);
        Assert.Null(session.DisplayName);
    }

    [Fact]
    public async Task LoginReturnsServiceUnavailableUntilTelegramIsConfigured()
    {
        var response = await _client.GetAsync("/api/admin/login");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task LogoutRequiresAnAdminSession()
    {
        var response = await _client.PostAsync("/api/admin/logout", content: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AdminHubNegotiationRequiresAnAdminSession()
    {
        var response = await _client.PostAsync(
            "/hubs/admin/negotiate?negotiateVersion=1",
            content: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}