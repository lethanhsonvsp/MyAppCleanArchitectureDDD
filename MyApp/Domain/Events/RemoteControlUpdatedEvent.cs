// Server/Domain/Events/RemoteControlUpdatedEvent.cs
// Domain Event

namespace MyApp.Domain.Events;

public record RemoteControlUpdatedEvent
{
    public int Heartbeat { get; init; }
    public bool RemoteReady { get; init; }
    public bool EStop { get; init; }
    public bool Enable { get; init; }
    public string Mode { get; init; } = "Unknown";

    public float Linear { get; init; }
    public float Angular { get; init; }
    public float Speed { get; init; }

    public string Action { get; init; } = "Idle";
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    // Factory method to create from state
    public static RemoteControlUpdatedEvent FromState(Domain.RemoteControl.RemoteControlState state)
    {
        return new RemoteControlUpdatedEvent
        {
            Heartbeat = state.Heartbeat,
            RemoteReady = state.RemoteReady,
            EStop = state.EStop,
            Enable = state.Enable,
            Mode = state.Mode,
            Linear = state.Linear,
            Angular = state.Angular,
            Speed = state.Speed,
            Action = state.Action,
            Timestamp = state.LastUpdated
        };
    }
}