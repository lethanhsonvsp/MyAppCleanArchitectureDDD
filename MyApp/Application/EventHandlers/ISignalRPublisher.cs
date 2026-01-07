using MyApp.Shared.DTOs;

namespace MyApp.Application.Charging.EventHandlers;

/// <summary>
/// SignalR Publisher Interface
/// Used by Event Handlers to broadcast real-time updates to clients
/// </summary>
public interface ISignalRPublisher
{
    /// <summary>
    /// Broadcast when charging status changes (start/stop)
    /// </summary>
    Task PublishChargingStatusAsync(Guid chargingId, bool isCharging);

    /// <summary>
    /// Broadcast when fault detected (OCP, OVP, Watchdog)
    /// </summary>
    Task PublishChargingFaultAsync(Guid chargingId, bool ocp, bool ovp, bool watchdog);

    /// <summary>
    /// Broadcast warnings (e.g., overtemperature)
    /// </summary>
    Task PublishChargingWarningAsync(Guid chargingId, string message);

    /// <summary>
    /// Broadcast complete charging snapshot (called on every CAN frame)
    /// </summary>
    Task PublishChargingSnapshotAsync(ChargingStatusDto snapshot);
}