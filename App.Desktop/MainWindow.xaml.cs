using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Microsoft.Extensions.DependencyInjection;
using Lazarus.Desktop.ViewModels;
using Lazarus.Desktop.Views;

namespace Lazarus.Desktop
{
    public partial class MainWindow : Window
    {
        private readonly IServiceProvider _serviceProvider;

        public MainWindow(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            Console.WriteLine("MainWindow: Starting initialization...");
            InitializeComponent();
            Console.WriteLine("MainWindow: InitializeComponent done");

            // Set ViewModels for existing views
            ConversationsView.DataContext = _serviceProvider.GetRequiredService<ChatViewModel>();
            ModelsView.DataContext = _serviceProvider.GetRequiredService<ModelsViewModel>();
            
            // Set DataContext for 3D Models view
            ModelsThreeDView.DataContext = _serviceProvider.GetRequiredService<Lazarus.Desktop.ViewModels.ThreeDModels.ThreeDModelsViewModel>();
            
            // Set DataContext for Images view
            ImagesView.DataContext = _serviceProvider.GetRequiredService<Lazarus.Desktop.ViewModels.Images.ImagesViewModel>();
            
            // Set DataContext for Video view
            VideoView.DataContext = _serviceProvider.GetRequiredService<Lazarus.Desktop.ViewModels.Video.VideoViewModel>();
            
            // Set DataContext for Voice view
            VoiceView.DataContext = _serviceProvider.GetRequiredService<Lazarus.Desktop.ViewModels.Voice.VoiceViewModel>();
            
            // Set DataContext for Entities view
            EntitiesView.DataContext = _serviceProvider.GetRequiredService<Lazarus.Desktop.ViewModels.Entities.EntitiesViewModel>();

            // Wire BaseModelView and other sub-ViewModels directly - the critical fix
            var baseModelViewModel = _serviceProvider.GetRequiredService<BaseModelViewModel>();
            var modelsView = ModelsView as ModelsView;
            if (modelsView?.BaseModelContent != null)
            {
                modelsView.BaseModelContent.DataContext = baseModelViewModel;
                Console.WriteLine("MainWindow: BaseModelView DataContext bound");
            }

            // Wire other Models sub-tabs
            if (modelsView != null)
            {
                if (modelsView.LoRAsContent != null)
                    modelsView.LoRAsContent.DataContext = _serviceProvider.GetRequiredService<LorAsViewModel>();
                if (modelsView.ControlNetsContent != null)
                    modelsView.ControlNetsContent.DataContext = _serviceProvider.GetRequiredService<ControlNetsViewModel>();
                if (modelsView.VAEsContent != null)
                    modelsView.VAEsContent.DataContext = _serviceProvider.GetRequiredService<VAEsViewModel>();
                if (modelsView.EmbeddingsContent != null)
                    modelsView.EmbeddingsContent.DataContext = _serviceProvider.GetRequiredService<EmbeddingsViewModel>();
                if (modelsView.HypernetworksContent != null)
                    modelsView.HypernetworksContent.DataContext = _serviceProvider.GetRequiredService<HypernetworksViewModel>();
                if (modelsView.AdvancedContent != null)
                    modelsView.AdvancedContent.DataContext = _serviceProvider.GetRequiredService<AdvancedViewModel>();
                Console.WriteLine("MainWindow: All Models sub-view DataContexts bound");
            }

            Console.WriteLine("MainWindow: DataContexts bound");
            
            // Initialize BaseModelViewModel after orchestrator is ready
            _ = InitializeViewModelsAsync();
            Console.WriteLine("MainWindow: ViewModel initialization started");
        }

        private async Task InitializeViewModelsAsync()
        {
            try
            {
                // Wait a moment for orchestrator to fully initialize
                await Task.Delay(500);
                
                // Update status first
                await RefreshStatusAsync();
                
                // Initialize BaseModelViewModel with models
                var baseModelViewModel = _serviceProvider.GetRequiredService<BaseModelViewModel>();
                await baseModelViewModel.InitializeAsync();
                
                Console.WriteLine("MainWindow: ViewModels initialized successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"MainWindow: ViewModel initialization failed - {ex.Message}");
            }
        }

        private async Task RefreshStatusAsync()
        {
            ApiStatus.Text = "API: checking...";
            
            // Retry logic for API health check
            var maxRetries = 5;
            var delay = TimeSpan.FromMilliseconds(500);
            
            for (int retry = 0; retry < maxRetries; retry++)
            {
                try
                {
                    var ok = await ApiClient.HealthAsync();
                    if (ok)
                    {
                        ApiStatus.Text = "API: online (127.0.0.1:11711)";
                        return;
                    }
                }
                catch
                {
                    // API not ready yet
                }
                
                if (retry < maxRetries - 1)
                {
                    await Task.Delay(delay);
                    delay = TimeSpan.FromMilliseconds(Math.Min(delay.TotalMilliseconds * 1.5, 2000));
                }
            }
            
            ApiStatus.Text = "API: offline";
        }

        private async void PingApi_Click(object sender, RoutedEventArgs e)
        {
            await RefreshStatusAsync();
        }

        // All seven tab click handlers
        private void ConversationsTab_Click(object sender, RoutedEventArgs e)
        {
            ShowTab("Conversations");
        }

        private void ModelsTab_Click(object sender, RoutedEventArgs e)
        {
            ShowTab("Models");
        }

        private void ImagesTab_Click(object sender, RoutedEventArgs e)
        {
            ShowTab("Images");
        }

        private void VideoTab_Click(object sender, RoutedEventArgs e)
        {
            ShowTab("Video");
        }

        private void ModelsThreeDTab_Click(object sender, RoutedEventArgs e)
        {
            ShowTab("3DModels");
        }

        private void VoiceTab_Click(object sender, RoutedEventArgs e)
        {
            ShowTab("Voice");
        }

        private void EntitiesTab_Click(object sender, RoutedEventArgs e)
        {
            ShowTab("Entities");
        }

        private void ShowTab(string tabName)
        {
            // Hide all views
            ConversationsView.Visibility = Visibility.Collapsed;
            ModelsView.Visibility = Visibility.Collapsed;
            ImagesView.Visibility = Visibility.Collapsed;
            VideoView.Visibility = Visibility.Collapsed;
            ModelsThreeDView.Visibility = Visibility.Collapsed;
            VoiceView.Visibility = Visibility.Collapsed;
            EntitiesView.Visibility = Visibility.Collapsed;

            ResetTabButtonStyles();

            switch (tabName)
            {
                case "Conversations":
                    ConversationsView.Visibility = Visibility.Visible;
                    SetActiveTabStyle(ConversationsTabButton);
                    break;
                case "Models":
                    ModelsView.Visibility = Visibility.Visible;
                    SetActiveTabStyle(ModelsTabButton);
                    break;
                case "Images":
                    ImagesView.Visibility = Visibility.Visible;
                    SetActiveTabStyle(ImagesTabButton);
                    break;
                case "Video":
                    VideoView.Visibility = Visibility.Visible;
                    SetActiveTabStyle(VideoTabButton);
                    break;
                case "3DModels":
                    ModelsThreeDView.Visibility = Visibility.Visible;
                    SetActiveTabStyle(ModelsThreeDTabButton);
                    break;
                case "Voice":
                    VoiceView.Visibility = Visibility.Visible;
                    SetActiveTabStyle(VoiceTabButton);
                    break;
                case "Entities":
                    EntitiesView.Visibility = Visibility.Visible;
                    SetActiveTabStyle(EntitiesTabButton);
                    break;
            }
        }

        private void ResetTabButtonStyles()
        {
            var inactiveBrush = FindResource("BorderBrush") as SolidColorBrush;
            var inactiveTextBrush = FindResource("TextMutedBrush") as SolidColorBrush;

            ConversationsTabButton.Background = inactiveBrush;
            ConversationsTabButton.Foreground = inactiveTextBrush;
            ModelsTabButton.Background = inactiveBrush;
            ModelsTabButton.Foreground = inactiveTextBrush;
            ImagesTabButton.Background = inactiveBrush;
            ImagesTabButton.Foreground = inactiveTextBrush;
            VideoTabButton.Background = inactiveBrush;
            VideoTabButton.Foreground = inactiveTextBrush;
            ModelsThreeDTabButton.Background = inactiveBrush;
            ModelsThreeDTabButton.Foreground = inactiveTextBrush;
            VoiceTabButton.Background = inactiveBrush;
            VoiceTabButton.Foreground = inactiveTextBrush;
            EntitiesTabButton.Background = inactiveBrush;
            EntitiesTabButton.Foreground = inactiveTextBrush;
        }

        private void SetActiveTabStyle(System.Windows.Controls.Button button)
        {
            var activeBrush = FindResource("AccentRedBrush") as SolidColorBrush;
            var activeTextBrush = FindResource("TextPrimaryBrush") as SolidColorBrush;

            button.Background = activeBrush;
            button.Foreground = activeTextBrush;
        }
    }
}