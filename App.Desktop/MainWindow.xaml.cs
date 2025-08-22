using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Lazarus.Desktop.ViewModels;

namespace Lazarus.Desktop
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            Console.WriteLine("MainWindow: Starting initialization...");
            InitializeComponent();
            Console.WriteLine("MainWindow: InitializeComponent done");

            var chatViewModel = new ChatViewModel();
            Console.WriteLine("MainWindow: ChatViewModel created");

            ChatView.DataContext = chatViewModel;
            Console.WriteLine("MainWindow: DataContext bound");

            _ = RefreshStatusAsync();
            Console.WriteLine("MainWindow: RefreshStatusAsync fired");
        }

        private async Task RefreshStatusAsync()
        {
            ApiStatus.Text = "API: checking…";
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
            // Hide all tabs
            ChatView.Visibility = Visibility.Collapsed;
            ModelsView.Visibility = Visibility.Collapsed;

            // Reset button styles
            ResetTabButtonStyles();

            // Show selected tab and update button style
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
            // Inactive style
            var inactiveBrush = new SolidColorBrush(Color.FromRgb(55, 65, 81)); // #374151
            var inactiveTextBrush = new SolidColorBrush(Color.FromRgb(156, 163, 175)); // #9ca3af

            ChatTabButton.Background = inactiveBrush;
            ChatTabButton.Foreground = inactiveTextBrush;
            ModelsTabButton.Background = inactiveBrush;
            ModelsTabButton.Foreground = inactiveTextBrush;
        }

        private void SetActiveTabStyle(System.Windows.Controls.Button button)
        {
            // Active style
            var activeBrush = new SolidColorBrush(Color.FromRgb(139, 92, 246)); // #8b5cf6
            var activeTextBrush = new SolidColorBrush(Colors.White);

            button.Background = activeBrush;
            button.Foreground = activeTextBrush;
        }
    }
}