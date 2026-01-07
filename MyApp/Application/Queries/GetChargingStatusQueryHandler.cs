using MediatR;
using MyApp.Application.Repository;
using MyApp.Shared.DTOs;

namespace MyApp.Application.Queries;

// ═════════════════════════════════════════════════════════════
// GET CHARGING STATUS QUERY
// ═════════════════════════════════════════════════════════════

public record GetChargingStatusQuery : IRequest<ChargingStatusDto?>;

public class GetChargingStatusQueryHandler : IRequestHandler<GetChargingStatusQuery, ChargingStatusDto?>
{
    private readonly IChargingRepository _repo;

    public GetChargingStatusQueryHandler(IChargingRepository repo)
    {
        _repo = repo;
    }

    public async Task<ChargingStatusDto?> Handle(GetChargingStatusQuery query, CancellationToken ct)
    {
        var state = await _repo.GetActiveAsync(ct);
        if (state == null)
            return null;

        return new ChargingStatusDto
        {
            Id = state.Id,
            Voltage_V = state.Voltage_V,
            Current_A = state.Current_A,
            Power_W = state.Voltage_V * state.Current_A,
            IsCharging = state.IsCharging,
            State = state.State.ToString(),
            HasFault = state.HasFault,
            HasOcp = state.HasOcp,
            HasOvp = state.HasOvp,
            HasWatchdogFault = state.HasWatchdogFault,

            AcVoltage_V = state.AcVoltage_V,
            AcCurrent_A = state.AcCurrent_A,
            AcFrequency_Hz = state.AcFrequency_Hz,

            WirelessEfficiency_Pct = state.WirelessEfficiency_Pct,
            WirelessGap_Mm = state.WirelessGap_Mm,
            WirelessOk = state.WirelessOk,

            SecondaryTemp_C = state.SecondaryTemp_C,
            PrimaryTemp_C = state.PrimaryTemp_C,

            LastUpdated = state.LastUpdated
        };
    }
}

// ═════════════════════════════════════════════════════════════
// GET CHARGING STATS QUERY
// ═════════════════════════════════════════════════════════════

public record GetChargingStatsQuery : IRequest<ChargingStatsDto?>;

public class GetChargingStatsQueryHandler : IRequestHandler<GetChargingStatsQuery, ChargingStatsDto?>
{
    private readonly IChargingRepository _repo;

    public GetChargingStatsQueryHandler(IChargingRepository repo)
    {
        _repo = repo;
    }

    public async Task<ChargingStatsDto?> Handle(GetChargingStatsQuery query, CancellationToken ct)
    {
        var state = await _repo.GetActiveAsync(ct);
        if (state == null)
            return null;

        return new ChargingStatsDto
        {
            AhDelivered = state.AhDelivered,
            ChargeCycles = state.ChargeCycles,
            UptimeHours = state.UptimeSec / 3600.0,
            LoadTimeHours = state.LoadTimeSec / 3600.0,
            IdleTimeHours = state.IdleTimeSec / 3600.0,

            SerialNumber = state.SerialNumber,
            FirmwareVersion = state.FirmwareVersion,
            HardwareVersion = state.HardwareVersion,

            CommSuccessRate = state.CommSuccessRate,
            CanBaudRate = state.CanBaudRate.ToString()
        };
    }
}