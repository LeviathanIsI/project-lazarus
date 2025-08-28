using System.Windows.Controls;

namespace Lazarus.Desktop.Views;

/// <summary>
/// System Context Bar - Always visible system state
/// Eliminates user blindness about model/runner/VRAM status
/// </summary>
public partial class SystemContextBar : UserControl
{
    public SystemContextBar()
    {
        InitializeComponent();
    }
}