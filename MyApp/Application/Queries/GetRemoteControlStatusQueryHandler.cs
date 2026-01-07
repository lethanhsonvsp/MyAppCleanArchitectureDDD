
using MyApp.Domain.RemoteControl;
using MyApp.Shared.DTOs;

namespace MyApp.Application.Queries;

public record GetRemoteControlStatusQuery;

public class GetRemoteControlStatusQueryHandler
{
    private readonly RemoteControlStateCache _cache;

    public GetRemoteControlStatusQueryHandler(RemoteControlStateCache cache)
    {
        _cache = cache;
    }

    public RemoteControlDto? Handle(GetRemoteControlStatusQuery query)
    {
        var state = _cache.GetCurrentState();

        if (state == null) return null;

        // Map Domain → DTO
        return new RemoteControlDto
        {
            Heartbeat = state.Heartbeat,
            RemoteReady = state.RemoteReady,
            LostLink = state.LostLink,
            EStop = state.EStop,
            Enable = state.Enable,
            Mode = state.Mode,
            LiftUp = state.LiftUp,
            LiftDown = state.LiftDown,
            RotateLeft = state.RotateLeft,
            RotateRight = state.RotateRight,
            ModeSelect = state.ModeSelect,
            Linear = state.Linear,
            Angular = state.Angular,
            Speed = state.Speed,
            Action = state.Action,
            Timestamp = state.LastUpdated
        };
    }
}

public class RemoteControlStateCache
{
    private RemoteControlState? _currentState;
    private readonly object _lock = new();

    public void UpdateState(RemoteControlState state)
    {
        lock (_lock)
        {
            _currentState = state;
        }
    }

    public RemoteControlState? GetCurrentState()
    {
        lock (_lock)
        {
            return _currentState;
        }
    }

    public bool HasState => _currentState != null;
}