using System.Windows.Controls;

namespace Lazarus.Desktop.Views
{
    /// <summary>
    /// Dashboard - Central command center and system overview
    /// First tab users see when opening Project Lazarus
    /// </summary>
    public partial class DashboardView : UserControl
    {
        public DashboardView()
        {
            Console.WriteLine("[DashboardView] CONSTRUCTOR: Starting initialization...");
            try 
            {
                InitializeComponent();
                Console.WriteLine("[DashboardView] CONSTRUCTOR: InitializeComponent completed successfully");

                // Force template refresh when ViewMode changes
                Lazarus.Desktop.Services.UserPreferencesService.ViewModeChanged += (_, __) =>
                {
                    Dispatcher.InvokeAsync(() =>
                    {
                        var content = Presenter.Content;
                        Presenter.Content = null;
                        Presenter.Content = content; // re-evaluate template selector
                    });
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DashboardView] CONSTRUCTOR: FAILED with exception: {ex.Message}");
                throw;
            }
        }
    }
}