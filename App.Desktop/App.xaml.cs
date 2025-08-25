using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Lazarus.Orchestrator;
using Lazarus.Shared.Utilities;
using Lazarus.Desktop.ViewModels;
using Lazarus.Desktop.Views;
using Lazarus.Desktop.Services;
using App.Shared.Enums;

namespace Lazarus.Desktop
{
    public partial class App : Application
    {
        private CancellationTokenSource? _cts;
        private ServiceProvider? _serviceProvider;
        
        // HYDRA PREVENTION - Stop recursive exception dialogs
        private static bool _isHandlingException = false;

        protected override async void OnStartup(StartupEventArgs e)
        {
            // Add HYDRA-SAFE exception handlers to prevent recursive dialogs
            AppDomain.CurrentDomain.UnhandledException += Application_UnhandledException;
            DispatcherUnhandledException += Application_DispatcherUnhandledException;


            Console.WriteLine("App: OnStartup called");

            base.OnStartup(e);
            _cts = new CancellationTokenSource();

            try
            {
                Console.WriteLine("App: Beginning startup sequence...");

                // Configure dependency injection
                Console.WriteLine("App: Configuring dependency injection...");
                var services = new ServiceCollection();
                ConfigureServices(services);
                Console.WriteLine("App: Building service provider...");
                _serviceProvider = services.BuildServiceProvider();
                Console.WriteLine("App: Service provider built successfully");

                // CRITICAL: Load default theme IMMEDIATELY after DI setup
                // This must happen BEFORE MainWindow XAML parsing
                Console.WriteLine("App: Loading default theme EARLY...");
                try 
                {
                    var preferencesService = _serviceProvider.GetRequiredService<UserPreferencesService>();
                    preferencesService.ApplyTheme(ThemeMode.Dark); // Load default Dark theme
                    Console.WriteLine("App: EARLY Default Dark theme loaded successfully");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"App: CRITICAL - EARLY Theme loading failed: {ex.Message}");
                    Console.WriteLine($"App: Stack trace: {ex.StackTrace}");
                    throw; // This is fatal - can't continue without themes
                }

                // Start orchestrator and wait for it to be ready
                Console.WriteLine("App: Starting orchestrator...");
                
                // Ensure Lazarus folders exist
                Console.WriteLine("App: Ensuring directories exist...");
                DirectoryBootstrap.EnsureDirectories();
                
                // Start orchestrator
                Console.WriteLine("App: Starting orchestrator host...");
                try
                {
                    await OrchestratorHost.StartAsync("http://127.0.0.1:11711", _cts.Token);
                    Console.WriteLine("App: Orchestrator started successfully");
                }
                catch (Exception orchestratorEx)
                {
                    Console.WriteLine($"App: Orchestrator startup failed: {orchestratorEx.GetType().Name}: {orchestratorEx.Message}");
                    Console.WriteLine($"App: Stack trace: {orchestratorEx.StackTrace}");
                    
                    if (orchestratorEx.InnerException != null)
                    {
                        Console.WriteLine($"App: Inner exception: {orchestratorEx.InnerException.GetType().Name}: {orchestratorEx.InnerException.Message}");
                    }
                    
                    // Try to continue anyway - maybe external orchestrator is running
                    Console.WriteLine("App: Continuing despite orchestrator startup failure...");
                }

                // Wait for API to be ready with timeout
                Console.WriteLine("App: Waiting for API to be ready...");
                var apiReady = await WaitForApiReadyAsync(TimeSpan.FromSeconds(15));
                if (apiReady)
                {
                    Console.WriteLine("App: API is ready");
                }
                else
                {
                    Console.WriteLine("App: Warning - API not ready after timeout, continuing anyway");
                    Console.WriteLine("App: This may result in chat functionality not working");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"App: Critical startup failure - {ex.GetType().Name}: {ex.Message}");
                Console.WriteLine($"App: Stack trace: {ex.StackTrace}");
                // Continue with UI startup even if orchestrator fails
            }

            // Theme already loaded early in startup process

            Console.WriteLine("App: Creating MainWindow...");
            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            Console.WriteLine("App: MainWindow created successfully");

            Console.WriteLine("App: Showing MainWindow...");
            mainWindow.Show();
            Console.WriteLine("App: MainWindow.Show() called successfully");
        }

        private async Task<bool> WaitForApiReadyAsync(TimeSpan timeout)
        {
            var endTime = DateTime.UtcNow.Add(timeout);
            var delay = TimeSpan.FromMilliseconds(100);

            while (DateTime.UtcNow < endTime)
            {
                try
                {
                    var isReady = await ApiClient.HealthAsync();
                    if (isReady)
                    {
                        return true;
                    }
                }
                catch
                {
                    // API not ready yet, continue waiting
                }

                await Task.Delay(delay, _cts?.Token ?? CancellationToken.None);
                delay = TimeSpan.FromMilliseconds(Math.Min(delay.TotalMilliseconds * 1.5, 1000));
            }

            return false;
        }

        private void ConfigureServices(IServiceCollection services)
        {
            Console.WriteLine("App: ConfigureServices - Starting DI registration...");
            
            // Register Global Services
            Console.WriteLine("App: Registering global services...");
            services.AddSingleton<UserPreferencesService>();
            
            // Register ViewModels
            Console.WriteLine("App: Registering core ViewModels...");
            services.AddSingleton<BaseModelViewModel>();
            services.AddSingleton<DynamicParameterViewModel>();
            services.AddSingleton<MainWindowViewModel>();
            services.AddTransient<ModelsViewModel>();
            services.AddTransient<ChatViewModel>();
            services.AddSingleton<LorAsViewModel>();
            services.AddTransient<ControlNetsViewModel>();
            services.AddTransient<VAEsViewModel>();
            services.AddTransient<EmbeddingsViewModel>();
            services.AddTransient<HypernetworksViewModel>();
            services.AddTransient<AdvancedViewModel>();
            
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
            Console.WriteLine("App: Registering Entities ViewModels...");
            services.AddTransient<Lazarus.Desktop.ViewModels.Entities.EntitiesViewModel>();
            services.AddTransient<Lazarus.Desktop.ViewModels.Entities.EntityCreationViewModel>();
            services.AddTransient<Lazarus.Desktop.ViewModels.Entities.BehavioralPatternsViewModel>();
            services.AddTransient<Lazarus.Desktop.ViewModels.Entities.InteractionTestingViewModel>();
            services.AddTransient<Lazarus.Desktop.ViewModels.Entities.EntityManagementViewModel>();
            Console.WriteLine("App: Entities ViewModels registered successfully");

            Console.WriteLine("App: Registering UI components...");
            services.AddTransient<MainWindow>();
            services.AddTransient<BaseModelView>();
            Console.WriteLine("App: ConfigureServices completed successfully");

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
        
        #region HYDRA-SAFE Exception Handlers
        
        /// <summary>
        /// EMERGENCY CRASH HANDLER - Prevents window-spawning hydra
        /// NO THEME RESOURCES - Bare MessageBox only
        /// </summary>
        private void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            if (_isHandlingException) 
            {
                Console.WriteLine("[HYDRA PREVENTION] Already handling exception - blocking recursive dialog");
                return; // NO RECURSION - CRITICAL
            }
            
            _isHandlingException = true;
            e.Handled = true; // HANDLE BEFORE UI OPERATIONS
            
            try
            {
                Console.WriteLine($"[EMERGENCY] UI Thread Exception: {e.Exception.GetType().Name}: {e.Exception.Message}");
                Console.WriteLine($"[EMERGENCY] Stack trace: {e.Exception.StackTrace}");
                
                // Show BARE MessageBox - NO themed dialogs, NO app resources
                MessageBox.Show($"Application crashed on UI thread:\n\n{e.Exception.Message}\n\nApplication will close.", 
                               "Critical Error", 
                               MessageBoxButton.OK, 
                               MessageBoxImage.Error);
                               
                // Clean shutdown to prevent further damage
                Application.Current.Shutdown();
            }
            catch (Exception fatalEx)
            {
                Console.WriteLine($"[FATAL] Exception handler itself crashed: {fatalEx.Message}");
                Environment.Exit(1); // Nuclear option if even error handling fails
            }
        }
        
        /// <summary>
        /// BACKGROUND THREAD CRASH HANDLER - Last resort for non-UI exceptions
        /// </summary>
        private void Application_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (_isHandlingException) return; // NO RECURSION
            _isHandlingException = true;
            
            try
            {
                var ex = e.ExceptionObject as Exception;
                Console.WriteLine($"[EMERGENCY] Background Exception: {ex?.GetType().Name}: {ex?.Message}");
                
                // Show BARE MessageBox - NO app resources
                MessageBox.Show($"Critical background error:\n\n{ex?.Message}\n\nApplication will close.", 
                               "Fatal Error", 
                               MessageBoxButton.OK, 
                               MessageBoxImage.Error);
            }
            catch (Exception fatalEx)
            {
                Console.WriteLine($"[FATAL] Background exception handler crashed: {fatalEx.Message}");
            }
            finally
            {
                Environment.Exit(1); // Background thread crashes are always fatal
            }
        }
        
        #endregion
    }
}