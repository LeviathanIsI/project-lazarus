using System.Windows;
using System.Windows.Controls;
using Lazarus.Desktop.ViewModels.ThreeDModels;

namespace Lazarus.Desktop.Views.ThreeDModels
{
    /// <summary>
    /// Interaction logic for ThreeDModelsView.xaml
    /// </summary>
    public partial class ThreeDModelsView : UserControl
    {
        public ThreeDModelsView()
        {
            InitializeComponent();
            
            // DataContext will be set by MainWindow via DI
            // Individual controls will be wired up by the main ThreeDModelsViewModel
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (DataContext is ThreeDModelsViewModel mainViewModel)
            {
                // Wire up child controls to their respective ViewModels
                ModelTreeControl.DataContext = mainViewModel.ModelTree;
                ViewportControl.DataContext = mainViewModel.Viewport;
                PropertiesPanel.DataContext = mainViewModel.Properties;
            }
        }
    }
}