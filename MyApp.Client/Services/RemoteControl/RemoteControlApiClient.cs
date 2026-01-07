using MyApp.Shared.DTOs;
using System.Net.Http.Json;

namespace MyApp.Client.Services.RemoteControl;

public class RemoteControlApiClient
{
    private readonly HttpClient _http;

    public RemoteControlApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<RemoteControlDto?> GetStatusAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<RemoteControlDto>("api/remotecontrol/status");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RemoteControlApiClient] Error: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> CheckHealthAsync()
    {
        try
        {
            var response = await _http.GetAsync("api/remotecontrol/health");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}