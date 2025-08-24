using System.Windows.Controls;
using Lazarus.Desktop.ViewModels;

namespace Lazarus.Desktop.Views
{
    public partial class BaseModelView : UserControl
    {
        public BaseModelView()
        {
            InitializeComponent();
            DataContext = new BaseModelViewModel();
        }
    }
}