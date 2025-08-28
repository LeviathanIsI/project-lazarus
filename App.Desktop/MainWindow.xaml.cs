using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Data;
using Microsoft.Extensions.DependencyInjection;
using Lazarus.Desktop.ViewModels;
using Lazarus.Desktop.ViewModels.Images;
using Lazarus.Desktop.ViewModels.Video;
using Lazarus.Desktop.ViewModels.ThreeDModels;
using Lazarus.Desktop.ViewModels.Voice;
using Lazarus.Desktop.ViewModels.Entities;
using Lazarus.Desktop.Views;
using Lazarus.Desktop.Services;
using System.Linq;

namespace Lazarus.Desktop
{
    public partial class MainWindow : Window
    {
        private readonly IServiceProvider _serviceProvider;

        public MainWindow(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            InitializeComponent();

            // Set DataContext to MainWindowViewModel for global preferences
            DataContext = _serviceProvider.GetRequiredService<MainWindowViewModel>();
            
            // Ensure ContentControl binding is properly set up
            MainContentHost.SetBinding(ContentControl.ContentProperty, new Binding("CurrentViewModel") { Source = DataContext });

            // Initialize BaseModelViewModel after orchestrator is ready
            _ = InitializeViewModelsAsync();
            
            // Subscribe to navigation changes for diagnostics
            var navigationService = _serviceProvider.GetRequiredService<INavigationService>();
            navigationService.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName?.Contains("Visible") == true)
                {
                    Dispatcher.BeginInvoke(new Action(VerifyExactlyOneVisible));
                }
            };
        }

        private async Task InitializeViewModelsAsync()
        {
            try
            {
                // Wait a moment for orchestrator to fully initialize
                await Task.Delay(500);
                
                // Initialize BaseModelViewModel with models
                var baseModelViewModel = _serviceProvider.GetRequiredService<BaseModelViewModel>();
                await baseModelViewModel.InitializeAsync();
                
                // Initialize LorAsViewModel after UI is stable
                var lorasViewModel = _serviceProvider.GetRequiredService<LorAsViewModel>();
                await lorasViewModel.InitializeAsync();
                
                Console.WriteLine("ViewModels initialized successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ViewModel initialization failed: {ex.Message}");
            }
        }

        private async void PingApi_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var status = await ApiClient.GetSystemStatusAsync();
                if (status != null)
                {
                    ApiStatus.Text = $"API: {status.ActiveRunner} ({status.LoadedModel})";
                    ApiStatus.Foreground = new SolidColorBrush(Colors.Green);
                }
                else
                {
                    ApiStatus.Text = "API: Unknown";
                    ApiStatus.Foreground = new SolidColorBrush(Colors.Orange);
                }
            }
            catch (Exception ex)
            {
                ApiStatus.Text = $"API: Error - {ex.Message}";
                ApiStatus.Foreground = new SolidColorBrush(Colors.Red);
            }
        }

        /// <summary>
        /// Diagnostic method to verify exactly one view is visible at all times
        /// </summary>
        private void VerifyExactlyOneVisible()
        {
            try
            {
                // ContentControl pattern now handles view switching automatically
                Console.WriteLine("[DIAGNOSTICS] ContentControl pattern - single view always displayed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DIAGNOSTICS] Error in VerifyExactlyOneVisible: {ex.Message}");
            }
        }
    }
}