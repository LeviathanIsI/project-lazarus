using Lazarus.App.Desktop.Extensions;
using Lazarus.App.Desktop.Services;
using Lazarus.App.Data.Extensions;
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
    private IServiceProvider? _serviceProvider;
    /// <inheritdoc />
    protected override async void OnStartup(StartupEventArgs e)
    {
        try
        {
            // Build and configure host with dependency injection
            _host = CreateHost();
            
            // Start the host (this starts hosted services like InfrastructureStartupService)
            await _host.StartAsync();
            
            _serviceProvider = _host.Services;
            
            // Initialize theme system
            InitializeTheme();
            
            // Start services
            StartServicesAsync();
            
            // Initialize navigation service after DI container is built
            var navigationService = _serviceProvider.GetRequiredService<Lazarus.App.Desktop.Services.INavigationService>();
            navigationService.Initialize();
            
            // Create and show main window
            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
            System.Diagnostics.Debug.WriteLine("Main window shown");
            
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
        try
        {
            // Ensure infrastructure services are properly stopped before exit
            if (_serviceProvider != null)
            {
                var infrastructureService = _serviceProvider.GetService<InfrastructureStartupService>();
                if (infrastructureService?.IsStarted == true)
                {
                    // Give infrastructure services time to shut down gracefully
                    var shutdownTask = infrastructureService.StopAsync();
                    if (!shutdownTask.Wait(10000)) // 10 second timeout
                    {
                        System.Diagnostics.Debug.WriteLine("Infrastructure shutdown timed out during application exit");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error during infrastructure shutdown: {ex.Message}");
        }
        finally
        {
            // Stop the host which will stop all hosted services
            if (_host != null)
            {
                var hostStopTask = _host.StopAsync();
                if (!hostStopTask.Wait(5000)) // 5 second timeout for host stop
                {
                    System.Diagnostics.Debug.WriteLine("Host shutdown timed out during application exit");
                }
                _host.Dispose();
            }
            
            base.OnExit(e);
        }
    }

    /// <summary>
    /// Creates and configures the dependency injection host
    /// </summary>
    /// <returns>Configured host instance</returns>
    private IHost CreateHost()
    {
        return Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // Add logging
                services.AddLogging(builder =>
                {
                    builder.AddDebug();
                    builder.AddConsole();
                });
                
                // Add desktop services
                services.AddDesktopServices(context.Configuration);
                
                // Add view models
                services.AddViewModels();
            })
            .Build();
    }
    
    /// <summary>
    /// Initializes the theme system with default theme
    /// </summary>
    private void InitializeTheme()
    {
        try
        {
            // Apply default dark theme on startup
            ThemeManager.ApplyTheme(Theme.Dark);
        }
        catch (Exception ex)
        {
            // Fallback - continue without theme if it fails
            System.Diagnostics.Debug.WriteLine($"Theme initialization failed: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Starts background services asynchronously
    /// </summary>
    private async void StartServicesAsync()
    {
        if (_serviceProvider != null)
        {
            try
            {
                // Initialize database schema first (apply migrations)
                await _serviceProvider.EnsureDatabaseCreatedAsync();
                System.Diagnostics.Debug.WriteLine("Database initialization completed successfully");
                
                // Initialize user profile directory structure
                var directoryService = _serviceProvider.GetRequiredService<IDirectoryService>();
                var initResult = await directoryService.InitializeUserProfileAsync();
                
                if (!initResult.Success && initResult.Errors.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"Directory initialization completed with errors: {initResult.Message}");
                }
                
                // Start system status monitoring
                var systemStatusService = _serviceProvider.GetRequiredService<Lazarus.App.Shared.Services.ISystemStatusService>();
                await systemStatusService.StartMonitoringAsync();
                
                // Load view mode preferences
                var viewModeService = _serviceProvider.GetRequiredService<Lazarus.App.Shared.Services.IViewModeService>();
                await viewModeService.LoadViewModeAsync();
                
                // Note: Infrastructure startup (orchestrator & llama.cpp) is handled 
                // by the hosted service system and will start automatically
                System.Diagnostics.Debug.WriteLine("Infrastructure startup initiated via hosted service");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Service startup failed: {ex.Message}");
                
                // For database initialization failures, show error dialog
                if (ex.Message.Contains("database") || ex.Message.Contains("migration"))
                {
                    MessageBox.Show($"Database initialization failed: {ex.Message}\n\nApplication may not function correctly.", 
                        "Database Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }
    }
}