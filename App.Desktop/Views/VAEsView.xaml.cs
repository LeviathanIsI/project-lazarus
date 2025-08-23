using System.Windows.Controls;
using Lazarus.Desktop.ViewModels;

namespace Lazarus.Desktop.Views
{
    public partial class VAEsView : UserControl
    {
        public VAEsView()
        {
            InitializeComponent();
            DataContext = new VAEsViewModel();
        }
    }
}