using Lazarus.Data.Configurations;
using Lazarus.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lazarus.Data;

/// <summary>
/// Entity Framework DbContext for Lazarus application data persistence.
/// </summary>
public class LazarusDbContext : DbContext, IAsyncDisposable
{
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="LazarusDbContext"/> class.
    /// Required constructor for DbContext pooling.
    /// </summary>
    /// <param name="options">The options for this context.</param>
    public LazarusDbContext(DbContextOptions<LazarusDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the conversations collection.
    /// </summary>
    public DbSet<Conversation> Conversations => Set<Conversation>();

    /// <summary>
    /// Gets or sets the messages collection.
    /// </summary>
    public DbSet<Message> Messages => Set<Message>();

    /// <summary>
    /// Gets or sets the models collection.
    /// </summary>
    public DbSet<Model> Models => Set<Model>();

    /// <summary>
    /// Gets or sets the settings collection.
    /// </summary>
    public DbSet<Settings> Settings => Set<Settings>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply entity configurations
        modelBuilder.ApplyConfiguration(new ConversationConfiguration());
        modelBuilder.ApplyConfiguration(new MessageConfiguration());
        modelBuilder.ApplyConfiguration(new ModelConfiguration());
        modelBuilder.ApplyConfiguration(new SettingsConfiguration());

        // SQLite-specific configurations
        ConfigureSqliteOptimizations(modelBuilder);
    }

    /// <summary>
    /// Ensures the database is created and migrated to the latest version.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task EnsureDatabaseAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
            // Migration completed successfully
        }
        catch
        {
            // Failed to migrate database
            throw;
        }
    }

    /// <summary>
    /// Executes a raw SQL command asynchronously.
    /// </summary>
    /// <param name="sql">The SQL command to execute.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of rows affected.</returns>
    public async Task<int> ExecuteRawSqlAsync(string sql, CancellationToken cancellationToken = default)
    {
        try
        {
            return await Database.ExecuteSqlRawAsync(sql, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // Failed to execute raw SQL
            throw;
        }
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return await base.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            await base.DisposeAsync().ConfigureAwait(false);
            _disposed = true;
        }
    }

    public override void Dispose()
    {
        if (!_disposed)
        {
            base.Dispose();
            _disposed = true;
        }
    }

    private void ConfigureSqliteOptimizations(ModelBuilder modelBuilder)
    {
        // Configure SQLite-specific optimizations
        // All datetime properties should be stored as TEXT in ISO format
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
                {
                    property.SetColumnType("TEXT");
                }
            }
        }
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.Entity is Settings settings && entry.State == EntityState.Modified)
            {
                settings.LastModified = DateTime.UtcNow;
            }
            else if (entry.Entity is Model model && entry.State == EntityState.Modified)
            {
                model.LastModified = DateTime.UtcNow;
            }
            else if (entry.Entity is Conversation conversation && entry.State == EntityState.Modified)
            {
                conversation.LastMessageAt = DateTime.UtcNow;
            }
        }
    }

}
