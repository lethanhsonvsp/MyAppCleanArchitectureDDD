using Microsoft.AspNetCore.SignalR;
using MyApp.Application.Charging.EventHandlers;
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
    private readonly IHubContext<ChargingHub> _hubContext;

    public ChargingSignalRPublisher(IHubContext<ChargingHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task PublishChargingStatusAsync(Guid chargingId, bool isCharging)
    {
        await _hubContext.Clients.All.SendAsync("ChargingStatusChanged", new
        {
            ChargingId = chargingId,
            IsCharging = isCharging,
            Timestamp = DateTime.UtcNow
        });
    }
    public async Task PublishChargingSnapshotAsync(ChargingStatusDto dto)
    {
        await _hubContext.Clients.All.SendAsync(
            "ChargingSnapshot",
            dto
        );
    }

    public async Task PublishChargingFaultAsync(Guid chargingId, bool ocp, bool ovp, bool watchdog)
    {
        await _hubContext.Clients.All.SendAsync("ChargingFault", new
        {
            ChargingId = chargingId,
            Ocp = ocp,
            Ovp = ovp,
            Watchdog = watchdog,
            Timestamp = DateTime.UtcNow
        });
    }

    public async Task PublishChargingWarningAsync(Guid chargingId, string message)
    {
        await _hubContext.Clients.All.SendAsync("ChargingWarning", new
        {
            ChargingId = chargingId,
            Message = message,
            Timestamp = DateTime.UtcNow
        });
    }
}