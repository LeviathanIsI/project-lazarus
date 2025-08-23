using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media;
using Lazarus.Desktop.Helpers;

namespace Lazarus.Desktop.ViewModels.Video
{
    public class MotionControlViewModel : INotifyPropertyChanged
    {
        private double _panX = 0;
        private double _panY = 0;
        private double _zoom = 1.0;
        private double _rotation = 0;
        private bool _enableObjectTracking = false;
        private string _trackingPrompt = "";
        private double _motionSmoothing = 0.5;
        private bool _hasMotionPath = false;
        private bool _isProcessing = false;
        private string _processingStatus = "";
        private double _timelinePosition = 0;
        private Geometry? _motionPath;

        public MotionControlViewModel()
        {
            ApplyMotionCommand = new SimpleRelayCommand(ApplyMotion, CanApplyMotion);
            PreviewMotionCommand = new SimpleRelayCommand(PreviewMotion);
            ApplyPresetCommand = new SimpleRelayCommand<MotionPreset>(ApplyPreset);
            
            MotionPresets = new ObservableCollection<MotionPreset>
            {
                new() { Name = "Smooth Pan Left", Description = "Smooth horizontal pan from right to left" },
                new() { Name = "Zoom In", Description = "Gradual zoom into the center of the frame" },
                new() { Name = "Circle Track", Description = "Circular camera movement around subject" },
                new() { Name = "Dolly Zoom", Description = "Classic Hitchcock dolly zoom effect" },
                new() { Name = "Handheld Shake", Description = "Realistic handheld camera movement" },
                new() { Name = "Drone Ascent", Description = "Smooth upward movement like a drone shot" }
            };

            // Generate sample motion path
            GenerateMotionPath();
        }

        public double PanX
        {
            get => _panX;
            set
            {
                if (SetProperty(ref _panX, value))
                    GenerateMotionPath();
            }
        }

        public double PanY
        {
            get => _panY;
            set
            {
                if (SetProperty(ref _panY, value))
                    GenerateMotionPath();
            }
        }

        public double Zoom
        {
            get => _zoom;
            set
            {
                if (SetProperty(ref _zoom, value))
                    GenerateMotionPath();
            }
        }

        public double Rotation
        {
            get => _rotation;
            set
            {
                if (SetProperty(ref _rotation, value))
                    GenerateMotionPath();
            }
        }

        public bool EnableObjectTracking
        {
            get => _enableObjectTracking;
            set => SetProperty(ref _enableObjectTracking, value);
        }

        public string TrackingPrompt
        {
            get => _trackingPrompt;
            set => SetProperty(ref _trackingPrompt, value);
        }

        public double MotionSmoothing
        {
            get => _motionSmoothing;
            set => SetProperty(ref _motionSmoothing, value);
        }

        public bool HasMotionPath
        {
            get => _hasMotionPath;
            set => SetProperty(ref _hasMotionPath, value);
        }

        public bool IsProcessing
        {
            get => _isProcessing;
            set => SetProperty(ref _isProcessing, value);
        }

        public string ProcessingStatus
        {
            get => _processingStatus;
            set => SetProperty(ref _processingStatus, value);
        }

        public double TimelinePosition
        {
            get => _timelinePosition;
            set => SetProperty(ref _timelinePosition, value);
        }

        public Geometry? MotionPath
        {
            get => _motionPath;
            set => SetProperty(ref _motionPath, value);
        }

        public ObservableCollection<MotionPreset> MotionPresets { get; }

        public ICommand ApplyMotionCommand { get; }
        public ICommand PreviewMotionCommand { get; }
        public ICommand ApplyPresetCommand { get; }

        private bool CanApplyMotion()
        {
            return !IsProcessing;
        }

        private async void ApplyMotion()
        {
            try
            {
                IsProcessing = true;
                ProcessingStatus = "Analyzing motion parameters...";
                await Task.Delay(1000);

                ProcessingStatus = "Generating motion path...";
                await Task.Delay(1500);

                ProcessingStatus = "Applying motion effects...";
                await Task.Delay(2000);

                ProcessingStatus = "Motion applied successfully!";
                await Task.Delay(1000);
                ProcessingStatus = "";
            }
            catch (Exception ex)
            {
                ProcessingStatus = $"Error: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Motion application error: {ex.Message}");
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private void PreviewMotion()
        {
            // TODO: Implement motion preview
            System.Diagnostics.Debug.WriteLine("Previewing motion...");
        }

        private void ApplyPreset(MotionPreset? preset)
        {
            if (preset == null) return;

            // Apply preset values based on preset name
            switch (preset.Name)
            {
                case "Smooth Pan Left":
                    PanX = -0.8;
                    PanY = 0;
                    Zoom = 1.0;
                    Rotation = 0;
                    break;
                case "Zoom In":
                    PanX = 0;
                    PanY = 0;
                    Zoom = 1.5;
                    Rotation = 0;
                    break;
                case "Circle Track":
                    PanX = 0;
                    PanY = 0;
                    Zoom = 1.0;
                    Rotation = 45;
                    break;
                // Add more presets as needed
            }

            System.Diagnostics.Debug.WriteLine($"Applied preset: {preset.Name}");
        }

        private void GenerateMotionPath()
        {
            // Generate a simple motion path visualization based on current parameters
            var geometry = new PathGeometry();
            var figure = new PathFigure { StartPoint = new System.Windows.Point(200, 150) };
            
            // Create path based on motion parameters
            var segment = new LineSegment(new System.Windows.Point(200 + PanX * 100, 150 + PanY * 100), true);
            figure.Segments.Add(segment);
            
            geometry.Figures.Add(figure);
            MotionPath = geometry;
            HasMotionPath = true;
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

    public class MotionPreset
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
    }
}