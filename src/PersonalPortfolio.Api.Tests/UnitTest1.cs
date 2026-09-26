using PersonalPortfolio.Api.Admin;

namespace PersonalPortfolio.Api.Tests;

public sealed class AdminPresenceTrackerTests
{
    [Fact]
    public void OwnerRemainsOnlineUntilEveryConnectionDisconnects()
    {
        var tracker = new AdminPresenceTracker();

        tracker.Connected("phone");
        tracker.Connected("laptop");
        tracker.Disconnected("phone");

        Assert.True(tracker.IsOwnerOnline);
        Assert.Equal(1, tracker.ActiveConnectionCount);

        tracker.Disconnected("laptop");

        Assert.False(tracker.IsOwnerOnline);
        Assert.Equal(0, tracker.ActiveConnectionCount);
    }
}