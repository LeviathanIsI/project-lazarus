using System.Windows.Controls;
using Lazarus.Desktop.Services;
using App.Shared.Enums;

namespace Lazarus.Desktop.Views.Images
{
    public partial class InpaintingView : UserControl
    {
        public InpaintingView()
        {
            InitializeComponent();
            
            // Subscribe to ViewMode changes to force template re-evaluation
            Loaded += (_, __) =>
            {
                UserPreferencesService.ViewModeChanged += OnViewModeChanged;
            };
            
            Unloaded += (_, __) =>
            {
                UserPreferencesService.ViewModeChanged -= OnViewModeChanged;
            };
        }

        private void OnViewModeChanged(object? sender, ViewMode newMode)
        {
            // Force ContentPresenter to re-evaluate the template selector
            if (ModeAwareContent != null)
            {
                var content = ModeAwareContent.Content;
                ModeAwareContent.Content = null;
                ModeAwareContent.Content = content;
            }
        }
    }
}