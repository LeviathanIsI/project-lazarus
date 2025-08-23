using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Lazarus.Desktop.ViewModels.Video
{
    public class VideoViewModel : INotifyPropertyChanged
    {
        public Text2VideoViewModel Text2Video { get; }
        public Video2VideoViewModel Video2Video { get; }
        public MotionControlViewModel MotionControl { get; }
        public TemporalEffectsViewModel TemporalEffects { get; }

        public VideoViewModel()
        {
            Text2Video = new Text2VideoViewModel();
            Video2Video = new Video2VideoViewModel();
            MotionControl = new MotionControlViewModel();
            TemporalEffects = new TemporalEffectsViewModel();

            // Wire up communication between ViewModels if needed
            Text2Video.PropertyChanged += OnSubViewModelPropertyChanged;
            Video2Video.PropertyChanged += OnSubViewModelPropertyChanged;
            MotionControl.PropertyChanged += OnSubViewModelPropertyChanged;
            TemporalEffects.PropertyChanged += OnSubViewModelPropertyChanged;
        }

        private void OnSubViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // Handle cross-communication between video generation sub-tabs if needed
            // For example, passing generated videos between tabs for further processing
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}