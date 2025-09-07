using Lazarus.Data.Extensions;
using Lazarus.Desktop.Configuration;
using Lazarus.Desktop.Services;
using Lazarus.Desktop.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.IO;
using System.Reflection;

namespace Lazarus.Desktop.Extensions;

/// <summary>
/// Extension methods for configuring services in the dependency injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds all Lazarus Desktop services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddLazarusDesktop(this IServiceCollection services, IConfiguration configuration)
    {
        // Add configuration options
        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));
        services.Configure<OrchestratorOptions>(configuration.GetSection(OrchestratorOptions.SectionName));
        services.Configure<UIOptions>(configuration.GetSection(UIOptions.SectionName));
        services.Configure<BinaryValidationOptions>(configuration.GetSection(BinaryValidationOptions.SectionName));

        // Add core services
        services.AddLazarusCore(configuration);
        services.AddLazarusUI();
        services.AddLazarusViewModels();
        services.AddLazarusBackgroundServices();

        return services;
    }

    /// <summary>
    /// Adds core infrastructure services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddLazarusCore(this IServiceCollection services, IConfiguration configuration)
    {
        // Add logging with Serilog
        services.AddLogging(builder =>
        {
            // Respect fixed disk layout under %LOCALAPPDATA%\Lazarus\logs (text logs)
            var logsDir = Lazarus.Shared.LazarusPaths.FlatLogs;
            var logFilePath = Path.Combine(logsDir, "lazarus-.log");

            var loggerConfig = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .WriteTo.Console(outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}");

            // Only write file logs if the expected directory already exists
            if (Directory.Exists(logsDir))
            {
                loggerConfig = loggerConfig.WriteTo.File(
                    path: logFilePath,
                    rollingInterval: Serilog.RollingInterval.Day,
                    retainedFileCountLimit: 7,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}");
            }

            var logger = loggerConfig.CreateLogger();

            builder.ClearProviders();
            builder.AddSerilog(logger, dispose: true);
        });

        // Orchestrator clients: ensure a single shared instance across view models
        // to keep health state and events consistent in the UI.
        services.AddHttpClient<OrchestratorClient>((serviceProvider, httpClient) =>
        {
            var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<OrchestratorOptions>>().Value;
            httpClient.BaseAddress = new Uri(options.BaseUrl);
            httpClient.Timeout = options.RequestTimeout;
        });
        services.AddSingleton<IOrchestratorClient>(sp => sp.GetRequiredService<OrchestratorClient>());

        services.AddHttpClient<OrchestratorRunnerClient>((serviceProvider, httpClient) =>
        {
            var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<OrchestratorOptions>>().Value;
            httpClient.BaseAddress = new Uri(options.BaseUrl);
            httpClient.Timeout = options.RequestTimeout;
        });
        services.AddSingleton<IOrchestratorRunnerClient>(sp => sp.GetRequiredService<OrchestratorRunnerClient>());

        // Add data layer services with shared path contract for DB location
        var dbPath = Lazarus.Shared.LazarusPaths.DatabaseFile;
        var connectionString = $"Data Source={dbPath};Cache=Shared;";
        services.AddLazarusData(connectionString);

        // Bootstrap filesystem layout (registered for use at startup)
        services.AddSingleton<IFileSystemBootstrapService, FileSystemBootstrapService>();

        return services;
    }

    /// <summary>
    /// Adds UI-specific services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddLazarusUI(this IServiceCollection services)
        {
            // Singleton services for app-wide state
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<IBinaryValidationService, BinaryValidationService>();
        services.AddSingleton<IModelCatalogService, ModelCatalogService>();
        services.AddSingleton<Lazarus.Backend.Services.IModelInventoryService, Lazarus.Backend.Services.ModelInventoryService>();
        services.AddSingleton<Lazarus.Backend.Services.IModelPresetService, Lazarus.Backend.Services.ModelPresetService>();

        // Singleton ViewModelLocator for XAML binding support
        services.AddSingleton<ViewModelLocator>();

        return services;
    }

    /// <summary>
    /// Adds all ViewModels with appropriate lifetimes.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddLazarusViewModels(this IServiceCollection services)
        {
            // Register ViewModels with transient lifetime for fresh instances
            // The ViewModelLocator will manage singleton instances as needed
            services.AddTransient<MainViewModel>();
            services.AddTransient<NavigationViewModel>();
            services.AddTransient<ModelsViewModel>();

        // Auto-register all ViewModels in the assembly
        var assembly = Assembly.GetExecutingAssembly();
        // Auto-register only true ViewModels by naming convention to avoid DI creating helper types
        var viewModelTypes = assembly.GetTypes()
            .Where(type => type.IsClass &&
                          !type.IsAbstract &&
                          type.IsSubclassOf(typeof(ViewModelBase)) &&
                          type.Name.EndsWith("ViewModel", StringComparison.Ordinal) &&
                          type != typeof(MainViewModel) &&
                          type != typeof(NavigationViewModel) &&
                          type != typeof(ModelsViewModel))
            .ToList();

        foreach (var viewModelType in viewModelTypes)
        {
            services.AddTransient(viewModelType);
        }

        return services;
    }

    /// <summary>
    /// Adds background services for async operations.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddLazarusBackgroundServices(this IServiceCollection services)
    {
        // Add hosted services for background operations
        services.AddHostedService<HealthMonitorService>();

        // Auto-start local orchestrator host if not running
        // DEBUG: uses `dotnet run`; RELEASE: tries App.Orchestrator.Host.exe near the app
        services.AddSingleton<IOrchestratorProcessService, OrchestratorProcessService>();
        services.AddHostedService<OrchestratorBootstrapHostedService>();

        return services;
    }

    /// <summary>
    /// Configures the application to run as a Windows service or console application.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddLazarusHosting(this IServiceCollection services)
    {
        services.AddHostedService<ApplicationHostService>();

        return services;
    }
}

/// <summary>
/// Hosted service that manages the WPF application lifecycle.
/// </summary>
internal sealed class ApplicationHostService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHostApplicationLifetime _appLifetime;
    private readonly ILogger<ApplicationHostService> _logger;

    public ApplicationHostService(
        IServiceProvider serviceProvider,
        IHostApplicationLifetime appLifetime,
        ILogger<ApplicationHostService> logger)
    {
        _serviceProvider = serviceProvider;
        _appLifetime = appLifetime;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Lazarus Desktop application");

        _appLifetime.ApplicationStarted.Register(() =>
        {
            Task.Run(async () =>
            {
                try
                {
                    // Ensure first-run filesystem layout exists before DB init
                    var bootstrap = _serviceProvider.GetRequiredService<IFileSystemBootstrapService>();
                    await bootstrap.EnsureLayoutAsync(cancellationToken).ConfigureAwait(false);

                    // Ensure database is ready
                    await _serviceProvider.EnsureDatabaseAsync(cancellationToken: cancellationToken)
                        .ConfigureAwait(false);

                    // Apply SQLite optimizations
                    await _serviceProvider.OptimizeSqliteAsync(cancellationToken: cancellationToken)
                        .ConfigureAwait(false);

                    _logger.LogInformation("Database initialization completed");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to initialize database during startup");
                    _appLifetime.StopApplication();
                }
            }, cancellationToken);
        });

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping Lazarus Desktop application");
        return Task.CompletedTask;
    }
}
