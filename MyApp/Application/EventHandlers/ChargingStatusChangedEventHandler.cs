using MediatR;
using MyApp.Application.Repository;
using MyApp.Domain.Events;
using MyApp.Shared.DTOs;

namespace MyApp.Application.EventHandlers;

#region ================= APPLICATION NOTIFICATIONS =================

// Domain → Application
public record ChargingStatusChangedNotification(
    ChargingStatusChangedEvent DomainEvent
) : INotification;

public record ChargingFaultDetectedNotification(
    ChargingFaultDetectedEvent DomainEvent
) : INotification;

public record ChargingOvertemperatureWarningNotification(
    ChargingOvertemperatureWarningEvent DomainEvent
) : INotification;

// Snapshot notification (FULL STATE)
public record ChargingSnapshotNotification(
    ChargingStatusDto Dto
) : INotification;

#endregion

// ═════════════════════════════════════════════════════════════
// SNAPSHOT HANDLER (QUAN TRỌNG NHẤT)
// ═════════════════════════════════════════════════════════════

public class ChargingSnapshotEventHandler
    : INotificationHandler<ChargingSnapshotNotification>
{
    private readonly ISignalRPublisher _signalR;

    public ChargingSnapshotEventHandler(ISignalRPublisher signalR)
    {
        _signalR = signalR;
    }

    public async Task Handle(ChargingSnapshotNotification n, CancellationToken ct)
    {
        await _signalR.PublishChargingSnapshotAsync(n.Dto);
    }
}

// ═════════════════════════════════════════════════════════════
// STATUS CHANGED HANDLER
// ═════════════════════════════════════════════════════════════

public class ChargingStatusChangedEventHandler
    : INotificationHandler<ChargingStatusChangedNotification>
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

    public async Task Handle(
        ChargingStatusChangedNotification notification,
        CancellationToken ct)
    {
        var evt = notification.DomainEvent;

        _logger.LogInformation(
            "Charging status changed: {ChargingId} → {IsCharging}",
            evt.ChargingId, evt.IsCharging);

        await _signalR.PublishChargingStatusAsync(
            evt.ChargingId,
            evt.IsCharging);
    }
}

// ═════════════════════════════════════════════════════════════
// FAULT HANDLER
// ═════════════════════════════════════════════════════════════

public class ChargingFaultDetectedEventHandler
    : INotificationHandler<ChargingFaultDetectedNotification>
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

    public async Task Handle(
        ChargingFaultDetectedNotification notification,
        CancellationToken ct)
    {
        var evt = notification.DomainEvent;

        _logger.LogWarning(
            "FAULT: {ChargingId} OCP={Ocp} OVP={Ovp} WD={Watchdog}",
            evt.ChargingId, evt.Ocp, evt.Ovp, evt.Watchdog);

        _canSender.StopPeriodicTransmission();

        await _signalR.PublishChargingFaultAsync(
            evt.ChargingId,
            evt.Ocp,
            evt.Ovp,
            evt.Watchdog);
    }
}

public interface ISignalRPublisher
{
    Task PublishChargingStatusAsync(Guid chargingId, bool isCharging);
    Task PublishChargingFaultAsync(Guid chargingId, bool ocp, bool ovp, bool watchdog);
    Task PublishChargingWarningAsync(Guid chargingId, string message);
    Task PublishChargingSnapshotAsync(ChargingStatusDto dto);
}
