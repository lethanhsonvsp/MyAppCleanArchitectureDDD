using Microsoft.EntityFrameworkCore;
using MyApp.Application.Repository;
using MyApp.Domain.Charging;
using System;

namespace MyApp.Infrastructure.Persistence;

/// <summary>
/// Charging Repository Implementation
/// NOTE: Charging is typically a streaming/real-time module,
/// so persistence is optional or minimal (only for historical data)
/// </summary>
public class ChargingRepository : IChargingRepository
{
    private readonly AppDbContext _db;

    // In-memory cache for active state (performance optimization)
    private ChargingState? _activeState;
    private readonly object _lock = new();

    public ChargingRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<ChargingState?> GetActiveAsync(CancellationToken ct = default)
    {
        lock (_lock)
        {
            // Return cached state if available
            if (_activeState != null)
                return Task.FromResult<ChargingState?>(_activeState);
        }

        // Otherwise try to load from DB (or create new)
        return Task.FromResult<ChargingState?>(ChargingState.Create());
    }

    public async Task<ChargingState?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.ChargingStates
            .FirstOrDefaultAsync(s => s.Id == id, ct);
    }

    public Task SaveAsync(ChargingState state, CancellationToken ct = default)
    {
        lock (_lock)
        {
            _activeState = state;
        }

        // Optionally persist to DB for historical data
        // For real-time streaming, you might skip DB writes entirely
        // and only use in-memory state

        return Task.CompletedTask;
    }
}