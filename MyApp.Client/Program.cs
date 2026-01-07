using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MyApp.Client.Services.Charging;
using MyApp.Client.Services.RemoteControl;
var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Services.AddSingleton<RemoteControlUiState>();
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:7000";
builder.Services.AddScoped<RemoteControlSignalRClient>(sp =>
{
    return new RemoteControlSignalRClient(apiBaseUrl);
});
builder.Services.AddHttpClient<RemoteControlApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
});
builder.Services.AddSingleton<ChargingUiState>();

builder.Services.AddScoped<ChargingSignalRClient>(sp =>
{
    var uiState = sp.GetRequiredService<ChargingUiState>();
    return new ChargingSignalRClient(apiBaseUrl, uiState);
});


builder.Services.AddHttpClient<ChargingApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
});
// ===== Charging SERVICES =====


await builder.Build().RunAsync();
