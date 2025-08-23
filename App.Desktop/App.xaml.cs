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

            // Start orchestrator in background - don't block UI startup
            _ = Task.Run(async () =>
            {
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
            }, _cts.Token);

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
            services.AddSingleton<BaseModelViewModel>();
            services.AddTransient<ModelsViewModel>();
            services.AddTransient<ChatViewModel>();
            
            // Register 3D Model ViewModels
            services.AddTransient<Lazarus.Desktop.ViewModels.ThreeDModels.ThreeDModelsViewModel>();
            services.AddTransient<Lazarus.Desktop.ViewModels.ThreeDModels.ViewportViewModel>();
            services.AddTransient<Lazarus.Desktop.ViewModels.ThreeDModels.ModelTreeViewModel>();
            services.AddTransient<Lazarus.Desktop.ViewModels.ThreeDModels.ModelPropertiesViewModel>();
            
            // Register Images ViewModels
            services.AddTransient<Lazarus.Desktop.ViewModels.Images.ImagesViewModel>();
            services.AddTransient<Lazarus.Desktop.ViewModels.Images.Text2ImageViewModel>();
            services.AddTransient<Lazarus.Desktop.ViewModels.Images.Image2ImageViewModel>();
            services.AddTransient<Lazarus.Desktop.ViewModels.Images.InpaintingViewModel>();
            services.AddTransient<Lazarus.Desktop.ViewModels.Images.UpscalingViewModel>();

            // Register Video ViewModels
            services.AddTransient<Lazarus.Desktop.ViewModels.Video.VideoViewModel>();
            services.AddTransient<Lazarus.Desktop.ViewModels.Video.Text2VideoViewModel>();
            services.AddTransient<Lazarus.Desktop.ViewModels.Video.Video2VideoViewModel>();
            services.AddTransient<Lazarus.Desktop.ViewModels.Video.MotionControlViewModel>();
            services.AddTransient<Lazarus.Desktop.ViewModels.Video.TemporalEffectsViewModel>();

            // Register Voice ViewModels
            services.AddTransient<Lazarus.Desktop.ViewModels.Voice.VoiceViewModel>();
            services.AddTransient<Lazarus.Desktop.ViewModels.Voice.TTSConfigurationViewModel>();
            services.AddTransient<Lazarus.Desktop.ViewModels.Voice.VoiceCloningViewModel>();
            services.AddTransient<Lazarus.Desktop.ViewModels.Voice.RealTimeSynthesisViewModel>();
            services.AddTransient<Lazarus.Desktop.ViewModels.Voice.AudioProcessingViewModel>();

            // Register Entities ViewModels
            services.AddTransient<Lazarus.Desktop.ViewModels.Entities.EntitiesViewModel>();
            services.AddTransient<Lazarus.Desktop.ViewModels.Entities.EntityCreationViewModel>();
            services.AddTransient<Lazarus.Desktop.ViewModels.Entities.BehavioralPatternsViewModel>();
            services.AddTransient<Lazarus.Desktop.ViewModels.Entities.InteractionTestingViewModel>();
            services.AddTransient<Lazarus.Desktop.ViewModels.Entities.EntityManagementViewModel>();

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