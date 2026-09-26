using System.Collections.Concurrent;

namespace PersonalPortfolio.Api.Admin;

public interface IAdminPresenceTracker
{
    bool IsOwnerOnline { get; }
    int ActiveConnectionCount { get; }
    void Connected(string connectionId);
    void Disconnected(string connectionId);
}

public sealed class AdminPresenceTracker : IAdminPresenceTracker
{
    private readonly ConcurrentDictionary<string, byte> _connections = new();

    public bool IsOwnerOnline => !_connections.IsEmpty;

    public int ActiveConnectionCount => _connections.Count;

    public void Connected(string connectionId)
    {
        _connections.TryAdd(connectionId, 0);
    }

    public void Disconnected(string connectionId)
    {
        _connections.TryRemove(connectionId, out _);
    }
}