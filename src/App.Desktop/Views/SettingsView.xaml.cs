using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using Lazarus.Desktop.ViewModels;

namespace Lazarus.Desktop.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
        DataContext = App.ServiceProvider.GetRequiredService<SettingsViewModel>();
    }
}

