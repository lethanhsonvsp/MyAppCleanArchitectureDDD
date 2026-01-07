namespace MyApp.Shared.Contracts.Charging;

// ═════════════════════════════════════════════════════════════
// START CHARGING REQUEST
// ═════════════════════════════════════════════════════════════

public class StartChargingRequest
{
    public double Voltage_V { get; set; }
    public double Current_A { get; set; }
}

public class StartChargingResponse
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

// ═════════════════════════════════════════════════════════════
// STOP CHARGING REQUEST
// ═════════════════════════════════════════════════════════════

public class StopChargingRequest
{
    // Empty - no parameters needed
}

public class StopChargingResponse
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

// ═════════════════════════════════════════════════════════════
// CLEAR FAULTS REQUEST
// ═════════════════════════════════════════════════════════════

public class ClearFaultsRequest
{
    // Empty
}

public class ClearFaultsResponse
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}