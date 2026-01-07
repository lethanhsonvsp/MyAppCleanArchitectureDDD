using Microsoft.AspNetCore.SignalR.Client;

namespace MyApp.Client.Services.Charging;

/// <summary>
/// SignalR Client for real-time Charging updates
/// </summary>
public class ChargingSignalRClient : IAsyncDisposable
{
    private readonly HubConnection _connection;
    private readonly ChargingUiState? _uiState;

    public ChargingSignalRClient(string apiBaseUrl)
    {
        _connection = new HubConnectionBuilder()
            .WithUrl($"{apiBaseUrl}/hubs/charging")
            .WithAutomaticReconnect()
            .Build();

        RegisterHandlers();
    }

    private void RegisterHandlers()
    {
        _connection.On<ChargingStatusUpdate>("ChargingStatusChanged", update =>
        {
            _uiState!.IsCharging = update.IsCharging;
            _uiState.NotifyChange();
        });

        _connection.On<ChargingFaultUpdate>("ChargingFault", fault =>
        {
            _uiState!.HasFault = true;
            _uiState.HasOcp = fault.Ocp;
            _uiState.HasOvp = fault.Ovp;
            _uiState.HasWatchdogFault = fault.Watchdog;
            _uiState.NotifyChange();
        });

        _connection.On<ChargingWarningUpdate>("ChargingWarning", warning =>
        {
            Console.WriteLine($"⚠️ WARNING: {warning.Message}");
            _uiState!.NotifyChange();
        });
    }

    public async Task StartAsync()
    {
        if (_connection.State == HubConnectionState.Disconnected)
        {
            await _connection.StartAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync();
    }

    // ═════════════════════════════════════════════════════════════
    // SIGNALR MESSAGE TYPES
    // ═════════════════════════════════════════════════════════════

    private record ChargingStatusUpdate(Guid ChargingId, bool IsCharging, DateTime Timestamp);
    private record ChargingFaultUpdate(Guid ChargingId, bool Ocp, bool Ovp, bool Watchdog, DateTime Timestamp);
    private record ChargingWarningUpdate(Guid ChargingId, string Message, DateTime Timestamp);
}