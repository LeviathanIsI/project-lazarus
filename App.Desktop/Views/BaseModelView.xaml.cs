using System.Windows.Controls;

namespace Lazarus.Desktop.Views
{
    public partial class BaseModelView : UserControl
    {
        public BaseModelView()
        {
            InitializeComponent();
            // ⚠️ Do NOT override DataContext here
        }
    }
}
