using Lazarus.Desktop.Extensions;
using Lazarus.Desktop.Services;
using Lazarus.Desktop.Views;
using Lazarus.Shared;
using Lazarus.Desktop.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Diagnostics;
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
        private LoadingWindow? _loadingWindow;
        private bool _disposed;

        /// <summary>
        /// Gets the current service provider instance.
        /// </summary>
        public static IServiceProvider ServiceProvider { get; private set; } = null!;

        protected override async void OnStartup(StartupEventArgs e)
        {
            // Binding trace to warnings (visible in Output window)
            PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Warning;

            try
            {
                // Build and start the host first
                _host = CreateHost(e.Args);
                await _host.StartAsync().ConfigureAwait(true);

                // Configure application-wide settings
                ConfigureApplication();

                // Get the service provider and logger
                ServiceProvider = _host.Services;
                _logger = ServiceProvider.GetRequiredService<ILogger<App>>();

                _logger.LogInformation("Host started successfully, beginning UI initialization");

                // Show loading window through DI
                await ShowLoadingWindowThroughDIAsync().ConfigureAwait(true);
            }
            catch (TaskCanceledException tex)
            {
                // Suppress TaskCanceledException to keep UI alive during diagnostics
                Debug.WriteLine("[Startup] TaskCanceledException suppressed so UI stays alive: " + tex);
                _logger?.LogWarning(tex, "Startup canceled; keeping UI alive for diagnostics");

                try
                {
                    if (ServiceProvider != null)
                    {
                        await InitializeMainWindowAsync().ConfigureAwait(true);
                    }
                }
                catch { }
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

                // Avoid immediate shutdown during diagnostics to inspect UI state
                // Shutdown(1);
                // return;
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

        private static DateTime _lastUiExceptionShownAt = DateTime.MinValue;
        private static string? _lastUiExceptionKey;

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            _logger?.LogCritical(e.Exception, "Unhandled exception on UI thread");
            SafeLog("DispatcherUnhandled", e.Exception);

            // Always mark handled so we don't cascade dialog storms during diagnostics
            e.Handled = true;

            // Suppress message box for common WPF binding issues that are recoverable
            var msg = e.Exception.Message ?? string.Empty;
            var isBindingUnsetValue = msg.Contains("DependencyProperty.UnsetValue", StringComparison.OrdinalIgnoreCase)
                                      || msg.Contains("BorderBrush", StringComparison.OrdinalIgnoreCase)
                                      || e.Exception is System.Windows.Markup.XamlParseException;

            if (isBindingUnsetValue)
            {
                // Log as error and return silently
                _logger?.LogError(e.Exception, "Suppressed WPF binding error on UI thread");
                return;
            }

            // Throttle repeated dialogs of the same message
            var now = DateTime.UtcNow;
            var key = msg;
            var isRepeat = key == _lastUiExceptionKey && (now - _lastUiExceptionShownAt) < TimeSpan.FromSeconds(5);
            if (isRepeat)
            {
                return;
            }
            _lastUiExceptionKey = key;
            _lastUiExceptionShownAt = now;

            var result = MessageBox.Show(
                $"An unexpected error occurred:\n\n{e.Exception.Message}\n\nWould you like to continue running the application?",
                "Lazarus Error",
                MessageBoxButton.YesNo,
                MessageBoxImage.Error);

            if (result == MessageBoxResult.No)
            {
                Shutdown(1);
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

                // Register WPF windows for DI resolution
                services.AddTransient<MainWindow>();
                services.AddTransient<Views.LoadingWindow>();

                // Factory that creates SelectableAdapter given an AdapterInfo at runtime
                services.AddTransient<Func<Lazarus.Shared.AdapterInfo, Lazarus.Desktop.ViewModels.SelectableAdapter>>(sp =>
                {
                    return info => ActivatorUtilities.CreateInstance<Lazarus.Desktop.ViewModels.SelectableAdapter>(sp, info);
                });
                // Do not register SelectableAdapter directly; it requires AdapterInfo.
                // Use the typed factory above to create instances with per-item data.
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

            // Logging is configured to use existing LocalAppData layout; do not create new folders here
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

        private async Task ShowLoadingWindowThroughDIAsync()
        {
            try
            {
                _logger?.LogInformation("Resolving loading window through DI");
                
                // Resolve loading window through DI (all dependencies will be injected)
                _loadingWindow = ServiceProvider.GetRequiredService<Views.LoadingWindow>();
                
                // Subscribe to events
                _loadingWindow.InitializationCompleted += OnInitializationCompleted;
                _loadingWindow.InitializationFailed += OnInitializationFailed;
                
                // Show loading window
                _loadingWindow.Show();
                
                _logger?.LogInformation("Loading window displayed through DI, starting initialization");
                
                // The loading window will handle its own initialization process
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to show loading window through DI");
                // Fall back to direct main window initialization
                await InitializeMainWindowAsync().ConfigureAwait(true);
            }
        }
        
        private async void OnInitializationCompleted(object? sender, EventArgs e)
        {
            try
            {
                _logger?.LogInformation("Initialization completed, showing main window");
                
                // Close loading window
                _loadingWindow?.Close();
                
                // Initialize and show main window
                await InitializeMainWindowAsync().ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to initialize main window after loading completed");
                MessageBox.Show($"Failed to start main application: {ex.Message}", 
                    "Lazarus Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(1);
            }
        }
        
        private void OnInitializationFailed(object? sender, EventArgs e)
        {
            _logger?.LogError("Initialization failed");
            
            // Loading window will handle showing the error and retry/exit options
            // Don't shut down here as the user might want to retry
        }

        private Task InitializeMainWindowAsync()
        {
            try
            {
                _logger?.LogInformation("Resolving main window through DI");
                
                // Initialize services that need async setup
                var themeService = ServiceProvider.GetRequiredService<Services.IThemeService>();
                var navigationService = ServiceProvider.GetRequiredService<Services.INavigationService>();

                // Apply initial theme
                var uiOptions = ServiceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<Configuration.UIOptions>>().Value;
                themeService.ApplyTheme(uiOptions.Theme);

                // Create and configure ViewModelLocator for XAML binding
                var viewModelLocator = ServiceProvider.GetRequiredService<ViewModelLocator>();
                Resources["ViewModelLocator"] = viewModelLocator;

                // Resolve main window through DI
                var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
                MainWindow = mainWindow;

                // Show window FIRST so UI is visible
                mainWindow.Show();

                // Navigate to startup view AFTER window is shown
                navigationService.NavigateTo(uiOptions.StartupView);

                _logger?.LogInformation("Main window initialized and displayed through DI");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to initialize main window through DI");
                throw;
            }

            return Task.CompletedTask;
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception exception)
            {
                _logger?.LogCritical(exception, "Unhandled exception on background thread. IsTerminating: {IsTerminating}", e.IsTerminating);
                SafeLog("AppDomain.Unhandled", exception);
            }
        }

        private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            _logger?.LogError(e.Exception, "Unobserved task exception");
            SafeLog("TaskScheduler.Unobserved", e.Exception);
            e.SetObserved(); // Prevent the process from terminating
        }

        private static void SafeLog(string tag, Exception? ex)
        {
            try
            {
                var root = System.IO.Path.Combine(Lazarus.Shared.LazarusPaths.SystemData.Logs, "Images");
                System.IO.Directory.CreateDirectory(root);
                var line = $"[{DateTime.Now:HH:mm:ss}] {tag}: {ex?.GetType().Name} {ex?.Message}{Environment.NewLine}{ex?.StackTrace}{Environment.NewLine}";
                System.IO.File.AppendAllText(System.IO.Path.Combine(root, "images-errors.log"), line);
                System.Diagnostics.Debug.WriteLine(line);
            }
            catch
            {
                // never throw from logger
            }
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
