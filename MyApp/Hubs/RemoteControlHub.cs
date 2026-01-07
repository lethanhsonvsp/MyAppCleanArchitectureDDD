using Microsoft.AspNetCore.SignalR;
using MyApp.Shared.DTOs;

namespace MyApp.Hubs;

public class RemoteControlHub : Hub
{
    public async Task JoinGroup(string robotId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"robot_{robotId}");
        await Clients.Caller.SendAsync("Joined", $"Joined robot_{robotId}");
    }

    public async Task LeaveGroup(string robotId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"robot_{robotId}");
    }

    public override async Task OnConnectedAsync()
    {
        Console.WriteLine($"[RemoteControlHub] Client connected: {Context.ConnectionId}");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        Console.WriteLine($"[RemoteControlHub] Client disconnected: {Context.ConnectionId}");
        await base.OnDisconnectedAsync(exception);
    }
}

// SignalR Publisher - call from background service
public class RemoteControlSignalRPublisher
{
    private readonly IHubContext<RemoteControlHub> _hubContext;

    public RemoteControlSignalRPublisher(IHubContext<RemoteControlHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task BroadcastStateUpdate(RemoteControlDto dto)
    {
        await _hubContext.Clients.All.SendAsync("RemoteControlUpdated", dto);
    }

    public async Task BroadcastToRobot(string robotId, RemoteControlDto dto)
    {
        await _hubContext.Clients.Group($"robot_{robotId}").SendAsync("RemoteControlUpdated", dto);
    }
}