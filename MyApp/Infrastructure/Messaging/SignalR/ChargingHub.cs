using Microsoft.AspNetCore.SignalR;
using MyApp.Application.EventHandlers;
using MyApp.Shared.DTOs;

namespace MyApp.Infrastructure.Messaging.SignalR;

/// <summary>
/// SignalR Hub for real-time Charging updates
/// </summary>
public class ChargingHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        await Clients.Caller.SendAsync("Connected", Context.ConnectionId);
        await base.OnConnectedAsync();
    }
}

/// <summary>
/// SignalR Publisher Implementation
/// </summary>
public class ChargingSignalRPublisher : ISignalRPublisher
{
    private readonly IHubContext<ChargingHub> _hub;

    public ChargingSignalRPublisher(IHubContext<ChargingHub> hub)
    {
        _hub = hub;
    }

    public Task PublishChargingStatusAsync(Guid id, bool charging)
        => _hub.Clients.All.SendAsync("ChargingStatusChanged", new
        {
            ChargingId = id,
            IsCharging = charging,
            Timestamp = DateTime.UtcNow
        });

    public Task PublishChargingFaultAsync(Guid id, bool ocp, bool ovp, bool watchdog)
        => _hub.Clients.All.SendAsync("ChargingFault", new
        {
            ChargingId = id,
            Ocp = ocp,
            Ovp = ovp,
            Watchdog = watchdog,
            Timestamp = DateTime.UtcNow
        });

    public Task PublishChargingWarningAsync(Guid id, string msg)
        => _hub.Clients.All.SendAsync("ChargingWarning", new
        {
            ChargingId = id,
            Message = msg,
            Timestamp = DateTime.UtcNow
        });

    public Task PublishChargingSnapshotAsync(ChargingStatusDto dto)
        => _hub.Clients.All.SendAsync("ChargingSnapshot", dto);
}