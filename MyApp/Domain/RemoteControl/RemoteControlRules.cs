namespace MyApp.Domain.RemoteControl;

public static class RemoteControlRules
{
    /// <summary>
    /// Validate if remote control is safe to operate
    /// </summary>
    public static bool IsSafeToOperate(RemoteControlState state)
    {
        return state.RemoteReady && !state.EStop && state.Enable;
    }

    /// <summary>
    /// Validate if movement commands are allowed
    /// </summary>
    public static bool CanMove(RemoteControlState state)
    {
        return IsSafeToOperate(state);
    }

    /// <summary>
    /// Validate joystick input range
    /// </summary>
    public static bool IsValidJoystickInput(float linear, float angular)
    {
        return linear >= -1f && linear <= 1f &&
               angular >= -1f && angular <= 1f;
    }

    /// <summary>
    /// Check if heartbeat is stale (not updated for too long)
    /// </summary>
    public static bool IsHeartbeatStale(RemoteControlState state, TimeSpan maxAge)
    {
        return (DateTime.UtcNow - state.LastUpdated) > maxAge;
    }

    /// <summary>
    /// Validate speed range
    /// </summary>
    public static bool IsValidSpeed(float speed)
    {
        return speed >= 0f && speed <= 1f;
    }

    /// <summary>
    /// Check if E-Stop is triggered
    /// </summary>
    public static bool IsEmergencyStop(RemoteControlState state)
    {
        return state.EStop || state.LostLink;
    }
}