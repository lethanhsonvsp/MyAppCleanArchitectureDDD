using Microsoft.EntityFrameworkCore;
using MyApp.Application.Charging;
using MyApp.Application.Repository;
using MyApp.Domain.Charging;

namespace MyApp.Infrastructure.Persistence;

/// <summary>
/// Charging Repository Implementation
/// NOTE: Charging is a real-time streaming module,
/// so we use in-memory cache (no DB writes on every CAN frame)
/// </summary>
public class ChargingRepository : IChargingRepository
{
    // ✅ SINGLETON: In-memory cache for active state
    private ChargingState? _activeState;
    private readonly object _lock = new();

    public Task<ChargingState?> GetActiveAsync(CancellationToken ct = default)
    {
        lock (_lock)
        {
            // Return cached state or create new
            if (_activeState == null)
            {
                _activeState = ChargingState.Create();
            }

            return Task.FromResult<ChargingState?>(_activeState);
        }
    }

    public Task<ChargingState?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        lock (_lock)
        {
            if (_activeState?.Id == id)
                return Task.FromResult<ChargingState?>(_activeState);

            return Task.FromResult<ChargingState?>(null);
        }
    }

    public Task SaveAsync(ChargingState state, CancellationToken ct = default)
    {
        lock (_lock)
        {
            _activeState = state;
        }

        // ✅ NO DB WRITES: Real-time data doesn't need persistence
        // If you need historical data, write to DB in background job instead
        return Task.CompletedTask;
    }
}