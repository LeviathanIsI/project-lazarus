using Lazarus.App.Desktop.Services;
using Lazarus.App.Desktop.ViewModels;
using Lazarus.App.Shared.Services;
using Lazarus.App.Shared.Performance;
using Lazarus.App.Data.Services;
using Lazarus.App.Data.Repositories;
using Lazarus.App.Data.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

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
    /// <param name="configuration">The configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddDesktopServices(this IServiceCollection services, IConfiguration configuration)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        // Add data services (Entity Framework, repositories, etc.)
        services.AddDataServices(configuration);

        // Add theme management services
        services.AddSingleton<IUserPreferencesService, UserPreferencesService>();
        
        // Add navigation services
        services.AddSingleton<INavigationService, NavigationService>();
        
        // Add view mode and system status services
        services.AddSingleton<IViewModeService, ViewModeService>();
        services.AddSingleton<HardwareMonitoringService>();
        services.AddSingleton<OrchestratorHealthService>();
        services.AddSingleton<PerformanceAnalyzer>();
        services.AddSingleton<HardwareInventoryService>();
        services.AddSingleton<ISystemStatusService, SystemStatusService>();
        
        // Add Performance Budgeter services
        services.AddPerformanceBudgeter();
        services.AddSingleton<PerformanceAnalysisService>();
        services.AddHostedService<PerformanceBaselineService>();
        
        // Add directory management services
        services.AddSingleton<IDirectoryService, DirectoryService>();
        
        // Add chat services
        services.AddSingleton<IChatService, ChatService>();
        
        // Add Asset Management services (Asset-Keeper) - AssetKeeperService is registered in AddDataServices as singleton
        // IModelScannerService now registered as singleton in AddDataServices to align with DbContext lifecycle
        
        // Add infrastructure services
        services.AddSingleton<RunnerProcessService>();
        services.AddSingleton<OrchestratorHostService>();
        services.AddSingleton<InfrastructureStartupService>();
        services.AddHostedService(provider => provider.GetRequiredService<InfrastructureStartupService>());
        
        // Register main window
        services.AddTransient<MainWindow>();
        
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

        // Register main view model
        services.AddTransient<MainWindowViewModel>();

        // Register navigation view models as SINGLETON to preserve state across navigation
        // This prevents model state corruption during tab switching
        services.AddSingleton<DashboardViewModel>();
        services.AddSingleton<ConversationsViewModel>();
        services.AddSingleton<ModelConfigurationViewModel>();
        services.AddSingleton<RunnerManagerViewModel>();
        services.AddSingleton<JobsViewModel>();
        services.AddSingleton<DatasetsViewModel>();
        services.AddSingleton<ImagesViewModel>();
        services.AddSingleton<VideoViewModel>();
        services.AddSingleton<VoiceViewModel>();
        services.AddSingleton<ThreeDModelsViewModel>();
        services.AddSingleton<EntitiesViewModel>();
        services.AddSingleton<TrainingViewModel>();

        return services;
    }
}