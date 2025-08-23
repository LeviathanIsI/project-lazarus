using System.Windows.Controls;

namespace Lazarus.Desktop.Views
{
    /// <summary>
    /// Interaction logic for HypernetworksView.xaml
    /// </summary>
    public partial class HypernetworksView : UserControl
    {
        public HypernetworksView()
        {
            InitializeComponent();
            DataContext = new ViewModels.HypernetworksViewModel();
        }
    }
}