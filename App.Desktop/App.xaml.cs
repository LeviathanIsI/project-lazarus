using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Lazarus.Orchestrator;
using Lazarus.Shared.Utilities; // <-- added

namespace Lazarus.Desktop
{
    public partial class App : Application
    {
        private CancellationTokenSource? _cts;

        protected override async void OnStartup(StartupEventArgs e)
        {
            Console.WriteLine("App: OnStartup called");

            base.OnStartup(e);
            _cts = new CancellationTokenSource();

            try
            {
                // ensure Lazarus folders exist
                DirectoryBootstrap.EnsureDirectories(); // <-- added

                Console.WriteLine("App: Starting orchestrator...");
                await OrchestratorHost.StartAsync("http://127.0.0.1:11711", _cts.Token);
                Console.WriteLine("App: Orchestrator started successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"App: Orchestrator failed to start - {ex.Message}");
                // If it fails, we still let the UI come up; you can add a toast later.
            }

            try
            {
                Console.WriteLine("App: Creating MainWindow...");
                var mainWindow = new MainWindow();
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

        protected override async void OnExit(ExitEventArgs e)
        {
            try { await OrchestratorHost.StopAsync(); } catch { /* shrug */ }
            _cts?.Cancel();
            _cts?.Dispose();
            base.OnExit(e);
        }
    }
}
