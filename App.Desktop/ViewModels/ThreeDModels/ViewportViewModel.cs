using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Lazarus.Desktop.ViewModels.ThreeDModels
{
    public class ViewportViewModel : INotifyPropertyChanged
    {
        private Point3D _cameraPosition = new(5, 3, 8);
        private Vector3D _cameraLookDirection = new(-5, -3, -8);
        private bool _hasModel = false;
        private bool _showGrid = true;
        private string _renderMode = "Solid";
        private GeometryModel3D? _currentModel;

        // Camera properties
        public Point3D CameraPosition
        {
            get => _cameraPosition;
            set => SetProperty(ref _cameraPosition, value);
        }

        public Vector3D CameraLookDirection
        {
            get => _cameraLookDirection;
            set => SetProperty(ref _cameraLookDirection, value);
        }

        // Model state
        public bool HasModel
        {
            get => _hasModel;
            set => SetProperty(ref _hasModel, value);
        }

        public bool ShowGrid
        {
            get => _showGrid;
            set => SetProperty(ref _showGrid, value);
        }

        public string RenderMode
        {
            get => _renderMode;
            set => SetProperty(ref _renderMode, value);
        }

        public GeometryModel3D? CurrentModel
        {
            get => _currentModel;
            set
            {
                SetProperty(ref _currentModel, value);
                HasModel = value != null;
            }
        }

        // Commands
        public ICommand SetRenderModeCommand { get; }
        public ICommand ResetCameraCommand { get; }
        public ICommand ToggleGridCommand { get; }

        public ViewportViewModel()
        {
            SetRenderModeCommand = new Lazarus.Desktop.Helpers.RelayCommand(param => SetRenderMode(param?.ToString() ?? "Solid"));
            ResetCameraCommand = new Lazarus.Desktop.Helpers.RelayCommand(_ => ResetCamera());
            ToggleGridCommand = new Lazarus.Desktop.Helpers.RelayCommand(_ => ShowGrid = !ShowGrid);
        }

        private void SetRenderMode(string mode)
        {
            RenderMode = mode;
            // TODO: Apply render mode to current model
        }

        private void ResetCamera()
        {
            CameraPosition = new Point3D(5, 3, 8);
            CameraLookDirection = new Vector3D(-5, -3, -8);
        }

        public void LoadModel(string filePath)
        {
            try
            {
                // TODO: Implement ModelLoader
                var model = CreateTestCube(); // Temporary placeholder
                CurrentModel = model;
                FitCameraToModel();
            }
            catch (Exception ex)
            {
                // TODO: Handle loading errors
                System.Diagnostics.Debug.WriteLine($"Model loading failed: {ex.Message}");
            }
        }

        private GeometryModel3D CreateTestCube()
        {
            var mesh = new MeshGeometry3D();

            // Simple cube vertices
            mesh.Positions = new Point3DCollection(new[]
            {
                new Point3D(-1, -1, -1), new Point3D(1, -1, -1), new Point3D(1, 1, -1), new Point3D(-1, 1, -1),
                new Point3D(-1, -1, 1), new Point3D(1, -1, 1), new Point3D(1, 1, 1), new Point3D(-1, 1, 1)
            });

            // Triangle indices for cube faces
            mesh.TriangleIndices = new Int32Collection(new[]
            {
                0,1,2, 0,2,3, // front
                4,7,6, 4,6,5, // back
                0,4,5, 0,5,1, // bottom
                2,6,7, 2,7,3, // top
                0,3,7, 0,7,4, // left
                1,5,6, 1,6,2  // right
            });

            var material = new DiffuseMaterial(new SolidColorBrush(Colors.Gray));
            return new GeometryModel3D(mesh, material);
        }

        private void FitCameraToModel()
        {
            if (CurrentModel?.Geometry is MeshGeometry3D mesh)
            {
                var bounds = CalculateBounds(mesh);
                var center = new Point3D(
                    (bounds.Item1.X + bounds.Item2.X) / 2,
                    (bounds.Item1.Y + bounds.Item2.Y) / 2,
                    (bounds.Item1.Z + bounds.Item2.Z) / 2);

                var size = Math.Max(
                    Math.Max(bounds.Item2.X - bounds.Item1.X, bounds.Item2.Y - bounds.Item1.Y),
                    bounds.Item2.Z - bounds.Item1.Z);

                var distance = size * 2;
                CameraPosition = new Point3D(center.X + distance, center.Y + distance * 0.5, center.Z + distance);
                CameraLookDirection = new Vector3D(-distance, -distance * 0.5, -distance);
            }
        }

        private (Point3D, Point3D) CalculateBounds(MeshGeometry3D mesh)
        {
            if (mesh.Positions.Count == 0)
                return (new Point3D(-1, -1, -1), new Point3D(1, 1, 1));

            var min = new Point3D(double.MaxValue, double.MaxValue, double.MaxValue);
            var max = new Point3D(double.MinValue, double.MinValue, double.MinValue);

            foreach (var pos in mesh.Positions)
            {
                if (pos.X < min.X) min.X = pos.X;
                if (pos.Y < min.Y) min.Y = pos.Y;
                if (pos.Z < min.Z) min.Z = pos.Z;
                if (pos.X > max.X) max.X = pos.X;
                if (pos.Y > max.Y) max.Y = pos.Y;
                if (pos.Z > max.Z) max.Z = pos.Z;
            }

            return (min, max);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }

    // Mouse interaction handler
    public class CameraController
    {
        private bool _isRotating = false;
        private bool _isPanning = false;
        private Point _lastMousePosition;
        private ViewportViewModel _viewModel;

        public CameraController(ViewportViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        public void HandleMouseDown(MouseButtonEventArgs e, Point position)
        {
            _lastMousePosition = position;

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                _isRotating = true;
            }
            else if (e.RightButton == MouseButtonState.Pressed)
            {
                _isPanning = true;
            }
        }

        public void HandleMouseMove(Point position)
        {
            if (!_isRotating && !_isPanning) return;

            var delta = position - _lastMousePosition;
            _lastMousePosition = position;

            if (_isRotating)
            {
                RotateCamera(delta.X * 0.01, delta.Y * 0.01);
            }
            else if (_isPanning)
            {
                PanCamera(delta.X * 0.01, delta.Y * 0.01);
            }
        }

        public void HandleMouseUp()
        {
            _isRotating = false;
            _isPanning = false;
        }

        public void HandleMouseWheel(int delta)
        {
            var zoomFactor = delta > 0 ? 0.9 : 1.1;
            ZoomCamera(zoomFactor);
        }

        private void RotateCamera(double deltaX, double deltaY)
        {
            var pos = _viewModel.CameraPosition;
            var look = _viewModel.CameraLookDirection;

            // Simplified orbit rotation around origin
            var distance = Math.Sqrt(pos.X * pos.X + pos.Y * pos.Y + pos.Z * pos.Z);
            var azimuth = Math.Atan2(pos.Z, pos.X) - deltaX;
            var elevation = Math.Asin(pos.Y / distance) + deltaY;

            elevation = Math.Max(-Math.PI / 2 + 0.1, Math.Min(Math.PI / 2 - 0.1, elevation));

            var newPos = new Point3D(
                distance * Math.Cos(elevation) * Math.Cos(azimuth),
                distance * Math.Sin(elevation),
                distance * Math.Cos(elevation) * Math.Sin(azimuth));

            _viewModel.CameraPosition = newPos;
            _viewModel.CameraLookDirection = new Vector3D(-newPos.X, -newPos.Y, -newPos.Z);
        }

        private void PanCamera(double deltaX, double deltaY)
        {
            var pos = _viewModel.CameraPosition;
            var look = _viewModel.CameraLookDirection;

            // Simple pan by moving position perpendicular to look direction
            var right = Vector3D.CrossProduct(look, new Vector3D(0, 1, 0));
            right.Normalize();
            var up = Vector3D.CrossProduct(right, look);
            up.Normalize();

            var panOffset = right * deltaX + up * deltaY;
            _viewModel.CameraPosition = pos + panOffset;
        }

        private void ZoomCamera(double factor)
        {
            var pos = _viewModel.CameraPosition;
            var newPos = new Point3D(pos.X * factor, pos.Y * factor, pos.Z * factor);
            _viewModel.CameraPosition = newPos;
            _viewModel.CameraLookDirection = new Vector3D(-newPos.X, -newPos.Y, -newPos.Z);
        }
    }
}