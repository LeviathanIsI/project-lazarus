using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Lazarus.Desktop.Services;
using Lazarus.Desktop.ViewModels;
using Lazarus.Desktop.Extensions;

namespace Lazarus.Desktop
{
    /// <summary>
    /// Lazarus application with clean startup sequence
    /// </summary>
    public partial class App : Application
    {
        private IHost? _host;

        /// <summary>
        /// Gets the service provider from the built host
        /// </summary>
        public IServiceProvider Services => _host!.Services;

        /// <summary>
        /// Static access to service provider for backward compatibility
        /// </summary>
        public static IServiceProvider ServiceProvider => ((App)Current).Services;

        protected override async void OnStartup(StartupEventArgs e)
        {
            // Add global exception handler
            AppDomain.CurrentDomain.UnhandledException += (s, ex) =>
            {
                System.Diagnostics.Debug.WriteLine($"Unhandled exception: {ex.ExceptionObject}");
            };

            Dispatcher.UnhandledException += (s, ex) =>
            {
                System.Diagnostics.Debug.WriteLine($"Dispatcher exception: {ex.Exception}");
                ex.Handled = true;
            };

            // Don't let WPF auto-shutdown when splash closes
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            // 1) Show dumb splash (no DI dependencies)
            var splash = new Views.StartupWindow();
            splash.Show();
            System.Diagnostics.Debug.WriteLine("StartupWindow shown successfully");

            try
            {

                // 2) Build Host/DI before resolving anything
                _host = CreateHost(e.Args);
                System.Diagnostics.Debug.WriteLine("Host created");
                await _host.StartAsync();
                System.Diagnostics.Debug.WriteLine("Host started");

                // 3) Run bootstrap tasks with progress reporting
                var bootstrapper = Services.GetRequiredService<IAppBootstrapper>();
                System.Diagnostics.Debug.WriteLine("Bootstrapper resolved");
                var progress = new Progress<BootstrapProgress>(p =>
                {
                    // Keep UI responsive
                    splash.SetStatus(p.Step, p.Percent);
                });
                await bootstrapper.InitializeAsync(progress, CancellationToken.None);

                // 4) Resolve & show MainWindow AFTER host is ready
                System.Diagnostics.Debug.WriteLine("Resolving MainWindow");
                var main = Services.GetRequiredService<MainWindow>();
                System.Diagnostics.Debug.WriteLine("MainWindow resolved");
                MainWindow = main;
                main.Show();
                System.Diagnostics.Debug.WriteLine("MainWindow shown");

                // 5) Close splash and restore normal shutdown behavior
                splash.Close();
                ShutdownMode = ShutdownMode.OnMainWindowClose;
            }
            catch (Exception ex)
            {
                // Surface the real DI error so we can fix it
                MessageBox.Show($"Fatal startup error:\n\n{ex.Message}\n\nStack Trace:\n{ex.StackTrace}",
                    "Lazarus Startup Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                splash.Close();
                Shutdown(-1);
            }

            base.OnStartup(e);
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            if (_host is not null)
            {
                try { await _host.StopAsync(TimeSpan.FromSeconds(2)); }
                finally { _host.Dispose(); }
            }
            base.OnExit(e);
        }

        private static IHost CreateHost(string[] args)
        {
            var builder = Host.CreateDefaultBuilder(args);

            // Configure app configuration
            builder.ConfigureAppConfiguration((context, config) =>
            {
                var appDirectory = System.IO.Path.GetDirectoryName(
                    System.Reflection.Assembly.GetExecutingAssembly().Location) ?? System.IO.Directory.GetCurrentDirectory();

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
                services.AddSingleton<MainWindow>(sp =>
                {
                    var w = new MainWindow();
                    w.DataContext = sp.GetRequiredService<MainViewModel>();
                    return w;
                });
                services.AddTransient<Views.StartupWindow>();

                // Register bootstrapper
                services.AddSingleton<IAppBootstrapper, AppBootstrapper>();

                // Factory that creates SelectableAdapter given an AdapterInfo at runtime
                services.AddTransient<Func<Lazarus.Shared.AdapterInfo, Lazarus.Desktop.ViewModels.SelectableAdapter>>(sp =>
                {
                    return info => ActivatorUtilities.CreateInstance<Lazarus.Desktop.ViewModels.SelectableAdapter>(sp, info);
                });
            });

            return builder.Build();
        }
    }
}
