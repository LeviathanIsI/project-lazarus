using Lazarus.Desktop.Extensions;
using Lazarus.Desktop.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;

namespace Lazarus.Desktop
{
    /// <summary>
    /// Main application class with comprehensive dependency injection and service lifetime management.
    /// Implements proper async startup patterns and graceful shutdown handling.
    /// </summary>
    public partial class App : Application, IDisposable
    {
        private IHost? _host;
        private ILogger<App>? _logger;
        private bool _disposed;

        /// <summary>
        /// Gets the current service provider instance.
        /// </summary>
        public static IServiceProvider ServiceProvider { get; private set; } = null!;

        protected override async void OnStartup(StartupEventArgs e)
        {
            try
            {
                // Build and start the host
                _host = CreateHost(e.Args);
                await _host.StartAsync().ConfigureAwait(true);

                // Configure application-wide settings
                ConfigureApplication();

                // Get the service provider and logger
                ServiceProvider = _host.Services;
                _logger = ServiceProvider.GetRequiredService<ILogger<App>>();

                _logger.LogInformation("Lazarus Desktop application started successfully");

                // Perform lightweight binary validation before UI initialization
                await ValidateBinariesAsync().ConfigureAwait(true);

                // Initialize and show the main window
                await InitializeMainWindowAsync().ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                // Log the error if possible, otherwise show message box
                if (_logger != null)
                {
                    _logger.LogCritical(ex, "Fatal error during application startup");
                }
                else
                {
                    MessageBox.Show($"Fatal error during startup: {ex.Message}",
                        "Lazarus Startup Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }

                // Shutdown the application
                Shutdown(1);
                return;
            }

            base.OnStartup(e);
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            try
            {
                _logger?.LogInformation("Lazarus Desktop application shutting down");

                // Dispose the ViewModelLocator to clean up singleton ViewModels
                var viewModelLocator = ServiceProvider?.GetService<ViewModelLocator>();
                viewModelLocator?.Dispose();

                // Stop the host gracefully
                if (_host != null)
                {
                    using var shutdownCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                    await _host.StopAsync(shutdownCts.Token).ConfigureAwait(true);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during application shutdown");
            }
            finally
            {
                _host?.Dispose();
                _disposed = true;
            }

            base.OnExit(e);
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            _logger?.LogCritical(e.Exception, "Unhandled exception on UI thread");

            var result = MessageBox.Show(
                $"An unexpected error occurred:\n\n{e.Exception.Message}\n\nWould you like to continue running the application?",
                "Lazarus Error",
                MessageBoxButton.YesNo,
                MessageBoxImage.Error);

            if (result == MessageBoxResult.No)
            {
                Shutdown(1);
            }
            else
            {
                e.Handled = true;
            }
        }

        private static IHost CreateHost(string[] args)
        {
            var builder = Host.CreateDefaultBuilder(args);

            // Configure application settings
            builder.ConfigureAppConfiguration((context, config) =>
            {
                var appDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Directory.GetCurrentDirectory();

                config.SetBasePath(appDirectory)
                      .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                      .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true)
                      .AddEnvironmentVariables()
                      .AddCommandLine(args);
            });

            // Configure services
            builder.ConfigureServices((context, services) =>
            {
                services.AddLazarusDesktop(context.Configuration);
                services.AddLazarusHosting();
            });

            return builder.Build();
        }

        private void ConfigureApplication()
        {
            // Set global exception handlers
            DispatcherUnhandledException += OnDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

            // Configure WPF application properties
            ShutdownMode = ShutdownMode.OnMainWindowClose;

            // Ensure logs directory exists
            var logsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            Directory.CreateDirectory(logsPath);
        }

        /// <summary>
        /// Performs lightweight binary validation during startup.
        /// Validates file existence and CUDA driver availability without spawning processes.
        /// </summary>
        private async Task ValidateBinariesAsync()
        {
            var binaryValidationService = ServiceProvider.GetRequiredService<Services.IBinaryValidationService>();

            _logger?.LogInformation("Starting binary validation");

            using var startupTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            await binaryValidationService.ValidateAsync(startupTimeout.Token).ConfigureAwait(true);

            var status = binaryValidationService.Status;
            if (status.IsSystemReady)
            {
                _logger?.LogInformation("Binary validation completed successfully - system ready");
            }
            else
            {
                _logger?.LogWarning("Binary validation completed with issues - system may have limited functionality");
                foreach (var issue in status.Issues)
                {
                    _logger?.LogWarning("Binary validation issue: {Issue}", issue);
                }
            }
        }

        private Task InitializeMainWindowAsync()
        {
            // Initialize services that need async setup
            var themeService = ServiceProvider.GetRequiredService<Services.IThemeService>();
            var navigationService = ServiceProvider.GetRequiredService<Services.INavigationService>();

            // Apply initial theme
            var uiOptions = ServiceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<Configuration.UIOptions>>().Value;
            themeService.ApplyTheme(uiOptions.Theme);

            // Create and configure ViewModelLocator for XAML binding
            var viewModelLocator = ServiceProvider.GetRequiredService<ViewModelLocator>();
            Resources["ViewModelLocator"] = viewModelLocator;

            // Create and show main window
            var mainWindow = new MainWindow();
            var mainViewModel = viewModelLocator.MainViewModel;

            mainWindow.DataContext = mainViewModel;
            MainWindow = mainWindow;

            // Navigate to startup view
            navigationService.NavigateTo(uiOptions.StartupView);

            mainWindow.Show();

            _logger?.LogInformation("Main window initialized and displayed");

            return Task.CompletedTask;
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception exception)
            {
                _logger?.LogCritical(exception, "Unhandled exception on background thread. IsTerminating: {IsTerminating}", e.IsTerminating);
            }
        }

        private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            _logger?.LogError(e.Exception, "Unobserved task exception");
            e.SetObserved(); // Prevent the process from terminating
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _host?.Dispose();
                _disposed = true;
            }
        }
    }
}