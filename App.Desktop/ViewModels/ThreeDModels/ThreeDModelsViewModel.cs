using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Lazarus.Desktop.ViewModels.ThreeDModels
{
    /// <summary>
    /// Main ViewModel for the 3D Models tab that orchestrates the child view models
    /// </summary>
    public class ThreeDModelsViewModel : INotifyPropertyChanged
    {
        public ModelTreeViewModel ModelTree { get; }
        public ViewportViewModel Viewport { get; }
        public ModelPropertiesViewModel Properties { get; }

        public ThreeDModelsViewModel()
        {
            ModelTree = new ModelTreeViewModel();
            Viewport = new ViewportViewModel();
            Properties = new ModelPropertiesViewModel();

            // Wire up communication between ViewModels
            ModelTree.PropertyChanged += OnModelTreePropertyChanged;
        }

        private void OnModelTreePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ModelTreeViewModel.SelectedNode))
            {
                var selectedNode = ModelTree.SelectedNode;
                if (selectedNode != null && !selectedNode.IsDirectory)
                {
                    // Load model in viewport
                    Viewport.LoadModel(selectedNode.FullPath);
                    
                    // Update properties panel
                    Properties.SelectedModelPath = selectedNode.FullPath;
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}