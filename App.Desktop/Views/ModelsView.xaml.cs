using System.Windows;
using System.Windows.Controls;
using Lazarus.Desktop.ViewModels;

namespace Lazarus.Desktop.Views
{
    /// <summary>
    /// ModelsView simplified - now directly hosts BaseModelView
    /// Subtab navigation (LoRAs, ControlNets, etc.) handled by main sidebar
    /// </summary>
    public partial class ModelsView : UserControl
    {
        public ModelsView()
        {
            InitializeComponent();
            Console.WriteLine("[ModelsView] Simplified constructor - directly hosting BaseModelView");
        }
    }
}