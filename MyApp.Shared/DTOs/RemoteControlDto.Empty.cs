namespace MyApp.Shared.DTOs;

public partial record RemoteControlDto
{
    public static RemoteControlDto Empty => new()
    {
        Heartbeat = 0,
        RemoteReady = false,
        LostLink = true,
        EStop = false,
        Enable = false,
        Mode = "Unknown",

        LiftUp = false,
        LiftDown = false,
        RotateLeft = false,
        RotateRight = false,
        ModeSelect = false,

        Linear = 0,
        Angular = 0,
        Speed = 0,

        Action = "No Data",
        Timestamp = DateTime.MinValue
    };
}
