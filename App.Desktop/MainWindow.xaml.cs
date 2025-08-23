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

            // Wire BaseModelView directly - the critical fix
            var baseModelViewModel = _serviceProvider.GetRequiredService<BaseModelViewModel>();
            var modelsView = ModelsView as ModelsView;
            if (modelsView?.BaseModelContent != null)
            {
                modelsView.BaseModelContent.DataContext = baseModelViewModel;
                Console.WriteLine("MainWindow: BaseModelView DataContext bound");
            }

            Console.WriteLine("MainWindow: DataContexts bound");
            _ = RefreshStatusAsync();
            Console.WriteLine("MainWindow: RefreshStatusAsync fired");
        }

        private async Task RefreshStatusAsync()
        {
            ApiStatus.Text = "API: checking...";
            var ok = await ApiClient.HealthAsync();
            ApiStatus.Text = ok ? "API: online (127.0.0.1:11711)" : "API: offline";
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
            var inactiveBrush = new SolidColorBrush(Color.FromRgb(55, 65, 81));
            var inactiveTextBrush = new SolidColorBrush(Color.FromRgb(156, 163, 175));

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
            var activeBrush = new SolidColorBrush(Color.FromRgb(139, 92, 246));
            var activeTextBrush = new SolidColorBrush(Colors.White);

            button.Background = activeBrush;
            button.Foreground = activeTextBrush;
        }
    }
}