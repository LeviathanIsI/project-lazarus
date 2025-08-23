using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Lazarus.Desktop.ViewModels.ThreeDModels;

namespace Lazarus.Desktop.Views.ThreeDModels.Controls
{
    /// <summary>
    /// Interaction logic for ViewportControl.xaml
    /// </summary>
    public partial class ViewportControl : UserControl
    {
        private CameraController? _cameraController;
        private ViewportViewModel? _viewModel;

        public ViewportControl()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            _viewModel = DataContext as ViewportViewModel;
            if (_viewModel != null)
            {
                _cameraController = new CameraController(_viewModel);
            }
        }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var position = e.GetPosition(InteractionSurface);
            _cameraController?.HandleMouseDown(e, position);
            InteractionSurface.CaptureMouse();
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (InteractionSurface.IsMouseCaptured)
            {
                var position = e.GetPosition(InteractionSurface);
                _cameraController?.HandleMouseMove(position);
            }
        }

        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _cameraController?.HandleMouseUp();
            InteractionSurface.ReleaseMouseCapture();
        }

        private void OnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var position = e.GetPosition(InteractionSurface);
            _cameraController?.HandleMouseDown(e, position);
            InteractionSurface.CaptureMouse();
        }

        private void OnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            _cameraController?.HandleMouseUp();
            InteractionSurface.ReleaseMouseCapture();
        }

        private void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            _cameraController?.HandleMouseWheel(e.Delta);
        }
    }
}