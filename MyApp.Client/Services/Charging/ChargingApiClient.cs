using System.Net.Http.Json;
using MyApp.Shared.Contracts.Charging;
using MyApp.Shared.DTOs;

namespace MyApp.Client.Services.Charging;

/// <summary>
/// REST API Client for Charging
/// </summary>
public class ChargingApiClient
{
    private readonly HttpClient _http;

    public ChargingApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<ChargingStatusDto?> GetStatusAsync()
    {
        return await _http.GetFromJsonAsync<ChargingStatusDto>("api/charging/status");
    }

    public async Task<ChargingStatsDto?> GetStatsAsync()
    {
        return await _http.GetFromJsonAsync<ChargingStatsDto>("api/charging/stats");
    }

    public async Task<bool> StartChargingAsync(double voltage, double current)
    {
        var request = new StartChargingRequest
        {
            Voltage_V = voltage,
            Current_A = current
        };

        var response = await _http.PostAsJsonAsync("api/charging/start", request);
        var result = await response.Content.ReadFromJsonAsync<StartChargingResponse>();

        return result?.Success ?? false;
    }

    public async Task<bool> StopChargingAsync()
    {
        var response = await _http.PostAsJsonAsync("api/charging/stop", new StopChargingRequest());
        var result = await response.Content.ReadFromJsonAsync<StopChargingResponse>();

        return result?.Success ?? false;
    }

    public async Task<bool> ClearFaultsAsync()
    {
        var response = await _http.PostAsJsonAsync("api/charging/clear-faults", new ClearFaultsRequest());
        var result = await response.Content.ReadFromJsonAsync<ClearFaultsResponse>();

        return result?.Success ?? false;
    }
}