using Microsoft.EntityFrameworkCore;
using MyApp.Domain.Charging;

namespace MyApp.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public DbSet<ChargingState> ChargingStates => Set<ChargingState>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ═════════════════════════════════════════════════════════════
        // CHARGING STATE ENTITY CONFIGURATION
        // ═════════════════════════════════════════════════════════════

        modelBuilder.Entity<ChargingState>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.FirmwareVersion).HasMaxLength(50);
            entity.Property(e => e.HardwareVersion).HasMaxLength(50);

            // Ignore domain events (not persisted)
            entity.Ignore(e => e.DomainEvents);

            entity.HasIndex(e => e.LastUpdated);
        });
    }
}