namespace MyApp.Client.Services.Charging;

/// <summary>
/// Charging UI State Management (Singleton)
/// </summary>
public class ChargingUiState
{
    // ═════════════════════════════════════════════════════════════
    // POWER
    // ═════════════════════════════════════════════════════════════

    public double Voltage_V { get; set; }
    public double Current_A { get; set; }
    public bool IsCharging { get; set; }

    // ═════════════════════════════════════════════════════════════
    // STATUS
    // ═════════════════════════════════════════════════════════════

    public string State { get; set; } = "Uninit";
    public bool HasFault { get; set; }
    public bool HasOcp { get; set; }
    public bool HasOvp { get; set; }
    public bool HasWatchdogFault { get; set; }

    // ═════════════════════════════════════════════════════════════
    // AC INPUT
    // ═════════════════════════════════════════════════════════════

    public double AcVoltage_V { get; set; }
    public double AcCurrent_A { get; set; }
    public double AcFrequency_Hz { get; set; }

    // ═════════════════════════════════════════════════════════════
    // WIRELESS
    // ═════════════════════════════════════════════════════════════

    public double WirelessEfficiency_Pct { get; set; }
    public int WirelessGap_Mm { get; set; }
    public bool WirelessOk { get; set; }

    // ═════════════════════════════════════════════════════════════
    // TEMPERATURE
    // ═════════════════════════════════════════════════════════════

    public double SecondaryTemp_C { get; set; }
    public double PrimaryTemp_C { get; set; }

    // ═════════════════════════════════════════════════════════════
    // EVENTS
    // ═════════════════════════════════════════════════════════════

    public event Action? OnChange;

    public void NotifyChange()
    {
        OnChange?.Invoke();
    }

    // ═════════════════════════════════════════════════════════════
    // POLLING (Fallback if SignalR fails)
    // ═════════════════════════════════════════════════════════════

    public async Task StartPolling(ChargingApiClient apiClient, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var status = await apiClient.GetStatusAsync();
                if (status != null)
                {
                    UpdateFromDto(status);
                    NotifyChange();
                }
            }
            catch
            {
                // Ignore polling errors
            }

            await Task.Delay(500, ct); // 2 Hz
        }
    }


    public void UpdateFromDto(MyApp.Shared.DTOs.ChargingStatusDto dto)
    {
        Voltage_V = dto.Voltage_V;
        Current_A = dto.Current_A;
        IsCharging = dto.IsCharging;

        State = dto.State;
        HasFault = dto.HasFault;
        HasOcp = dto.HasOcp;
        HasOvp = dto.HasOvp;
        HasWatchdogFault = dto.HasWatchdogFault;

        AcVoltage_V = dto.AcVoltage_V;
        AcCurrent_A = dto.AcCurrent_A;
        AcFrequency_Hz = dto.AcFrequency_Hz;

        WirelessEfficiency_Pct = dto.WirelessEfficiency_Pct;
        WirelessGap_Mm = dto.WirelessGap_Mm;
        WirelessOk = dto.WirelessOk;

        SecondaryTemp_C = dto.SecondaryTemp_C;
        PrimaryTemp_C = dto.PrimaryTemp_C;
    }
}