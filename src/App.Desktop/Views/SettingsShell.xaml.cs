using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using Lazarus.Desktop.ViewModels;

namespace Lazarus.Desktop.Views;

public partial class SettingsShell : UserControl
{
    public SettingsShell()
    {
        InitializeComponent();
        DataContext = App.ServiceProvider.GetRequiredService<SettingsShellViewModel>();
    }
}

