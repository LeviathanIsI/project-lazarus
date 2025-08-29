using System.Windows.Controls;
using Lazarus.Desktop.Services;
using App.Shared.Enums;

namespace Lazarus.Desktop.Views
{
    /// <summary>
    /// Runner Manager - Control and manage LLM backends
    /// Provides interface for switching between llama.cpp, vLLM, ExLlamaV2, etc.
    /// </summary>
    public partial class RunnerManagerView : UserControl
    {
        public RunnerManagerView()
        {
            InitializeComponent();
            UserPreferencesService.ViewModeChanged += OnViewModeChanged;
        }

        private void OnViewModeChanged(object? sender, ViewMode e)
        {
            // Force re-application of the ViewMode template for this view
            if (Content is ContentControl cc)
            {
                var current = cc.Content;
                cc.Content = null;
                cc.Content = current;
            }
        }
    }
}