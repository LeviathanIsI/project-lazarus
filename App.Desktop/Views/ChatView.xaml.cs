using System.Windows.Controls;
using Lazarus.Desktop.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace Lazarus.Desktop.Views
{
    public partial class ChatView : UserControl
    {
        public ChatView()
        {
            InitializeComponent();
        }
        
        private void ConfigureModelsButton_Click(object sender, RoutedEventArgs e)
        {
            // Use MVVM navigation service instead of direct MainWindow manipulation
            if (Application.Current is App app && app.ServiceProvider != null)
            {
                var navigationService = app.ServiceProvider.GetRequiredService<INavigationService>();
                navigationService.NavigateTo(NavigationTab.Models);
            }
        }
    }
}
