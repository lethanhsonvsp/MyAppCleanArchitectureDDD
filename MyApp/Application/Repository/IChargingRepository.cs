using MyApp.Domain.Charging;
using MyApp.Domain.Events;

namespace MyApp.Application.Repository;

// ═════════════════════════════════════════════════════════════
// REPOSITORY INTERFACE
// ═════════════════════════════════════════════════════════════

public interface IChargingRepository
{
    Task<ChargingState?> GetActiveAsync(CancellationToken ct = default);
    Task<ChargingState?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task SaveAsync(ChargingState state, CancellationToken ct = default);
}

// ═════════════════════════════════════════════════════════════
// CAN COMMAND SENDER INTERFACE
// ═════════════════════════════════════════════════════════════

public interface ICanCommandSender
{
    Task SendControlCommandAsync(
        double voltage_V,
        double current_A,
        bool powerStage1,
        bool clearFaults);

    void StartPeriodicTransmission();
    void StopPeriodicTransmission();
    bool IsTxActive { get; }
}


