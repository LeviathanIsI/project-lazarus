using System.Windows.Controls;
using Lazarus.Desktop.Services;
using App.Shared.Enums;

namespace Lazarus.Desktop.Views
{
    public partial class DatasetsView : UserControl
    {
        public DatasetsView()
        {
            InitializeComponent();
            UserPreferencesService.ViewModeChanged += OnViewModeChanged;
        }

        private void OnViewModeChanged(object? sender, ViewMode e)
        {
            var content = ModeAwareContent;
            if (content != null)
            {
                var current = content.Content;
                content.Content = null;
                content.Content = current;
            }
        }
    }
}