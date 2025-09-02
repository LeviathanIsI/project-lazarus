using Lazarus.App.Desktop.Services;
using Lazarus.App.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Lazarus.App.Desktop.Extensions;

/// <summary>
/// Extension methods for configuring desktop services in the service collection
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds desktop services to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddDesktopServices(this IServiceCollection services)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        // Add theme management services
        services.AddSingleton<IUserPreferencesService, UserPreferencesService>();
        
        // Add desktop-specific services here
        // For example: dialog services, file services, etc.

        return services;
    }

    /// <summary>
    /// Adds view models to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddViewModels(this IServiceCollection services)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        // Register view models
        services.AddTransient<MainWindowViewModel>();

        return services;
    }
}