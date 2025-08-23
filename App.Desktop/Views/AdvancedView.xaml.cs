using System.Windows.Controls;

namespace Lazarus.Desktop.Views
{
    /// <summary>
    /// Interaction logic for AdvancedView.xaml
    /// </summary>
    public partial class AdvancedView : UserControl
    {
        public AdvancedView()
        {
            InitializeComponent();
            DataContext = new ViewModels.AdvancedViewModel();
        }
    }
}