using System.Diagnostics;
using System.Windows.Controls;

namespace Lazarus.Desktop.Views;

public partial class PathsSettingsView : UserControl
{
    public PathsSettingsView()
    {
        InitializeComponent();
        Debug.WriteLine("[VIEW] PathsSettingsView constructed");
    }
}

