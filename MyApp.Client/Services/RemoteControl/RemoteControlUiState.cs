using MyApp.Shared.DTOs;
using MyApp.Shared.Enums;

namespace MyApp.Client.Services.RemoteControl;

public class RemoteControlUiState
{
    private RemoteControlDto? _current;

    public RemoteControlDto Current => _current ?? RemoteControlDto.Empty;

    public UiConnectionStatus Status { get; private set; }
        = UiConnectionStatus.Initial;

    public event Action? OnChange;

    public void SetConnecting()
    {
        Status = UiConnectionStatus.Connecting;
        _current = null;
        OnChange?.Invoke();
    }

    public void Update(RemoteControlDto dto)
    {
        _current = dto;
        Status = UiConnectionStatus.Connected;
        OnChange?.Invoke();
    }

    public void SetDisconnected()
    {
        Status = UiConnectionStatus.Disconnected;
        _current = null;
        OnChange?.Invoke();
    }
}