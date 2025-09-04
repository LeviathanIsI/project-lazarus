using Lazarus.App.Data.Repositories;
using Lazarus.App.Data.Services;
using Lazarus.App.Data.Threading;
using Lazarus.App.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lazarus.App.Data.Extensions;

/// <summary>
/// Extension methods for configuring data services in the service collection
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds data services to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddDataServices(this IServiceCollection services, IConfiguration configuration)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));
        
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        // Add Entity Framework with SQLite - FIXED: Singleton for navigation persistence
        var connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? "Data Source=lazarus.db";

        // CRITICAL FIX: Change from Scoped to Singleton to prevent phantom model spawning
        services.AddDbContext<LazarusDbContext>(options =>
        {
            options.UseSqlite(connectionString, sqliteOptions =>
            {
                sqliteOptions.CommandTimeout(30); // 30 second timeout for long operations
            });
            
            // Enhanced configuration for singleton lifetime
            options.EnableSensitiveDataLogging(false);
            options.EnableServiceProviderCaching(true); // Enable for singleton
            options.EnableDetailedErrors(false); // Disable in production for security
            
            // Configure connection pooling for persistent entity tracking
            options.LogTo(message => System.Diagnostics.Debug.WriteLine(message), 
                Microsoft.Extensions.Logging.LogLevel.Warning);
        }, ServiceLifetime.Singleton); // FIXED: Singleton lifetime prevents navigation-induced phantom spawning

        // CRITICAL FIX: Repository pattern aligned with singleton DbContext
        services.AddSingleton<ITrainingSessionRepository, TrainingSessionRepository>();
        services.AddSingleton<ILlmAssetRepository, LlmAssetRepository>();
        
        // CRITICAL FIX: Services aligned with singleton DbContext for persistent entity tracking
        services.AddSingleton<IAssetKeeperService, AssetKeeperService>();
        services.AddSingleton<IAssetRegistryPurificationService, AssetRegistryPurificationService>();
        services.AddSingleton<IModelScannerService, ModelScannerService>();
        
        // Add thread-safe database connection management (keep as singleton)
        services.AddSingleton<ThreadSafeDbContextFactory>();
        
        // Add database health monitoring
        services.AddHostedService<DatabaseConnectionHealthMonitor>();
        
        // Add database exorcism startup service for phantom elimination
        services.AddHostedService<DatabaseExorcismStartupService>();

        return services;
    }

    /// <summary>
    /// Ensures the database is created and migrations are applied with proper threading discipline
    /// </summary>
    /// <param name="serviceProvider">The service provider</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public static async Task EnsureDatabaseCreatedAsync(this IServiceProvider serviceProvider, 
        CancellationToken cancellationToken = default)
    {
        // FIXED: Use singleton scope for persistent database context
        var context = serviceProvider.GetRequiredService<LazarusDbContext>();
        
        try
        {
            // Apply pending migrations with proper async disposal and timeout handling
            await context.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException("Database migration was cancelled", ex, cancellationToken);
        }
        // Let other exceptions bubble up for proper error handling
    }
}