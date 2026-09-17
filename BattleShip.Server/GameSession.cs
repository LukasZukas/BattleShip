using BattleShip.Shared;

namespace BattleShip.Server;

public class GameSession
{
    public string SessionId { get; } = Guid.NewGuid().ToString();
    public string PlayerAConnectionId { get; }
    public string PlayerBConnectionId { get; }

    public GridModel GridA { get; } = new();
    public GridModel GridB { get; } = new();

    public string CurrentTurnConnectionId { get; private set; }

    private readonly object _lock = new();

    public GameSession(string playerA, string playerB)
    {
        PlayerAConnectionId = playerA;
        PlayerBConnectionId = playerB;
        CurrentTurnConnectionId = playerA; // player A always go first
    }

    public string OpponentOf(string connectionId) =>
        connectionId == PlayerAConnectionId ? PlayerBConnectionId : PlayerAConnectionId;

    private GridModel GridOf(string connectionId) =>
        connectionId == PlayerAConnectionId ? GridA : GridB;
    
    public bool TryFireShot(string shooterConnectionId, int x, int y)
    {
        lock (_lock)
        {
            if (shooterConnectionId != CurrentTurnConnectionId) return false;
            if (x < 0 || x >= GridModel.Size || y < 0 || y >= GridModel.Size) return false;

            var defenderId = OpponentOf(shooterConnectionId);
            var defenderGrid = GridOf(defenderId);

            if (!defenderGrid.TryMarkFired(x, y)) return false;

            CurrentTurnConnectionId = defenderId;
            return true;
        }
    }
}