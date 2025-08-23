using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Lazarus.Desktop.Helpers;

namespace Lazarus.Desktop.ViewModels.Video
{
    public class Text2VideoViewModel : INotifyPropertyChanged
    {
        private string _prompt = "";
        private string _negativePrompt = "";
        private double _durationSeconds = 3.0;
        private int _frameRate = 24;
        private string _resolution = "512x512";
        private int _steps = 20;
        private double _cfgScale = 7.0;
        private string _cameraMovement = "Static";
        private double _motionIntensity = 0.5;
        private bool _hasGeneratedVideo;
        private bool _isGenerating;
        private string _generationStatus = "";

        public Text2VideoViewModel()
        {
            GenerateCommand = new SimpleRelayCommand(GenerateVideo, CanGenerate);
            PlayCommand = new SimpleRelayCommand(PlayVideo, CanPlayVideo);
            PauseCommand = new SimpleRelayCommand(PauseVideo, CanPauseVideo);
            StopCommand = new SimpleRelayCommand(StopVideo, CanStopVideo);
            SaveCommand = new SimpleRelayCommand(SaveVideo, CanSaveVideo);
            GenerationHistory = new ObservableCollection<VideoGenerationHistoryItem>();
        }

        public string Prompt
        {
            get => _prompt;
            set => SetProperty(ref _prompt, value);
        }

        public string NegativePrompt
        {
            get => _negativePrompt;
            set => SetProperty(ref _negativePrompt, value);
        }

        public double DurationSeconds
        {
            get => _durationSeconds;
            set => SetProperty(ref _durationSeconds, value);
        }

        public int FrameRate
        {
            get => _frameRate;
            set => SetProperty(ref _frameRate, value);
        }

        public string Resolution
        {
            get => _resolution;
            set => SetProperty(ref _resolution, value);
        }

        public int Steps
        {
            get => _steps;
            set => SetProperty(ref _steps, value);
        }

        public double CfgScale
        {
            get => _cfgScale;
            set => SetProperty(ref _cfgScale, value);
        }

        public string CameraMovement
        {
            get => _cameraMovement;
            set => SetProperty(ref _cameraMovement, value);
        }

        public double MotionIntensity
        {
            get => _motionIntensity;
            set => SetProperty(ref _motionIntensity, value);
        }

        public bool HasGeneratedVideo
        {
            get => _hasGeneratedVideo;
            set => SetProperty(ref _hasGeneratedVideo, value);
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

        public ObservableCollection<VideoGenerationHistoryItem> GenerationHistory { get; }

        public ICommand GenerateCommand { get; }
        public ICommand PlayCommand { get; }
        public ICommand PauseCommand { get; }
        public ICommand StopCommand { get; }
        public ICommand SaveCommand { get; }

        private bool CanGenerate()
        {
            return !IsGenerating && !string.IsNullOrWhiteSpace(Prompt);
        }

        private bool CanPlayVideo()
        {
            return HasGeneratedVideo && !IsGenerating;
        }

        private bool CanPauseVideo()
        {
            return HasGeneratedVideo && !IsGenerating;
        }

        private bool CanStopVideo()
        {
            return HasGeneratedVideo && !IsGenerating;
        }

        private bool CanSaveVideo()
        {
            return HasGeneratedVideo && !IsGenerating;
        }

        private async void GenerateVideo()
        {
            try
            {
                IsGenerating = true;
                GenerationStatus = "Initializing video generation...";

                // Add to history immediately
                GenerationHistory.Insert(0, new VideoGenerationHistoryItem
                {
                    Prompt = Prompt,
                    NegativePrompt = NegativePrompt,
                    DurationSeconds = DurationSeconds,
                    FrameRate = FrameRate,
                    Resolution = Resolution,
                    Steps = Steps,
                    CfgScale = CfgScale,
                    CameraMovement = CameraMovement,
                    MotionIntensity = MotionIntensity,
                    Timestamp = DateTime.Now.ToString("HH:mm:ss")
                });

                // Simulate video generation process
                for (int i = 0; i <= 100; i += 10)
                {
                    GenerationStatus = $"Generating frames... {i}%";
                    await Task.Delay(200);
                }

                GenerationStatus = "Encoding video...";
                await Task.Delay(500);

                GenerationStatus = "Finalizing...";
                await Task.Delay(300);

                HasGeneratedVideo = true;
                GenerationStatus = "Video generation complete!";
                await Task.Delay(1000);
                GenerationStatus = "";
            }
            catch (Exception ex)
            {
                GenerationStatus = $"Error: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Video generation error: {ex.Message}");
            }
            finally
            {
                IsGenerating = false;
            }
        }

        private void PlayVideo()
        {
            // TODO: Implement video playback
            System.Diagnostics.Debug.WriteLine("Playing video...");
        }

        private void PauseVideo()
        {
            // TODO: Implement video pause
            System.Diagnostics.Debug.WriteLine("Pausing video...");
        }

        private void StopVideo()
        {
            // TODO: Implement video stop
            System.Diagnostics.Debug.WriteLine("Stopping video...");
        }

        private void SaveVideo()
        {
            // TODO: Implement video save
            System.Diagnostics.Debug.WriteLine("Saving video...");
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

    public class VideoGenerationHistoryItem
    {
        public string Prompt { get; set; } = "";
        public string NegativePrompt { get; set; } = "";
        public double DurationSeconds { get; set; }
        public int FrameRate { get; set; }
        public string Resolution { get; set; } = "";
        public int Steps { get; set; }
        public double CfgScale { get; set; }
        public string CameraMovement { get; set; } = "";
        public double MotionIntensity { get; set; }
        public string Timestamp { get; set; } = "";
    }
}