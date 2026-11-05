using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace PersonalPortfolio.Api.Admin;

[Authorize(Policy = AdminAuthentication.AdminPolicy)]
public sealed class AdminPresenceHub(IAdminPresenceTracker presence) : Hub
{
    public override async Task OnConnectedAsync()
    {
        presence.Connected(Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        presence.Disconnected(Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}