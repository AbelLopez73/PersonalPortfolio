using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using PersonalPortfolio.Api.Assistant;

namespace PersonalPortfolio.Api.Admin;

[Authorize(Policy = AdminAuthentication.AdminPolicy)]
public sealed class AdminPresenceHub(
    IAdminPresenceTracker presence,
    IContactChatStore contactChats) : Hub
{
    public override async Task OnConnectedAsync()
    {
        presence.Connected(Context.ConnectionId);
        contactChats.SetOwnerOnline(presence.IsOwnerOnline);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        presence.Disconnected(Context.ConnectionId);
        contactChats.SetOwnerOnline(presence.IsOwnerOnline);
        await base.OnDisconnectedAsync(exception);
    }
}