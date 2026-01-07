// Client/Services/RemoteControl/RemoteControlSignalRClient.cs
// SignalR Client for real-time updates

using Microsoft.AspNetCore.SignalR.Client;
using MyApp.Shared.DTOs;

namespace MyApp.Client.Services.RemoteControl;

public class RemoteControlSignalRClient : IAsyncDisposable
{
    private HubConnection? _connection;
    private readonly string _hubUrl;

    public event Action<RemoteControlDto>? OnRemoteControlUpdated;
    public bool IsConnected => _connection?.State == HubConnectionState.Connected;

    public RemoteControlSignalRClient(string baseUrl)
    {
        _hubUrl = $"{baseUrl}/hubs/remotecontrol";
    }

    public async Task ConnectAsync()
    {
        if (_connection != null)
            return;

        _connection = new HubConnectionBuilder()
            .WithUrl(_hubUrl)
            .WithAutomaticReconnect()
            .Build();

        _connection.On<RemoteControlDto>("RemoteControlUpdated", (dto) =>
        {
            OnRemoteControlUpdated?.Invoke(dto);
        });

        _connection.Closed += async (error) =>
        {
            Console.WriteLine($"[RemoteControlSignalR] Connection closed: {error?.Message}");
            await Task.Delay(5000);
            await ConnectAsync();
        };

        try
        {
            await _connection.StartAsync();
            Console.WriteLine("[RemoteControlSignalR] Connected");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RemoteControlSignalR] Connection failed: {ex.Message}");
        }
    }

    public async Task JoinRobotGroup(string robotId)
    {
        if (_connection?.State == HubConnectionState.Connected)
        {
            await _connection.InvokeAsync("JoinGroup", robotId);
        }
    }

    public async Task DisconnectAsync()
    {
        if (_connection != null)
        {
            await _connection.StopAsync();
            await _connection.DisposeAsync();
            _connection = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
    }
}