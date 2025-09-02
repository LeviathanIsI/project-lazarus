using Lazarus.App.Data.Repositories;
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

        // Add Entity Framework with SQLite
        var connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? "Data Source=lazarus.db";

        services.AddDbContext<LazarusDbContext>(options =>
        {
            options.UseSqlite(connectionString);
            options.EnableSensitiveDataLogging(false);
            options.EnableServiceProviderCaching();
        });

        // Add repositories
        services.AddScoped<ITrainingSessionRepository, TrainingSessionRepository>();

        return services;
    }

    /// <summary>
    /// Ensures the database is created and migrations are applied
    /// </summary>
    /// <param name="serviceProvider">The service provider</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public static async Task EnsureDatabaseCreatedAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<LazarusDbContext>();
        
        await context.Database.EnsureCreatedAsync();
    }
}