namespace BattleShip.Shared;

public record FireShotRequest(int X, int Y);

public record MatchFoundMessage(string SessionId, bool YouGoFirst);

public record ShotResultMessage(string ShooterConnectionId, int X, int Y, bool IsYourTurnNext);

public record OpponentDisconnectedMessage(string SessionId);