using MyApp.Application.Abstractions;
using MyApp.Application.Charging.EventHandlers;
using MyApp.Application.Repository;
using MyApp.Domain.Charging;

namespace MyApp.Infrastructure.Industrial.CAN.Adapters;

/// <summary>
/// CAN Adapter for Charging Module
/// - Reads CAN frames and updates ChargingState
/// - Sends CAN control commands
/// </summary>
public sealed class CanChargingAdapter : ICanCommandSender, IDisposable
{
    private readonly SocketCan _can;
    private readonly IChargingRepository _repo;
    private readonly IMessageBus _messageBus;
    private readonly ILogger<CanChargingAdapter> _logger;

    private readonly Timer _txTimer;
    private readonly object _lock = new();

    private TxCommand? _currentCommand;
    private bool _isTxActive;

    private record TxCommand(double Voltage_V, double Current_A, bool PowerStage1, bool ClearFaults);

    public bool IsTxActive => _isTxActive;
    private readonly ISignalRPublisher _signalRPublisher;
    private DateTime _lastBroadcast = DateTime.MinValue;

    public CanChargingAdapter(
        SocketCan can,
        IChargingRepository repo,
        IMessageBus messageBus,
        ISignalRPublisher signalRPublisher,
        ILogger<CanChargingAdapter> logger)
    {
        _can = can;
        _repo = repo;
        _messageBus = messageBus;
        _signalRPublisher = signalRPublisher;
        _logger = logger;

        // ✅ CRITICAL: Subscribe to CAN frame events
        _can.OnFrameReceived += OnCanFrameReceived;

        Console.WriteLine("✅ CanChargingAdapter created and subscribed to CAN events");

        _txTimer = new Timer(_ => OnTxTick(), null, Timeout.Infinite, Timeout.Infinite);
    }

    private async void OnCanFrameReceived(SocketCan.CanFrame frame)
    {
        try
        {
            // ✅ DEBUG: Log every frame received
            Console.WriteLine($"📥 CAN Frame: ID=0x{frame.Id:X3} DLC={frame.Dlc}");

            var state = await _repo.GetActiveAsync() ?? ChargingState.Create();

            DecodeCanFrame(frame.Id, frame.Data, state);

            // Publish domain events
            foreach (var evt in state.DomainEvents)
                await _messageBus.PublishAsync(evt);

            state.ClearDomainEvents();

            await _repo.SaveAsync(state);

            // ✅ BROADCAST REAL-TIME SNAPSHOT (debounced to ~10 Hz)
            var now = DateTime.UtcNow;
            if ((now - _lastBroadcast).TotalMilliseconds >= 100)
            {
                Console.WriteLine($"📡 Broadcasting snapshot: {state.Voltage_V:F1}V {state.Current_A:F2}A");
                await BroadcastSnapshot(state);
                _lastBroadcast = now;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing CAN frame 0x{CanId:X3}", frame.Id);
        }
    }

    private async Task BroadcastSnapshot(ChargingState state)
    {
        var dto = new MyApp.Shared.DTOs.ChargingStatusDto
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

        await _signalRPublisher.PublishChargingSnapshotAsync(dto);
    }

    // ═════════════════════════════════════════════════════════════
    // DECODE CAN FRAME
    // ═════════════════════════════════════════════════════════════

    private void DecodeCanFrame(uint canId, byte[] d, ChargingState state)
    {
        switch (canId)
        {
            case 0x191: // TX Command Echo
                state.EchoCommand(
                    CanBit.Get(d, 0, 20) * 0.001,
                    CanBit.Get(d, 32, 18) * 0.001,
                    CanBit.Get(d, 20, 1) == 1
                );
                break;

            case 0x311: // DC Power Measurement
                state.UpdatePowerMeasurement(
                    CanBit.Get(d, 0, 20) * 0.001,
                    CanBit.Get(d, 20, 18) * 0.001,
                    CanBit.Get(d, 39, 1) == 1
                );
                break;

            case 0x321: // Status Report (Fault source)
                state.UpdateStatus(
                    (ChargerStateEnum)CanBit.Get(d, 0, 6),
                    CanBit.Get(d, 12, 1) == 1,
                    CanBit.Get(d, 18, 1) == 1,
                    CanBit.Get(d, 21, 1) == 1 || CanBit.Get(d, 22, 1) == 1,
                    CanBit.Get(d, 24, 1) == 1
                );
                break;

            case 0x3C1: // AC Input
                state.UpdateAcMeasurement(
                    CanBit.Get(d, 0, 20) * 0.001,
                    CanBit.Get(d, 20, 18) * 0.001,
                    CanBit.Get(d, 38, 10) * 0.1
                );
                break;

            case 0x3E1: // Wireless Status
                var efficiency = CanBit.Get(d, 16, 10) * 0.1;
                var gap = (int)CanBit.Get(d, 32, 8);
                state.UpdateWireless(efficiency, gap, false, false);
                break;

            case 0x5F1: // Wireless Flags
                var underCurrent = CanBit.Get(d, 6, 1) == 1;
                var wirelessOk = CanBit.Get(d, 7, 1) == 1;
                state.UpdateWireless(state.WirelessEfficiency_Pct, state.WirelessGap_Mm, underCurrent, wirelessOk);
                break;

            case 0x3F1: // Temperature
                state.UpdateTemperature(
                    (short)CanBit.Get(d, 0, 16) * 0.005,
                    (short)CanBit.Get(d, 16, 16) * 0.005
                );
                break;

            case 0x511: // Life Report A
            case 0x521: // Life Report B
            case 0x531: // Life Report C
                // Aggregate life stats (simplified)
                if (canId == 0x511)
                {
                    var ahDelivered = CanBit.Get(d, 0, 32) * 0.1;
                    var cycles = (uint)CanBit.Get(d, 32, 32);
                    state.UpdateLifeStats(ahDelivered, cycles, state.UptimeSec, state.LoadTimeSec, state.IdleTimeSec);
                }
                break;

            case 0x721: // Config A
                state.UpdateConfiguration(
                    (uint)CanBit.Get(d, 0, 32),
                    (byte)CanBit.Get(d, 32, 8),
                    (byte)CanBit.Get(d, 40, 8),
                    (byte)CanBit.Get(d, 48, 8),
                    (byte)CanBit.Get(d, 60, 4),
                    state.DeltaPN, 0, 0
                );
                break;

            case 0x771: // Comm Info
                state.UpdateCommunication(
                    (byte)CanBit.Get(d, 0, 8),
                    (byte)CanBit.Get(d, 8, 8),
                    CanBit.Get(d, 16, 32) * 0.000001,
                    state.CanBaudRate
                );
                break;

            case 0x781: // CAN Baud
                state.UpdateCommunication(
                    state.CommChannel,
                    state.CommId,
                    state.CommSuccessRate,
                    (CanBaudRateEnum)CanBit.Get(d, 0, 4)
                );
                break;
        }
    }

    // ═════════════════════════════════════════════════════════════
    // TX COMMANDS (ICanCommandSender)
    // ═════════════════════════════════════════════════════════════

    public Task SendControlCommandAsync(double voltage_V, double current_A, bool powerStage1, bool clearFaults)
    {
        lock (_lock)
        {
            _currentCommand = new TxCommand(voltage_V, current_A, powerStage1, clearFaults);
        }

        SendFrame();
        return Task.CompletedTask;
    }

    public void StartPeriodicTransmission()
    {
        lock (_lock)
        {
            _isTxActive = true;
        }
        _txTimer.Change(0, 100); // 10 Hz
    }

    public void StopPeriodicTransmission()
    {
        lock (_lock)
        {
            _isTxActive = false;
            _currentCommand = new TxCommand(0, 0, false, false);
        }

        // Send OFF frames for 500ms then stop
        for (int i = 0; i < 5; i++)
        {
            SendFrame();
            Thread.Sleep(100);
        }

        _txTimer.Change(Timeout.Infinite, Timeout.Infinite);
    }

    private void OnTxTick()
    {
        if (!_isTxActive)
            return;

        SendFrame();
    }

    private void SendFrame()
    {
        TxCommand? cmd;
        lock (_lock)
        {
            cmd = _currentCommand;
        }

        if (cmd == null)
            return;

        var data = EncodeControlCommand(cmd.Voltage_V, cmd.Current_A, cmd.PowerStage1, cmd.ClearFaults);
        _can.Send(0x191, data);
    }

    // ═════════════════════════════════════════════════════════════
    // ENCODE CONTROL COMMAND (0x191)
    // ═════════════════════════════════════════════════════════════

    private static byte[] EncodeControlCommand(double voltage, double current, bool powerStage1, bool clearFaults)
    {
        var d = new byte[8];

        // Voltage [0..19] – 0.001 V
        ulong v = (ulong)Math.Clamp(voltage * 1000.0, 0, (1UL << 20) - 1);
        CanBit.Set(d, 0, 20, v);

        // PowerStage1 [20]
        CanBit.Set(d, 20, 1, powerStage1 ? 1UL : 0UL);

        // ClearFaults [21]
        CanBit.Set(d, 21, 1, clearFaults ? 1UL : 0UL);

        // Current [32..49] – 0.001 A
        ulong iA = (ulong)Math.Clamp(current * 1000.0, 0, (1UL << 18) - 1);
        CanBit.Set(d, 32, 18, iA);

        return d;
    }

    public void Dispose()
    {
        _txTimer.Dispose();
    }
}