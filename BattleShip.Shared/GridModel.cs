namespace BattleShip.Shared;

public class GridModel
{
    public const int Size = 10;

    private readonly CellState[,] _cells = new CellState[Size, Size];
    
    public CellState GetCell(int x, int y) => _cells[x, y];
    
    public bool TryMarkFired(int x, int y)
    {
        if (_cells[x, y] == CellState.Fired) return false;
        _cells[x, y] = CellState.Fired;
        return true;
    }
}