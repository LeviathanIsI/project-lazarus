using System.Windows;
using System.Windows.Controls;

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
            // Find the MainWindow parent and trigger navigation to Model Configuration tab
            var mainWindow = Window.GetWindow(this) as MainWindow;
            mainWindow?.ShowTab("Models");
        }
    }
}
