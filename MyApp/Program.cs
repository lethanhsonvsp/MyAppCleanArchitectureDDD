using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using MyApp.Application.Abstractions;
using MyApp.Application.Charging.EventHandlers;
using MyApp.Application.EventHandlers;
using MyApp.Application.Queries;
using MyApp.Application.Repository;
using MyApp.Components;
using MyApp.Hubs;
using MyApp.Infrastructure.Hardware;
using MyApp.Infrastructure.Industrial.CAN;
using MyApp.Infrastructure.Industrial.CAN.Adapters;
using MyApp.Infrastructure.Messaging;
using MyApp.Infrastructure.Messaging.SignalR;
using MyApp.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMudServices();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();
// Add services
builder.Services.AddControllers();
builder.Services.AddSignalR();

// ===== DOMAIN & APPLICATION =====
builder.Services.AddSingleton<RemoteControlStateCache>();
builder.Services.AddScoped<GetRemoteControlStatusQueryHandler>();

// ===== INFRASTRUCTURE =====
// Background Service - Hardware polling
builder.Services.AddSingleton<RemoteControlHardwareService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<RemoteControlHardwareService>());
// SignalR Publisher
builder.Services.AddSingleton<RemoteControlSignalRPublisher>();

builder.Services.AddSingleton<MyApp.Client.Services.RemoteControl.RemoteControlUiState>();
// ===== CLIENT SERVICES (for Interactive Server components) =====
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:7000";

builder.Services.AddScoped<MyApp.Client.Services.RemoteControl.RemoteControlSignalRClient>(sp =>
{
    return new MyApp.Client.Services.RemoteControl.RemoteControlSignalRClient(apiBaseUrl);
});

builder.Services.AddSingleton<MyApp.Client.Services.Charging.ChargingUiState>();
builder.Services.AddHttpClient<MyApp.Client.Services.RemoteControl.RemoteControlApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
});
builder.Services.AddHostedService<RemoteControlEventHandler>();

builder.Services.AddScoped<MyApp.Client.Services.Charging.ChargingSignalRClient>(sp =>
{
    var uiState = sp.GetRequiredService<MyApp.Client.Services.Charging.ChargingUiState>();
    return new MyApp.Client.Services.Charging.ChargingSignalRClient(apiBaseUrl, uiState);
});


// Đăng ký HttpClient với BaseAddress từ configuration
builder.Services.AddHttpClient<MyApp.Client.Services.Charging.ChargingApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
});

// MediatR (CQRS)
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
});

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=myapp.db"));


builder.Services.AddSingleton(sp =>
{
    var canInterface = builder.Configuration["CAN:Interface"] ?? "can0";
    return new SocketCan(canInterface);
});

builder.Services.AddSingleton<CanChargingAdapter>();
builder.Services.AddSingleton<ICanCommandSender>(sp => sp.GetRequiredService<CanChargingAdapter>());

// Start CAN reader
builder.Services.AddHostedService<CanReaderBackgroundService>();


builder.Services.AddScoped<IChargingRepository, ChargingRepository>();
builder.Services.AddScoped<IMessageBus, InMemoryMessageBus>();
builder.Services.AddSingleton<ISignalRPublisher, ChargingSignalRPublisher>();


builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});
var app = builder.Build();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(MyApp.Client._Imports).Assembly);
app.MapControllers();
app.MapHub<RemoteControlHub>("/hubs/remotecontrol");
app.MapHub<ChargingHub>("/hubs/charging");

app.Run();
