using System.Diagnostics;
using System.Windows.Controls;

namespace Lazarus.Desktop.Views;

public partial class GeneralSettingsView : UserControl
{
    public GeneralSettingsView()
    {
        InitializeComponent();
        Debug.WriteLine("[VIEW] GeneralSettingsView constructed");
    }
}

