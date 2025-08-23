using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Lazarus.Desktop.Helpers;

namespace Lazarus.Desktop.ViewModels.Video
{
    public class Video2VideoViewModel : INotifyPropertyChanged
    {
        private string _prompt = "";
        private double _strength = 0.8;
        private string _styleMode = "None";
        private bool _hasResult;
        private bool _isGenerating;
        private string _generationStatus = "";

        public Video2VideoViewModel()
        {
            GenerateCommand = new SimpleRelayCommand(TransformVideo, CanTransform);
            BrowseVideoCommand = new SimpleRelayCommand(BrowseVideo);
            TransformationHistory = new ObservableCollection<VideoTransformationHistoryItem>();
        }

        public string Prompt
        {
            get => _prompt;
            set => SetProperty(ref _prompt, value);
        }

        public double Strength
        {
            get => _strength;
            set => SetProperty(ref _strength, value);
        }

        public string StyleMode
        {
            get => _styleMode;
            set => SetProperty(ref _styleMode, value);
        }

        public bool HasResult
        {
            get => _hasResult;
            set => SetProperty(ref _hasResult, value);
        }

        public bool IsGenerating
        {
            get => _isGenerating;
            set => SetProperty(ref _isGenerating, value);
        }

        public string GenerationStatus
        {
            get => _generationStatus;
            set => SetProperty(ref _generationStatus, value);
        }

        public ObservableCollection<VideoTransformationHistoryItem> TransformationHistory { get; }

        public ICommand GenerateCommand { get; }
        public ICommand BrowseVideoCommand { get; }

        private bool CanTransform()
        {
            return !IsGenerating && !string.IsNullOrWhiteSpace(Prompt);
        }

        private void BrowseVideo()
        {
            // TODO: Implement file browser for video selection
            System.Diagnostics.Debug.WriteLine("Browse video clicked");
        }

        private async void TransformVideo()
        {
            try
            {
                IsGenerating = true;
                GenerationStatus = "Analyzing source video...";

                TransformationHistory.Insert(0, new VideoTransformationHistoryItem
                {
                    Prompt = Prompt,
                    Strength = Strength,
                    StyleMode = StyleMode,
                    Timestamp = DateTime.Now.ToString("HH:mm:ss")
                });

                // Simulate transformation process
                GenerationStatus = "Applying transformations...";
                await Task.Delay(2000);

                GenerationStatus = "Rendering final video...";
                await Task.Delay(1500);

                HasResult = true;
                GenerationStatus = "Transformation complete!";
                await Task.Delay(1000);
                GenerationStatus = "";
            }
            catch (Exception ex)
            {
                GenerationStatus = $"Error: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Video transformation error: {ex.Message}");
            }
            finally
            {
                IsGenerating = false;
            }
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

    public class VideoTransformationHistoryItem
    {
        public string Prompt { get; set; } = "";
        public double Strength { get; set; }
        public string StyleMode { get; set; } = "";
        public string Timestamp { get; set; } = "";
    }
}