using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Lazarus.Orchestrator;
using Lazarus.Shared.Utilities;
using Lazarus.Desktop.ViewModels;
using Lazarus.Desktop.Views;

namespace Lazarus.Desktop
{
    public partial class App : Application
    {
        private CancellationTokenSource? _cts;
        private ServiceProvider? _serviceProvider;

        protected override async void OnStartup(StartupEventArgs e)
        {
            Console.WriteLine("App: OnStartup called");

            base.OnStartup(e);
            _cts = new CancellationTokenSource();

            // Configure dependency injection
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();

            try
            {
                // Ensure Lazarus folders exist
                DirectoryBootstrap.EnsureDirectories();

                Console.WriteLine("App: Starting orchestrator...");
                await OrchestratorHost.StartAsync("http://127.0.0.1:11711", _cts.Token);
                Console.WriteLine("App: Orchestrator started successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"App: Orchestrator failed to start - {ex.Message}");
            }

            try
            {
                Console.WriteLine("App: Creating MainWindow...");
                var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
                Console.WriteLine("App: MainWindow created successfully");

                mainWindow.Show();
                Console.WriteLine("App: MainWindow.Show() called");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"App: MainWindow creation failed - {ex.Message}");
                Console.WriteLine($"App: Stack trace - {ex.StackTrace}");
                throw;
            }
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Register ViewModels
            services.AddTransient<BaseModelViewModel>();
            services.AddTransient<ModelsViewModel>();
            services.AddTransient<ChatViewModel>();
            services.AddTransient<BaseModelViewModel>();

            // Register Views
            services.AddTransient<MainWindow>();
            services.AddTransient<BaseModelView>();

            // Register other services as needed
            // services.AddSingleton<IApiClient, ApiClient>();
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            try { await OrchestratorHost.StopAsync(); } catch { }
            _cts?.Cancel();
            _cts?.Dispose();
            _serviceProvider?.Dispose();
            base.OnExit(e);
        }
    }
}