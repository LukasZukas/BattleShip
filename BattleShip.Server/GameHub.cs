using System.Collections.Concurrent;
using BattleShip.Shared;
using Microsoft.AspNetCore.SignalR;

namespace BattleShip.Server;

public class GameHub : Hub
{
    // Connection ids waiting for an opponent.
    private static readonly ConcurrentQueue<string> WaitingQueue = new();
    private static readonly HashSet<string> WaitingConnectionIds = [];
    private static readonly HashSet<string> ConnectedConnectionIds = [];

    // Active sessions, looked up by either player's connection id.
    private static readonly ConcurrentDictionary<string, GameSession> SessionsByConnection = new();

    private static readonly object MatchmakingLock = new();

    public override async Task OnConnectedAsync()
    {
        lock (MatchmakingLock)
        {
            ConnectedConnectionIds.Add(Context.ConnectionId);
        }

        await base.OnConnectedAsync();
    }

    public async Task FindMatch()
    {
        string? opponentId = null;
        GameSession? session = null;

        lock (MatchmakingLock)
        {
            if (SessionsByConnection.ContainsKey(Context.ConnectionId))
            {
                return;
            }

            while (WaitingQueue.TryDequeue(out var waiting))
            {
                if (!WaitingConnectionIds.Remove(waiting) ||
                    !ConnectedConnectionIds.Contains(waiting) ||
                    waiting == Context.ConnectionId ||
                    SessionsByConnection.ContainsKey(waiting))
                {
                    continue;
                }

                opponentId = waiting;
                break;
            }

            if (opponentId is null)
            {
                if (WaitingConnectionIds.Add(Context.ConnectionId))
                {
                    WaitingQueue.Enqueue(Context.ConnectionId);
                }
            }
            else
            {
                session = new GameSession(opponentId, Context.ConnectionId);
                SessionsByConnection[opponentId] = session;
                SessionsByConnection[Context.ConnectionId] = session;
            }
        }

        if (opponentId is null || session is null)
        {
            return; // waiting in queue
        }

        await Groups.AddToGroupAsync(opponentId, session.SessionId);
        await Groups.AddToGroupAsync(Context.ConnectionId, session.SessionId);

        await Clients.Client(opponentId)
            .SendAsync("MatchFound", new MatchFoundMessage(session.SessionId, YouGoFirst: true));

        await Clients.Client(Context.ConnectionId)
            .SendAsync("MatchFound", new MatchFoundMessage(session.SessionId, YouGoFirst: false));
    }

    public async Task FireShot(FireShotRequest request)
    {
        if (!SessionsByConnection.TryGetValue(Context.ConnectionId, out var session)) return;

        bool success = session.TryFireShot(Context.ConnectionId, request.X, request.Y);
        if (!success) return; // Invalid move

        var opponentId = session.OpponentOf(Context.ConnectionId);

        await Clients.Client(Context.ConnectionId)
            .SendAsync("ShotResult", new ShotResultMessage(Context.ConnectionId, request.X, request.Y, IsYourTurnNext: false));

        await Clients.Client(opponentId)
            .SendAsync("ShotResult", new ShotResultMessage(Context.ConnectionId, request.X, request.Y, IsYourTurnNext: true));
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        GameSession? session = null;
        string? opponentId = null;

        lock (MatchmakingLock)
        {
            ConnectedConnectionIds.Remove(Context.ConnectionId);
            WaitingConnectionIds.Remove(Context.ConnectionId);

            if (SessionsByConnection.TryRemove(Context.ConnectionId, out session))
            {
                opponentId = session.OpponentOf(Context.ConnectionId);
                SessionsByConnection.TryRemove(opponentId, out _);
            }
        }

        if (session is not null && opponentId is not null)
        {
            await Clients.Client(opponentId)
                .SendAsync("OpponentDisconnected", new OpponentDisconnectedMessage(session.SessionId));
        }

        await base.OnDisconnectedAsync(exception);
    }
}
