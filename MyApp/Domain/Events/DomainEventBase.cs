namespace MyApp.Domain.Events;

public interface IDomainEvent
{
    DateTime OccurredAt { get; }
}

public abstract record DomainEventBase : IDomainEvent
{
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}

// ═════════════════════════════════════════════════════════════
// CHARGING DOMAIN EVENTS
// ═════════════════════════════════════════════════════════════

public record ChargingStateCreatedEvent(Guid ChargingId) : DomainEventBase;

public record ChargingStatusChangedEvent(
    Guid ChargingId,
    bool IsCharging
) : DomainEventBase;

public record ChargingFaultDetectedEvent(
    Guid ChargingId,
    bool Ocp,
    bool Ovp,
    bool Watchdog
) : DomainEventBase;

public record ChargingFaultClearedEvent(Guid ChargingId) : DomainEventBase;

public record ChargingOvertemperatureWarningEvent(
    Guid ChargingId,
    double SecondaryTemp_C,
    double PrimaryTemp_C
) : DomainEventBase;

public record ChargingCommandSentEvent(
    Guid ChargingId,
    double Voltage_V,
    double Current_A,
    bool PowerStage1
) : DomainEventBase;