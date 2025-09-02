using Lazarus.App.Desktop.ViewModels;
using System.Windows;

namespace Lazarus.App.Desktop;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
        
        // Set the DataContext to the MainWindowViewModel from DI container
        try
        {
            DataContext = App.GetService<MainWindowViewModel>();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to initialize main window: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <inheritdoc />
    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        
        // Additional initialization logic can go here
        if (DataContext is MainWindowViewModel viewModel)
        {
            _ = Task.Run(async () => await viewModel.InitializeAsync());
        }
    }
}