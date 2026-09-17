using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using BattleShip.Shared;
namespace BattleShip.Client;

public class ConnectionService
{
    private HubConnection? _connection;
    public string? MyConnectionId => _connection?.ConnectionId;

    public event Action<MatchFoundMessage>? MatchFound;
    public event Action<ShotResultMessage>? ShotResultReceived;
    public event Action<OpponentDisconnectedMessage>? OpponentDisconnected;
    
    public async Task ConnectAsync(string serverUrl)
    {
        _connection = new HubConnectionBuilder()
            .WithUrl($"{serverUrl}/gamehub")
            .WithAutomaticReconnect()
            .Build();

        _connection.On<MatchFoundMessage>("MatchFound", msg => MatchFound?.Invoke(msg));
        _connection.On<ShotResultMessage>("ShotResult", msg => ShotResultReceived?.Invoke(msg));
        _connection.On<OpponentDisconnectedMessage>("OpponentDisconnected", msg => OpponentDisconnected?.Invoke(msg));

        await _connection.StartAsync();
    }
    
    public Task FindMatchAsync()
    {
        if (_connection is null) throw new InvalidOperationException("Not connected yet.");
        return _connection.InvokeAsync("FindMatch");
    }

    public Task FireShotAsync(int x, int y)
    {
        if (_connection is null) throw new InvalidOperationException("Not connected yet.");
        return _connection.InvokeAsync("FireShot", new FireShotRequest(x, y));
    }
}