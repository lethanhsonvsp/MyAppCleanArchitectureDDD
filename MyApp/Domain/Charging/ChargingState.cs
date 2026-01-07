using MyApp.Domain.Events;

namespace MyApp.Domain.Charging;

/// <summary>
/// Charging Aggregate Root - Domain Entity
/// </summary>
public sealed class ChargingState
{
    // ===== Identity =====
    public Guid Id { get; private set; }

    // ===== Power Measurements =====
    public double Voltage_V { get; private set; }
    public double Current_A { get; private set; }
    public bool IsCharging { get; private set; }

    // ===== Status =====
    public ChargerStateEnum State { get; private set; }
    public bool HasFault { get; private set; }
    public bool HasOcp { get; private set; }
    public bool HasOvp { get; private set; }
    public bool HasWatchdogFault { get; private set; }

    // ===== AC Input =====
    public double AcVoltage_V { get; private set; }
    public double AcCurrent_A { get; private set; }
    public double AcFrequency_Hz { get; private set; }

    // ===== Wireless =====
    public double WirelessEfficiency_Pct { get; private set; }
    public int WirelessGap_Mm { get; private set; }
    public bool WirelessUnderCurrent { get; private set; }
    public bool WirelessOk { get; private set; }

    // ===== Temperature =====
    public double SecondaryTemp_C { get; private set; }
    public double PrimaryTemp_C { get; private set; }

    // ===== Life Statistics =====
    public double AhDelivered { get; private set; }
    public uint ChargeCycles { get; private set; }
    public uint UptimeSec { get; private set; }
    public uint LoadTimeSec { get; private set; }
    public uint IdleTimeSec { get; private set; }

    // ===== Configuration =====
    public uint SerialNumber { get; private set; }
    public string FirmwareVersion { get; private set; } = string.Empty;
    public string HardwareVersion { get; private set; } = string.Empty;
    public byte McuId { get; private set; }
    public uint DeltaPN { get; private set; }

    // ===== Communication =====
    public byte CommChannel { get; private set; }
    public byte CommId { get; private set; }
    public double CommSuccessRate { get; private set; }
    public CanBaudRateEnum CanBaudRate { get; private set; }

    // ===== Command Echo (Last TX) =====
    public double LastDemandVoltage_V { get; private set; }
    public double LastDemandCurrent_A { get; private set; }
    public bool LastPowerStage1 { get; private set; }
    public DateTime LastCommandTime { get; private set; }

    // ===== Timestamps =====
    public DateTime LastUpdated { get; private set; }

    // ===== Domain Events (not persisted) =====
    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    private ChargingState() { } // EF Core

    public static ChargingState Create()
    {
        var state = new ChargingState
        {
            Id = Guid.NewGuid(),
            State = ChargerStateEnum.Uninit,
            LastUpdated = DateTime.UtcNow
        };

        state.AddDomainEvent(new ChargingStateCreatedEvent(state.Id));
        return state;
    }

    // ═════════════════════════════════════════════════════════════
    // UPDATE METHODS (Called by Application Layer)
    // ═════════════════════════════════════════════════════════════

    public void UpdatePowerMeasurement(double voltage, double current, bool charging)
    {
        var oldCharging = IsCharging;

        Voltage_V = voltage;
        Current_A = current;
        IsCharging = charging;
        LastUpdated = DateTime.UtcNow;

        if (oldCharging != charging)
        {
            AddDomainEvent(new ChargingStatusChangedEvent(Id, charging));
        }
    }

    public void UpdateStatus(ChargerStateEnum state, bool fault, bool ocp, bool ovp, bool watchdog)
    {
        var oldFault = HasFault;

        State = state;
        HasFault = fault;
        HasOcp = ocp;
        HasOvp = ovp;
        HasWatchdogFault = watchdog;
        LastUpdated = DateTime.UtcNow;

        if (!oldFault && fault)
        {
            AddDomainEvent(new ChargingFaultDetectedEvent(Id, ocp, ovp, watchdog));
        }
        else if (oldFault && !fault)
        {
            AddDomainEvent(new ChargingFaultClearedEvent(Id));
        }
    }

    public void UpdateAcMeasurement(double voltage, double current, double frequency)
    {
        AcVoltage_V = voltage;
        AcCurrent_A = current;
        AcFrequency_Hz = frequency;
        LastUpdated = DateTime.UtcNow;
    }

    public void UpdateWireless(double efficiency, int gap, bool underCurrent, bool ok)
    {
        WirelessEfficiency_Pct = efficiency;
        WirelessGap_Mm = gap;
        WirelessUnderCurrent = underCurrent;
        WirelessOk = ok;
        LastUpdated = DateTime.UtcNow;
    }

    public void UpdateTemperature(double secondary, double primary)
    {
        SecondaryTemp_C = secondary;
        PrimaryTemp_C = primary;
        LastUpdated = DateTime.UtcNow;

        // Business rule: Overtemp warning
        if (secondary > 85 || primary > 85)
        {
            AddDomainEvent(new ChargingOvertemperatureWarningEvent(Id, secondary, primary));
        }
    }

    public void UpdateLifeStats(double ahDelivered, uint cycles, uint uptime, uint loadTime, uint idleTime)
    {
        AhDelivered = ahDelivered;
        ChargeCycles = cycles;
        UptimeSec = uptime;
        LoadTimeSec = loadTime;
        IdleTimeSec = idleTime;
        LastUpdated = DateTime.UtcNow;
    }

    public void UpdateConfiguration(uint serial, byte swMajor, byte swMinor, byte swDebug,
        byte mcuId, uint deltaPN, byte hwMajor, byte hwMinor)
    {
        SerialNumber = serial;
        FirmwareVersion = $"{swMajor}.{swMinor}.{swDebug}";
        HardwareVersion = $"{hwMajor}.{hwMinor}";
        McuId = mcuId;
        DeltaPN = deltaPN;
        LastUpdated = DateTime.UtcNow;
    }

    public void UpdateCommunication(byte channel, byte commId, double successRate, CanBaudRateEnum baudRate)
    {
        CommChannel = channel;
        CommId = commId;
        CommSuccessRate = successRate;
        CanBaudRate = baudRate;
        LastUpdated = DateTime.UtcNow;
    }

    public void EchoCommand(double voltage, double current, bool powerStage1)
    {
        LastDemandVoltage_V = voltage;
        LastDemandCurrent_A = current;
        LastPowerStage1 = powerStage1;
        LastCommandTime = DateTime.UtcNow;
    }

    // ═════════════════════════════════════════════════════════════
    // DOMAIN EVENT MANAGEMENT
    // ═════════════════════════════════════════════════════════════

    private void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}

public enum ChargerStateEnum
{
    Uninit = 0x00,
    Standby = 0x01,
    Charging = 0x04,
    Fault = 0x10
}

public enum CanBaudRateEnum
{
    K125 = 0,
    K250 = 1,
    K500 = 2,
    K1000 = 3,
    K800 = 4
}