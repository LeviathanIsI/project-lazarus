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
    private readonly ILogger<LazarusDbContext>? _logger;
    private readonly string _connectionString;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="LazarusDbContext"/> class.
    /// </summary>
    /// <param name="options">The options for this context.</param>
    /// <param name="logger">Optional logger for database operations.</param>
    public LazarusDbContext(DbContextOptions<LazarusDbContext> options, ILogger<LazarusDbContext>? logger = null)
        : base(options)
    {
        _logger = logger;
        _connectionString = Database.GetConnectionString() ?? GetDefaultConnectionString();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LazarusDbContext"/> class with connection string.
    /// </summary>
    /// <param name="connectionString">The SQLite connection string.</param>
    /// <param name="logger">Optional logger for database operations.</param>
    public LazarusDbContext(string connectionString, ILogger<LazarusDbContext>? logger = null)
        : base()
    {
        _logger = logger;
        _connectionString = connectionString;
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

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var connectionString = !string.IsNullOrEmpty(_connectionString)
                ? _connectionString
                : GetDefaultConnectionString();

            optionsBuilder.UseSqlite(connectionString, options =>
            {
                options.CommandTimeout(30);
            });
        }

        // Performance optimizations
        optionsBuilder.EnableSensitiveDataLogging(false);
        optionsBuilder.EnableServiceProviderCaching();
        optionsBuilder.EnableDetailedErrors(IsDevelopment());

        // Configure logging level based on environment
        if (_logger is not null)
        {
            var logLevel = IsDevelopment() ? LogLevel.Information : LogLevel.Warning;
            optionsBuilder.LogTo(message => _logger.Log(logLevel, "{Message}", message));
        }
    }

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
            _logger?.LogInformation("Database migration completed successfully");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to migrate database");
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
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to execute raw SQL: {Sql}", sql);
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

    private static string GetDefaultConnectionString()
    {
        var dbPath = Lazarus.Shared.LazarusPaths.DatabaseFile;
        return $"Data Source={dbPath};Cache=Shared;";
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

    private static bool IsDevelopment()
    {
#if DEBUG
        return true;
#else
        return false;
#endif
    }
}
