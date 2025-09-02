using Lazarus.App.Desktop.Extensions;
using Lazarus.App.Desktop.Services;
using Lazarus.App.Desktop.ViewModels;
using Lazarus.App.SDK.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Windows;

namespace Lazarus.App.Desktop;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private IHost? _host;

    /// <summary>
    /// Gets or sets the service provider for dependency injection
    /// </summary>
    public static IServiceProvider? ServiceProvider { get; set; }

    /// <summary>
    /// Gets a service from the dependency injection container
    /// </summary>
    /// <typeparam name="T">The type of service to retrieve</typeparam>
    /// <returns>The service instance</returns>
    public static T GetService<T>() where T : class
    {
        return ServiceProvider?.GetRequiredService<T>() 
            ?? throw new InvalidOperationException("Service provider not initialized");
    }

    /// <inheritdoc />
    protected override void OnStartup(StartupEventArgs e)
    {
        try
        {
            _host = CreateHostBuilder(e.Args).Build();
            ServiceProvider = _host.Services;
            
            // Initialize theme system synchronously
            Task.Run(InitializeThemeSystemAsync).Wait();
            
            // Start the host
            _ = _host.RunAsync();
            
            base.OnStartup(e);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Application failed to start: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    /// <inheritdoc />
    protected override void OnExit(ExitEventArgs e)
    {
        _host?.StopAsync().Wait(TimeSpan.FromSeconds(5));
        _host?.Dispose();
        base.OnExit(e);
    }

    /// <summary>
    /// Creates the host builder with configured services
    /// </summary>
    /// <param name="args">Command line arguments</param>
    /// <returns>Configured host builder</returns>
    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                config.AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", 
                    optional: true, reloadOnChange: true);
                config.AddEnvironmentVariables();
                config.AddCommandLine(args);
            })
            .ConfigureServices((context, services) =>
            {
                // Add logging
                services.AddLogging();

                // Add Lazarus SDK
                services.AddLazarusSDK(context.Configuration);

                // Add desktop services
                services.AddDesktopServices();

                // Add ViewModels
                services.AddViewModels();
            })
            .UseConsoleLifetime();

    /// <summary>
    /// Initializes the theme system asynchronously
    /// </summary>
    private async Task InitializeThemeSystemAsync()
    {
        try
        {
            if (ServiceProvider == null)
                return;

            var logger = ServiceProvider.GetService<ILogger<App>>();
            var userPreferencesService = ServiceProvider.GetService<IUserPreferencesService>();
            
            if (userPreferencesService != null)
            {
                logger?.LogInformation("Initializing theme system");
                
                // Load user preferences
                await userPreferencesService.LoadPreferencesAsync();
                
                // Apply the saved theme preference
                userPreferencesService.ApplyThemePreference();
                
                logger?.LogInformation("Theme system initialized with theme: {Theme}", 
                    userPreferencesService.CurrentTheme);
            }
            else
            {
                logger?.LogWarning("UserPreferencesService not available, using default theme");
            }
        }
        catch (Exception ex)
        {
            var logger = ServiceProvider?.GetService<ILogger<App>>();
            logger?.LogError(ex, "Error initializing theme system");
        }
    }
}