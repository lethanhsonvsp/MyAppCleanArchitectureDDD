// Server/Domain/RemoteControl/RemoteControlState.cs
// Domain State - Business Logic

namespace MyApp.Domain.RemoteControl;

public class RemoteControlState
{
    // ===== System =====
    public int Heartbeat { get; private set; }
    public bool RemoteReady { get; private set; }
    public bool LostLink { get; private set; }
    public bool EStop { get; private set; }
    public bool Enable { get; private set; }
    public string Mode { get; private set; } = "Unknown";

    // ===== Buttons =====
    public bool LiftUp { get; private set; }
    public bool LiftDown { get; private set; }
    public bool RotateLeft { get; private set; }
    public bool RotateRight { get; private set; }
    public bool ModeSelect { get; private set; }

    // ===== Analog Joystick =====
    public float Linear { get; private set; }    // -1 → +1
    public float Angular { get; private set; }   // -1 → +1

    // ===== Speed =====
    public float Speed { get; private set; }     // 0 → 1

    public string Action { get; private set; } = "Idle";

    public DateTime LastUpdated { get; private set; } = DateTime.UtcNow;

    // ===== Domain Methods =====

    /// <summary>
    /// Update state from raw Modbus data
    /// </summary>
    public void UpdateFromModbus(byte[] rawData)
    {
        if (rawData.Length < 8)
            throw new ArgumentException("Invalid data length");

        byte d0 = rawData[0]; // Word0 H
        byte d1 = rawData[1]; // Word0 L
        byte d2 = rawData[2]; // Word1 H
        byte d3 = rawData[3]; // Word1 L
        byte d6 = rawData[6]; // Word3 H (Joystick FB)
        byte d7 = rawData[7]; // Word3 L (Joystick LR)

        // System
        Heartbeat = (d0 >> 4) & 0x0F;
        LostLink = (d0 & 0b0000_0100) != 0;
        RemoteReady = (d0 & 0b0000_0100) == 0;
        EStop = (d0 & 0b0000_0001) != 0;

        // Buttons
        Enable = (d1 & 0b1000_0000) != 0;
        ModeSelect = (d1 & 0b0100_0000) != 0;
        LiftUp = (d1 & 0b0000_0001) != 0;
        LiftDown = (d1 & 0b0000_0010) != 0;
        RotateLeft = (d1 & 0b0000_0100) != 0;
        RotateRight = (d1 & 0b0000_1000) != 0;

        // Speed & Mode
        Speed = Math.Clamp((int)d3, 0, 100) / 100f;
        Mode = DecodeMode(d2);

        // Joystick ANALOG
        Linear = (d6 - 127f) / 127f;
        Angular = (d7 - 127f) / 127f;

        // Apply Safety Rules
        ApplySafetyRules();

        // Build Action String
        Action = BuildActionString();

        LastUpdated = DateTime.UtcNow;
    }

    /// <summary>
    /// Apply safety rules - if not safe, zero velocities
    /// </summary>
    private void ApplySafetyRules()
    {
        if (!RemoteReady || EStop || !Enable)
        {
            Linear = 0;
            Angular = 0;
        }
    }

    private static string DecodeMode(byte d2)
    {
        return (d2 & 0x0F) switch
        {
            0x00 => "Default",
            0x01 => "Maintenance",
            0x02 => "Override",
            _ => "Unknown"
        };
    }

    private string BuildActionString()
    {
        if (!RemoteReady) return "Remote Not Ready";
        if (EStop) return "E-STOP";
        if (!Enable) return "Disabled";

        if (Math.Abs(Linear) > 0.05)
            return Linear > 0 ? "Forward" : "Backward";

        if (Math.Abs(Angular) > 0.05)
            return Angular > 0 ? "Right" : "Left";

        if (LiftUp) return "Lift Up";
        if (LiftDown) return "Lift Down";
        if (RotateLeft) return "Rotate Left";
        if (RotateRight) return "Rotate Right";

        return "Idle";
    }

    // ===== Equality for change detection =====
    public override bool Equals(object? obj)
    {
        if (obj is not RemoteControlState other) return false;

        return Heartbeat == other.Heartbeat &&
               RemoteReady == other.RemoteReady &&
               EStop == other.EStop &&
               Enable == other.Enable &&
               Mode == other.Mode &&
               LiftUp == other.LiftUp &&
               LiftDown == other.LiftDown &&
               RotateLeft == other.RotateLeft &&
               RotateRight == other.RotateRight &&
               ModeSelect == other.ModeSelect &&
               Math.Abs(Linear - other.Linear) < 0.01f &&
               Math.Abs(Angular - other.Angular) < 0.01f &&
               Math.Abs(Speed - other.Speed) < 0.01f;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Heartbeat, RemoteReady, EStop, Enable, Linear, Angular);
    }
}