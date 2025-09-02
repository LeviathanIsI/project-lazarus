using Lazarus.App.SDK.Configuration;
using Lazarus.App.Shared.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lazarus.App.SDK.Extensions;

/// <summary>
/// Extension methods for configuring SDK services in the service collection
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Lazarus SDK services to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddLazarusSDK(this IServiceCollection services, IConfiguration configuration)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));
        
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        // Configure options
        services.Configure<LazarusApiOptions>(
            configuration.GetSection(LazarusApiOptions.SectionName));

        // Add HTTP client with typed client
        services.AddHttpClient<LazarusApiClient>();

        // Register the API client as ITrainingService
        services.AddScoped<ITrainingService, LazarusApiClient>();

        return services;
    }

    /// <summary>
    /// Adds Lazarus SDK services to the service collection with custom configuration
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configureOptions">Action to configure the API options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddLazarusSDK(this IServiceCollection services, Action<LazarusApiOptions> configureOptions)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));
        
        if (configureOptions == null)
            throw new ArgumentNullException(nameof(configureOptions));

        // Configure options
        services.Configure(configureOptions);

        // Add HTTP client with typed client
        services.AddHttpClient<LazarusApiClient>();

        // Register the API client as ITrainingService
        services.AddScoped<ITrainingService, LazarusApiClient>();

        return services;
    }
}