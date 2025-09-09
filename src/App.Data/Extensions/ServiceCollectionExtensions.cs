using Lazarus.Data.Repositories;
using Lazarus.Data.Seeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lazarus.Data.Extensions;

/// <summary>
/// Extension methods for configuring data services in the dependency injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Lazarus data services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">Optional custom connection string. If not provided, uses default location.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddLazarusData(this IServiceCollection services, string? connectionString = null)
    {
        // Configure DbContext
        services.AddDbContext<LazarusDbContext>(options =>
        {
            var connString = connectionString ?? GetDefaultConnectionString();

            options.UseSqlite(connString, sqliteOptions =>
            {
                sqliteOptions.CommandTimeout(30);
            });

            options.EnableSensitiveDataLogging(false);
            options.EnableServiceProviderCaching();

            // Enable detailed errors in development
#if DEBUG
            options.EnableDetailedErrors();
#endif
        });

        // Configure repositories
        services.AddScoped<IRepository<Entities.Conversation>, Repository<Entities.Conversation>>();
        services.AddScoped<IRepository<Entities.Message>, Repository<Entities.Message>>();
        services.AddScoped<IRepository<Entities.Model>, Repository<Entities.Model>>();
        services.AddScoped<IRepository<Entities.Settings>, Repository<Entities.Settings>>();
        services.AddScoped<IRepository<Entities.ImageJob>, Repository<Entities.ImageJob>>();

        // Configure specialized repositories
        services.AddScoped<IConversationRepository, ConversationRepository>();
        services.AddScoped<IMessageRepository, MessageRepository>();
        services.AddScoped<IModelRepository, ModelRepository>();
        services.AddScoped<ISettingsRepository, SettingsRepository>();
        services.AddScoped<IImageJobRepository, ImageJobRepository>();
        services.AddScoped<ImageJobRepository>();

        return services;
    }

    /// <summary>
    /// Adds Lazarus data services with a custom DbContext configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureDbContext">Action to configure the DbContext options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddLazarusData(this IServiceCollection services, Action<DbContextOptionsBuilder> configureDbContext)
    {
        // Configure DbContext with custom configuration
        services.AddDbContext<LazarusDbContext>(configureDbContext);

        // Configure repositories
        services.AddScoped<IRepository<Entities.Conversation>, Repository<Entities.Conversation>>();
        services.AddScoped<IRepository<Entities.Message>, Repository<Entities.Message>>();
        services.AddScoped<IRepository<Entities.Model>, Repository<Entities.Model>>();
        services.AddScoped<IRepository<Entities.Settings>, Repository<Entities.Settings>>();
        services.AddScoped<IRepository<Entities.ImageJob>, Repository<Entities.ImageJob>>();

        // Configure specialized repositories
        services.AddScoped<IConversationRepository, ConversationRepository>();
        services.AddScoped<IMessageRepository, MessageRepository>();
        services.AddScoped<IModelRepository, ModelRepository>();
        services.AddScoped<ISettingsRepository, SettingsRepository>();
        services.AddScoped<IImageJobRepository, ImageJobRepository>();
        services.AddScoped<ImageJobRepository>();

        return services;
    }

    /// <summary>
    /// Ensures the database is created and migrated during application startup.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="seedData">Whether to seed default data.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task EnsureDatabaseAsync(this IServiceProvider serviceProvider, bool seedData = true, CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<LazarusDbContext>();
        var logger = scope.ServiceProvider.GetService<ILogger<LazarusDbContext>>();

        try
        {
            await context.EnsureDatabaseAsync(cancellationToken).ConfigureAwait(false);

            if (seedData)
            {
                await DefaultSeedData.SeedAsync(context, cancellationToken).ConfigureAwait(false);
                logger?.LogInformation("Database seeding completed successfully");
            }

            logger?.LogInformation("Database initialization completed successfully");
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to initialize database");
            throw;
        }
    }

    /// <summary>
    /// Optimizes SQLite database settings for performance.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task OptimizeSqliteAsync(this IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<LazarusDbContext>();
        var logger = scope.ServiceProvider.GetService<ILogger<LazarusDbContext>>();

        try
        {
            // Enable SQLite performance optimizations
            await context.ExecuteRawSqlAsync("PRAGMA journal_mode = WAL;", cancellationToken).ConfigureAwait(false);
            await context.ExecuteRawSqlAsync("PRAGMA synchronous = NORMAL;", cancellationToken).ConfigureAwait(false);
            await context.ExecuteRawSqlAsync("PRAGMA cache_size = -64000;", cancellationToken).ConfigureAwait(false); // 64MB cache
            await context.ExecuteRawSqlAsync("PRAGMA temp_store = MEMORY;", cancellationToken).ConfigureAwait(false);
            await context.ExecuteRawSqlAsync("PRAGMA mmap_size = 268435456;", cancellationToken).ConfigureAwait(false); // 256MB mmap

            logger?.LogInformation("SQLite performance optimizations applied successfully");
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Failed to apply SQLite optimizations - continuing with default settings");
        }
    }

    private static string GetDefaultConnectionString()
    {
        var dbPath = Path.Combine(Lazarus.Shared.LazarusPaths.Root, "lazarus.db");
        return $"Data Source={dbPath};Cache=Shared;";
    }
}
