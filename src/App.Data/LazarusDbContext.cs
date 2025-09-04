using Lazarus.App.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Lazarus.App.Data;

/// <summary>
/// Entity Framework database context for the Lazarus application
/// Thread-safe with proper disposal patterns and connection management
/// </summary>
public class LazarusDbContext : DbContext
{
    private bool _disposed;
    private readonly object _lockObject = new object();

    /// <summary>
    /// Initializes a new instance of the <see cref="LazarusDbContext"/> class
    /// </summary>
    /// <param name="options">The database context options</param>
    public LazarusDbContext(DbContextOptions<LazarusDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the training sessions dataset
    /// </summary>
    public DbSet<TrainingSession> TrainingSessions { get; set; } = null!;

    /// <summary>
    /// Gets or sets the LLM assets dataset
    /// </summary>
    public DbSet<LlmAsset> LlmAssets { get; set; } = null!;

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure TrainingSession entity with navigation-safe indices
        modelBuilder.Entity<TrainingSession>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(256);

            entity.Property(e => e.Description)
                .HasMaxLength(1024);

            entity.Property(e => e.Status)
                .HasConversion<string>();

            entity.Property(e => e.Progress)
                .HasPrecision(5, 2);

            entity.Property(e => e.ConfigurationJson)
                .HasColumnType("TEXT");

            // Navigation-optimized indices for persistent entity tracking
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.IsDeleted);

            // Global query filter to exclude soft-deleted entities
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Configure LlmAsset entity
        modelBuilder.Entity<LlmAsset>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(256);

            entity.Property(e => e.FilePath)
                .IsRequired()
                .HasMaxLength(1024);

            entity.Property(e => e.FileHash)
                .IsRequired()
                .HasMaxLength(64);

            entity.Property(e => e.AssetType)
                .HasConversion<string>();

            entity.Property(e => e.Status)
                .HasConversion<string>();

            entity.Property(e => e.QuantizationFormat)
                .HasMaxLength(32);

            entity.Property(e => e.ParameterCount)
                .HasMaxLength(16);

            entity.Property(e => e.VramEstimateGb)
                .HasPrecision(8, 2);

            entity.Property(e => e.Architecture)
                .HasMaxLength(64);

            entity.Property(e => e.CompatibleRunners)
                .HasMaxLength(512);

            entity.Property(e => e.ActiveRunnerId)
                .HasMaxLength(128);

            entity.Property(e => e.MetadataJson)
                .HasColumnType("TEXT");

            entity.Property(e => e.Description)
                .HasMaxLength(1024);

            entity.Property(e => e.ValidationResult)
                .HasMaxLength(2048);

            // Indexes for performance
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.FilePath).IsUnique();
            entity.HasIndex(e => e.FileHash);
            entity.HasIndex(e => e.AssetType);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Architecture);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.IsDeleted);

            // Global query filter to exclude soft-deleted entities
            entity.HasQueryFilter(e => !e.IsDeleted);
        });
    }

    /// <inheritdoc />
    public override int SaveChanges()
    {
        lock (_lockObject)
        {
            ThrowIfDisposed();
            UpdateTimestamps();
            return base.SaveChanges();
        }
    }

    /// <inheritdoc />
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        
        // Use ConfigureAwait(false) to prevent deadlocks
        UpdateTimestamps();
        return await base.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Updates the timestamps for entities that are being added or modified
    /// </summary>
    private void UpdateTimestamps()
    {
        var entries = ChangeTracker
            .Entries()
            .Where(e => e.Entity is BaseEntity && (
                e.State == EntityState.Added || 
                e.State == EntityState.Modified));

        foreach (var entityEntry in entries)
        {
            var entity = (BaseEntity)entityEntry.Entity;

            if (entityEntry.State == EntityState.Added)
            {
                entity.CreatedAt = DateTimeOffset.UtcNow;
            }
            else if (entityEntry.State == EntityState.Modified)
            {
                entity.UpdatedAt = DateTimeOffset.UtcNow;
            }
        }
    }

    /// <summary>
    /// NAVIGATION SAFETY: Clears tracked entity phantoms to prevent navigation-induced duplicates
    /// Call this method when switching between navigation contexts to eliminate phantom entities
    /// </summary>
    public void ClearEntityTrackingPhantoms()
    {
        try
        {
            var phantomEntries = ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Detached || e.State == EntityState.Unchanged)
                .ToList();

            foreach (var entry in phantomEntries)
            {
                if (entry.Entity is LlmAsset)
                {
                    entry.State = EntityState.Detached; // Detach phantom LlmAsset entities
                }
            }

            // Clear all change tracking to prevent phantom spawning
            ChangeTracker.Clear();
        }
        catch (Exception ex)
        {
            // Log but don't throw - phantom clearing is defensive
            System.Diagnostics.Debug.WriteLine($"Phantom clearing warning: {ex.Message}");
        }
    }

    /// <summary>
    /// NAVIGATION SAFETY: Gets count of tracked entities for diagnostic purposes
    /// </summary>
    public int GetTrackedEntityCount()
    {
        return ChangeTracker.Entries().Count();
    }

    /// <summary>
    /// Throws ObjectDisposedException if the context has been disposed
    /// </summary>
    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(LazarusDbContext));
        }
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        if (!_disposed)
        {
            lock (_lockObject)
            {
                if (!_disposed)
                {
                    _disposed = true;
                }
            }
        }
        
        base.Dispose();
    }

    /// <inheritdoc />
    public override async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            _disposed = true;
        }
        
        await base.DisposeAsync().ConfigureAwait(false);
    }
}