using Lazarus.App.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Lazarus.App.Data;

/// <summary>
/// Entity Framework database context for the Lazarus application
/// </summary>
public class LazarusDbContext : DbContext
{
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

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure TrainingSession entity
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

            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.IsDeleted);

            // Global query filter to exclude soft-deleted entities
            entity.HasQueryFilter(e => !e.IsDeleted);
        });
    }

    /// <inheritdoc />
    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    /// <inheritdoc />
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
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
}