namespace BattleShip.Shared;

public class GridModel
{
    public const int Size = 10;

    private readonly CellState[,] _cells = new CellState[Size, Size];

    public GridModel() //temp for filling the grid with ship cells every other cell
    {
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                if ((x + y) % 2 == 0)
                {
                    _cells[x, y] = CellState.Ship;
                }
            }
        }
    }
    
    public CellState GetCell(int x, int y) => _cells[x, y];
    
    public bool TryMarkFired(int x, int y)
    {
        if (_cells[x, y] == CellState.Fired) return false;
        _cells[x, y] = CellState.Fired;
        return true;
    }
}