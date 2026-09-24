using System.Collections.Concurrent;

namespace BattleShip.Server;

public sealed class SessionManager
{
    private static readonly SessionManager _instance = new SessionManager();

    private readonly ConcurrentQueue<string> WaitingQueue = new();
    private readonly ConcurrentDictionary<string, GameSession> SessionsByConnection = new();
    private readonly HashSet<string> WaitingConnectionIds = [];
    private readonly HashSet<string> ConnectedConnectionIds = [];
    private readonly object _matchmakingLock = new();

    private SessionManager()
    {
    }

    public static SessionManager Instance => _instance;

    public void RegisterConnection(string connectionId)
    {
        lock (_matchmakingLock)
        {
            ConnectedConnectionIds.Add(connectionId);
        }
    }

    public GameSession? TryMatch(string connectionId)
    {
        string? opponentId = null;

        lock (_matchmakingLock)
        {
            if (SessionsByConnection.ContainsKey(connectionId))
            {
                return null;
            }

            while (WaitingQueue.TryDequeue(out var waiting))
            {
                if (!WaitingConnectionIds.Remove(waiting) ||
                    !ConnectedConnectionIds.Contains(waiting) ||
                    waiting == connectionId ||
                    SessionsByConnection.ContainsKey(waiting))
                {
                    continue;
                }

                opponentId = waiting;
                break;
            }

            if (opponentId is null)
            {
                if (WaitingConnectionIds.Add(connectionId))
                {
                    WaitingQueue.Enqueue(connectionId);
                }

                return null;
            }

            var session = new GameSession(opponentId, connectionId);
            SessionsByConnection[opponentId] = session;
            SessionsByConnection[connectionId] = session;
            return session;
        }
    }

    public bool TryGetSession(string connectionId, out GameSession? session) =>
        SessionsByConnection.TryGetValue(connectionId, out session);

    public bool TryRemoveSession(string connectionId, out GameSession? session)
    {
        lock (_matchmakingLock)
        {
            ConnectedConnectionIds.Remove(connectionId);
            WaitingConnectionIds.Remove(connectionId);

            if (!SessionsByConnection.TryRemove(connectionId, out session))
            {
                return false;
            }

            SessionsByConnection.TryRemove(session.OpponentOf(connectionId), out _);
            return true;
        }
    }
}
