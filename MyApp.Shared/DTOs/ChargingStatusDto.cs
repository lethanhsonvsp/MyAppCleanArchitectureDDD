namespace MyApp.Shared.DTOs;

/// <summary>
/// Charging Status DTO - Real-time snapshot
/// </summary>
public class ChargingStatusDto
{
    public Guid Id { get; set; }

    // Power
    public double Voltage_V { get; set; }
    public double Current_A { get; set; }
    public double Power_W { get; set; }
    public bool IsCharging { get; set; }

    // Status
    public string State { get; set; } = string.Empty;
    public bool HasFault { get; set; }
    public bool HasOcp { get; set; }
    public bool HasOvp { get; set; }
    public bool HasWatchdogFault { get; set; }

    // AC Input
    public double AcVoltage_V { get; set; }
    public double AcCurrent_A { get; set; }
    public double AcFrequency_Hz { get; set; }

    // Wireless
    public double WirelessEfficiency_Pct { get; set; }
    public int WirelessGap_Mm { get; set; }
    public bool WirelessOk { get; set; }

    // Temperature
    public double SecondaryTemp_C { get; set; }
    public double PrimaryTemp_C { get; set; }

    public DateTime LastUpdated { get; set; }
}

/// <summary>
/// Charging Statistics DTO - Historical data
/// </summary>
public class ChargingStatsDto
{
    public double AhDelivered { get; set; }
    public uint ChargeCycles { get; set; }
    public double UptimeHours { get; set; }
    public double LoadTimeHours { get; set; }
    public double IdleTimeHours { get; set; }

    public uint SerialNumber { get; set; }
    public string FirmwareVersion { get; set; } = string.Empty;
    public string HardwareVersion { get; set; } = string.Empty;

    public double CommSuccessRate { get; set; }
    public string CanBaudRate { get; set; } = string.Empty;
}