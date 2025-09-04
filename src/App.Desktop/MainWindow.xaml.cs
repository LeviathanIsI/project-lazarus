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
    /// <param name="viewModel">The main window view model</param>
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
    }
}