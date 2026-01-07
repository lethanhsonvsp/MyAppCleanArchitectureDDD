using MediatR;
using Microsoft.Extensions.Logging;
using MyApp.Application.EventHandlers;
using MyApp.Application.Repository;
using MyApp.Domain.Events;

namespace MyApp.Application.Charging.EventHandlers;

// ═════════════════════════════════════════════════════════════
// CHARGING STATUS CHANGED EVENT HANDLER
// ═════════════════════════════════════════════════════════════

public class ChargingStatusChangedEventHandler : INotificationHandler<ChargingStatusChangedEvent>
{
    private readonly ILogger<ChargingStatusChangedEventHandler> _logger;
    private readonly ISignalRPublisher _signalR;

    public ChargingStatusChangedEventHandler(
        ILogger<ChargingStatusChangedEventHandler> logger,
        ISignalRPublisher signalR)
    {
        _logger = logger;
        _signalR = signalR;
    }

    public async Task Handle(ChargingStatusChangedEvent evt, CancellationToken ct)
    {
        _logger.LogInformation(
            "Charging status changed: ChargingId={ChargingId}, IsCharging={IsCharging}",
            evt.ChargingId, evt.IsCharging);

        // Broadcast to all connected clients via SignalR
        await _signalR.PublishChargingStatusAsync(evt.ChargingId, evt.IsCharging);
    }
}

// ═════════════════════════════════════════════════════════════
// FAULT DETECTED EVENT HANDLER
// ═════════════════════════════════════════════════════════════

public class ChargingFaultDetectedEventHandler : INotificationHandler<ChargingFaultDetectedEvent>
{
    private readonly ILogger<ChargingFaultDetectedEventHandler> _logger;
    private readonly ISignalRPublisher _signalR;
    private readonly ICanCommandSender _canSender;

    public ChargingFaultDetectedEventHandler(
        ILogger<ChargingFaultDetectedEventHandler> logger,
        ISignalRPublisher signalR,
        ICanCommandSender canSender)
    {
        _logger = logger;
        _signalR = signalR;
        _canSender = canSender;
    }

    public async Task Handle(ChargingFaultDetectedEvent evt, CancellationToken ct)
    {
        _logger.LogWarning(
            "FAULT DETECTED: ChargingId={ChargingId}, OCP={Ocp}, OVP={Ovp}, Watchdog={Watchdog}",
            evt.ChargingId, evt.Ocp, evt.Ovp, evt.Watchdog);

        // Auto-stop charging on fault
        _canSender.StopPeriodicTransmission();

        // Notify UI
        await _signalR.PublishChargingFaultAsync(evt.ChargingId, evt.Ocp, evt.Ovp, evt.Watchdog);
    }
}

// ═════════════════════════════════════════════════════════════
// OVERTEMPERATURE WARNING HANDLER
// ═════════════════════════════════════════════════════════════

public class ChargingOvertemperatureWarningEventHandler : INotificationHandler<ChargingOvertemperatureWarningEvent>
{
    private readonly ILogger<ChargingOvertemperatureWarningEventHandler> _logger;
    private readonly ISignalRPublisher _signalR;

    public ChargingOvertemperatureWarningEventHandler(
        ILogger<ChargingOvertemperatureWarningEventHandler> logger,
        ISignalRPublisher signalR)
    {
        _logger = logger;
        _signalR = signalR;
    }

    public async Task Handle(ChargingOvertemperatureWarningEvent evt, CancellationToken ct)
    {
        _logger.LogWarning(
            "OVERTEMPERATURE: ChargingId={ChargingId}, Secondary={Secondary}°C, Primary={Primary}°C",
            evt.ChargingId, evt.SecondaryTemp_C, evt.PrimaryTemp_C);

        await _signalR.PublishChargingWarningAsync(evt.ChargingId,
            $"Overtemperature: Secondary={evt.SecondaryTemp_C:F1}°C, Primary={evt.PrimaryTemp_C:F1}°C");
    }
}