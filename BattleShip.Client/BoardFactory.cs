using Avalonia.Controls;
using Avalonia.Media;
using System;

namespace BattleShip.Client;

public enum BoardCellOwner
{
    Player,
    Enemy,
}

public sealed class BoardFactory
{
    public Button CreateBoardCell(BoardCellOwner owner, bool hasShip)
    {
        var button = new Button
        {
            Content = owner == BoardCellOwner.Player && hasShip ? "X" : null,
            Margin = new Avalonia.Thickness(1),
        };

        switch (owner)
        {
            case BoardCellOwner.Player:
                button.Background = Brushes.SteelBlue;
                button.IsEnabled = false;
                break;
            case BoardCellOwner.Enemy:
                button.Background = Brushes.DarkSlateGray;
                button.IsEnabled = true;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(owner), owner, null);
        }

        return button;
    }
}