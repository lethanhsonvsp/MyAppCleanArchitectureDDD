using MyApp.Application.Queries;
using MyApp.Hubs;
using MyApp.Infrastructure.Hardware;

namespace MyApp.Application.EventHandlers;

public sealed class RemoteControlEventHandler : IHostedService
{
    private readonly RemoteControlHardwareService _hardware;
    private readonly RemoteControlSignalRPublisher _publisher;
    private readonly RemoteControlStateCache _cache;

    public RemoteControlEventHandler(
        RemoteControlHardwareService hardware,
        RemoteControlSignalRPublisher publisher,
        RemoteControlStateCache cache)
    {
        _hardware = hardware;
        _publisher = publisher;
        _cache = cache;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _hardware.OnStateChanged += HandleStateChanged;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _hardware.OnStateChanged -= HandleStateChanged;
        return Task.CompletedTask;
    }

    private async void HandleStateChanged(MyApp.Domain.Events.RemoteControlUpdatedEvent domainEvent)
    {
        // Update cache
        var state = new MyApp.Domain.RemoteControl.RemoteControlState();
        state.UpdateFromModbus(new byte[8]); // TODO: lấy data thật
        _cache.UpdateState(state);

        // Map to DTO
        var dto = new MyApp.Shared.DTOs.RemoteControlDto
        {
            Heartbeat = domainEvent.Heartbeat,
            RemoteReady = domainEvent.RemoteReady,
            LostLink = false,
            EStop = domainEvent.EStop,
            Enable = domainEvent.Enable,
            Mode = domainEvent.Mode,
            Linear = domainEvent.Linear,
            Angular = domainEvent.Angular,
            Speed = domainEvent.Speed,
            Action = domainEvent.Action,
            Timestamp = domainEvent.Timestamp
        };

        await _publisher.BroadcastStateUpdate(dto);
    }
}
