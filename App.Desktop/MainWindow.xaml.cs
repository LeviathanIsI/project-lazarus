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

            // Set ViewModels for all views
            ChatView.DataContext = _serviceProvider.GetRequiredService<ChatViewModel>();
            ModelsView.DataContext = _serviceProvider.GetRequiredService<ModelsViewModel>();

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

        private void ChatTab_Click(object sender, RoutedEventArgs e)
        {
            ShowTab("Chat");
        }

        private void ModelsTab_Click(object sender, RoutedEventArgs e)
        {
            ShowTab("Models");
        }

        private void ShowTab(string tabName)
        {
            ChatView.Visibility = Visibility.Collapsed;
            ModelsView.Visibility = Visibility.Collapsed;
            ResetTabButtonStyles();

            switch (tabName)
            {
                case "Chat":
                    ChatView.Visibility = Visibility.Visible;
                    SetActiveTabStyle(ChatTabButton);
                    break;
                case "Models":
                    ModelsView.Visibility = Visibility.Visible;
                    SetActiveTabStyle(ModelsTabButton);
                    break;
            }
        }

        private void ResetTabButtonStyles()
        {
            var inactiveBrush = new SolidColorBrush(Color.FromRgb(55, 65, 81));
            var inactiveTextBrush = new SolidColorBrush(Color.FromRgb(156, 163, 175));

            ChatTabButton.Background = inactiveBrush;
            ChatTabButton.Foreground = inactiveTextBrush;
            ModelsTabButton.Background = inactiveBrush;
            ModelsTabButton.Foreground = inactiveTextBrush;
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