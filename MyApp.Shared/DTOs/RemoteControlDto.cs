namespace MyApp.Shared.DTOs;

public partial record RemoteControlDto
{
    public int Heartbeat { get; init; }
    public bool RemoteReady { get; init; }
    public bool LostLink { get; init; }
    public bool EStop { get; init; }
    public bool Enable { get; init; }
    public string Mode { get; init; } = "Unknown";

    public bool LiftUp { get; init; }
    public bool LiftDown { get; init; }
    public bool RotateLeft { get; init; }
    public bool RotateRight { get; init; }
    public bool ModeSelect { get; init; }

    public float Linear { get; init; }
    public float Angular { get; init; }
    public float Speed { get; init; }

    public string Action { get; init; } = "Idle";
    public DateTime Timestamp { get; init; }
}
