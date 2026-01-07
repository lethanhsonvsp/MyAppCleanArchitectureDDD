namespace MyApp.Domain.Charging;

/// <summary>
/// Domain Business Rules for Charging
/// </summary>
public static class ChargingRules
{
    // ═════════════════════════════════════════════════════════════
    // VOLTAGE LIMITS
    // ═════════════════════════════════════════════════════════════

    public const double MIN_VOLTAGE_V = 0;
    public const double MAX_VOLTAGE_V = 1000; // 20-bit = 1,048,575 * 0.001 V

    public static bool IsVoltageValid(double voltage)
        => voltage >= MIN_VOLTAGE_V && voltage <= MAX_VOLTAGE_V;

    // ═════════════════════════════════════════════════════════════
    // CURRENT LIMITS
    // ═════════════════════════════════════════════════════════════

    public const double MIN_CURRENT_A = 0;
    public const double MAX_CURRENT_A = 262; // 18-bit = 262,143 * 0.001 A

    public static bool IsCurrentValid(double current)
        => current >= MIN_CURRENT_A && current <= MAX_CURRENT_A;

    // ═════════════════════════════════════════════════════════════
    // TEMPERATURE LIMITS
    // ═════════════════════════════════════════════════════════════

    public const double OVERTEMP_WARNING_C = 85;
    public const double OVERTEMP_CRITICAL_C = 95;

    public static bool IsOvertemperature(double temp)
        => temp > OVERTEMP_WARNING_C;

    public static bool IsCriticalTemperature(double temp)
        => temp > OVERTEMP_CRITICAL_C;

    // ═════════════════════════════════════════════════════════════
    // COMMAND VALIDATION
    // ═════════════════════════════════════════════════════════════

    public static (bool IsValid, string? Error) ValidateCommand(
        double voltage,
        double current,
        bool powerStage1)
    {
        if (!IsVoltageValid(voltage))
            return (false, $"Voltage must be between {MIN_VOLTAGE_V}V and {MAX_VOLTAGE_V}V");

        if (!IsCurrentValid(current))
            return (false, $"Current must be between {MIN_CURRENT_A}A and {MAX_CURRENT_A}A");

        if ((voltage > 0 || current > 0) && !powerStage1)
            return (false, "PowerStage1 must be enabled when requesting power");

        return (true, null);
    }

    // ═════════════════════════════════════════════════════════════
    // SAFETY RULES
    // ═════════════════════════════════════════════════════════════

    public static bool CanStartCharging(ChargingState state)
    {
        if (state.HasFault)
            return false;

        if (state.State == ChargerStateEnum.Fault)
            return false;

        if (IsOvertemperature(state.SecondaryTemp_C) || IsOvertemperature(state.PrimaryTemp_C))
            return false;

        return true;
    }

    public static bool MustStopCharging(ChargingState state)
    {
        if (state.HasFault)
            return true;

        if (IsCriticalTemperature(state.SecondaryTemp_C) || IsCriticalTemperature(state.PrimaryTemp_C))
            return true;

        return false;
    }
}