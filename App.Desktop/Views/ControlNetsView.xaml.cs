using System.Windows.Controls;
using Lazarus.Desktop.ViewModels;

namespace Lazarus.Desktop.Views
{
    public partial class ControlNetsView : UserControl
    {
        public ControlNetsView()
        {
            InitializeComponent();
            DataContext = new ControlNetsViewModel();
        }
    }
}