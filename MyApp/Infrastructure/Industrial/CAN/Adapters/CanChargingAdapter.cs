using MyApp.Application.Abstractions;
using MyApp.Application.Repository;
using MyApp.Domain.Charging;
using MyApp.Infrastructure.Industrial.CAN;

namespace MyApp.Infrastructure.Industrial.CAN.Adapters;

public sealed class CanChargingAdapter : ICanCommandSender, IDisposable
{
    private readonly SocketCan _can;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CanChargingAdapter> _logger;

    private readonly Timer _txTimer;
    private readonly object _lock = new();

    private TxCommand? _currentCommand;
    private bool _isTxActive;

    private record TxCommand(double Voltage_V, double Current_A, bool PowerStage1, bool ClearFaults);

    public bool IsTxActive => _isTxActive;

    public CanChargingAdapter(
        SocketCan can,
        IServiceScopeFactory scopeFactory, // ← Chỉ cần scopeFactory thôi
        ILogger<CanChargingAdapter> logger)
    {
        _can = can;
        _scopeFactory = scopeFactory;
        _logger = logger;

        _can.OnFrameReceived += OnCanFrameReceived;
        _txTimer = new Timer(_ => OnTxTick(), null, Timeout.Infinite, Timeout.Infinite);
    }

    // ═════════════════════════════════════════════════════════════
    // RX HANDLER
    // ═════════════════════════════════════════════════════════════

    private async void OnCanFrameReceived(SocketCan.CanFrame frame)
    {
        try
        {
            // Tạo scope mới, lấy cả repo và messageBus từ scope
            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IChargingRepository>();
            var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

            var state = await repo.GetActiveAsync() ?? ChargingState.Create();

            DecodeCanFrame(frame.Id, frame.Data, state);

            // Publish domain events qua messageBus từ scope
            foreach (var evt in state.DomainEvents)
                await messageBus.PublishAsync(evt);

            state.ClearDomainEvents();

            await repo.SaveAsync(state);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing CAN frame 0x{CanId:X3}", frame.Id);
        }
    }

    // ═════════════════════════════════════════════════════════════
    // DECODE CAN FRAME - GIỮ NGUYÊN
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
    // TX COMMANDS - GIỮ NGUYÊN HẾT
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
        _txTimer.Change(0, 100);
    }

    public void StopPeriodicTransmission()
    {
        lock (_lock)
        {
            _isTxActive = false;
            _currentCommand = new TxCommand(0, 0, false, false);
        }

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

    private static byte[] EncodeControlCommand(double voltage, double current, bool powerStage1, bool clearFaults)
    {
        var d = new byte[8];

        ulong v = (ulong)Math.Clamp(voltage * 1000.0, 0, (1UL << 20) - 1);
        CanBit.Set(d, 0, 20, v);

        CanBit.Set(d, 20, 1, powerStage1 ? 1UL : 0UL);
        CanBit.Set(d, 21, 1, clearFaults ? 1UL : 0UL);

        ulong iA = (ulong)Math.Clamp(current * 1000.0, 0, (1UL << 18) - 1);
        CanBit.Set(d, 32, 18, iA);

        return d;
    }

    public void Dispose()
    {
        _txTimer.Dispose();
    }
}