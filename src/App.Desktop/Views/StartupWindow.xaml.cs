using System.Windows;
using System.Windows.Controls;

namespace Lazarus.Desktop.Views
{
    /// <summary>
    /// Dumb startup window with no DI dependencies
    /// </summary>
    public partial class StartupWindow : Window
    {
        public StartupWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Updates the status display during initialization
        /// </summary>
        /// <param name="step">Current step description</param>
        /// <param name="percent">Progress percentage (0-100)</param>
        public void SetStatus(string step, int percent)
        {
            // Update on UI thread
            Dispatcher.Invoke(() =>
            {
                StatusText.Text = step;
                ProgressBar.Value = percent;
                ProgressText.Text = $"{percent}%";
            });
        }
    }
}
