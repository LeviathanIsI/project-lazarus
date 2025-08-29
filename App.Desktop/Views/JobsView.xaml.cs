using System.Windows.Controls;
using Lazarus.Desktop.Services;
using App.Shared.Enums;

namespace Lazarus.Desktop.Views
{
    public partial class JobsView : UserControl
    {
        public JobsView()
        {
            InitializeComponent();
            // Ensure template refresh on mode change for this view instance
            UserPreferencesService.ViewModeChanged += OnViewModeChanged;
        }

        private void OnViewModeChanged(object? sender, ViewMode e)
        {
            // Re-apply template so visual changes are immediate when switching modes
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