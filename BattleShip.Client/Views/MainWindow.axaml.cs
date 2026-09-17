using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using BattleShip.Shared;

namespace BattleShip.Client.Views;

public partial class MainWindow : Window
{
    private const string ServerUrl = "http://localhost:5058";

    private readonly ConnectionService _connection = new();
    private readonly Button[,] _yourButtons = new Button[GridModel.Size, GridModel.Size];
    private readonly Button[,] _enemyButtons = new Button[GridModel.Size, GridModel.Size];

    private string? _myConnectionId;
    private bool _isMyTurn;
    
    public MainWindow()
    {
        InitializeComponent();

        _connection.MatchFound += OnMatchFound;
        _connection.ShotResultReceived += OnShotResultReceived;
        _connection.OpponentDisconnected += OnOpponentDisconnected;
    }

    private async void OnFindMatchClicked(object? sender, RoutedEventArgs e)
    {
        FindMatchButton.IsEnabled = false;
        StatusText.Text = "Connecting...";

        try
        {
            await _connection.ConnectAsync(ServerUrl);
            _myConnectionId = _connection.MyConnectionId;

            StatusText.Text = "Searching for an opponent...";
            await _connection.FindMatchAsync();
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Connection failed: {ex.Message}";
            FindMatchButton.IsEnabled = true;
        }
    }

    private void OnMatchFound(MatchFoundMessage msg)
    {
        Dispatcher.UIThread.Post(() =>
        {
            _isMyTurn = msg.YouGoFirst;
            BuildGrids();
            MenuPanel.IsVisible = false;
            GamePanel.IsVisible = true;
            UpdateTurnText();
        });
    }

    private void OnShotResultReceived(ShotResultMessage msg)
    {
        Dispatcher.UIThread.Post(() =>
        {
            bool iShotThis = msg.ShooterConnectionId == _myConnectionId;
            var grid = iShotThis ? _enemyButtons : _yourButtons;

            var button = grid[msg.X, msg.Y];
            button.Content = "X";
            button.Background = Brushes.OrangeRed;

            _isMyTurn = msg.IsYourTurnNext;
            UpdateTurnText();
        });
    }
    
    private void OnOpponentDisconnected(OpponentDisconnectedMessage msg)
    {
        Dispatcher.UIThread.Post(() =>
        {
            StatusText.Text = "Opponent disconnected.";
            GamePanel.IsVisible = false;
            MenuPanel.IsVisible = true;
            FindMatchButton.IsEnabled = true;
        });
    }
    
    private void BuildGrids()
    {
        YourGrid.Children.Clear();
        EnemyGrid.Children.Clear();

        for (int y = 0; y < GridModel.Size; y++)
        {
            for (int x = 0; x < GridModel.Size; x++)
            {
                var yourCell = new Button { Background = Brushes.SteelBlue, IsEnabled = false, Margin = new Avalonia.Thickness(1) };
                _yourButtons[x, y] = yourCell;
                YourGrid.Children.Add(yourCell);

                int capturedX = x, capturedY = y;
                var enemyCell = new Button { Background = Brushes.DarkSlateGray, Margin = new Avalonia.Thickness(1) };
                enemyCell.Click += async (_, _) => await OnEnemyCellClicked(capturedX, capturedY, enemyCell);
                _enemyButtons[x, y] = enemyCell;
                EnemyGrid.Children.Add(enemyCell);
            }
        }
    }
    
    private async System.Threading.Tasks.Task OnEnemyCellClicked(int x, int y, Button clicked)
    {
        if (!_isMyTurn) return;
        if (clicked.Content is not null) return; // already fired on this cell

        await _connection.FireShotAsync(x, y);
    }
    
    private void UpdateTurnText()
    {
        TurnText.Text = _isMyTurn ? "Your turn" : "Opponent's turn";
    }
}
