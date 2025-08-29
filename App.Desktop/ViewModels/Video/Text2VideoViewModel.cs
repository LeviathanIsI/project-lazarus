using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Win32;
using Lazarus.Desktop.Helpers;

namespace Lazarus.Desktop.ViewModels.Video
{
    public class Text2VideoViewModel : INotifyPropertyChanged
    {
        #region Private Fields
        private string _prompt = "";
        private string _negativePrompt = "";
        private double _durationSeconds = 5.0;
        private int _frameRate = 24;
        private string _aspectRatio = "16:9";
        private string _resolution = "1024x576";
        private int _steps = 20;
        private double _cfgScale = 7.0;
        private double _motionStrength = 0.7;
        private string _cameraMovement = "Static";
        private string _selectedModel = "Stable Video Diffusion";
        private string _selectedStyle = "Cinematic";
        private int _seed = -1;
        private bool _useRandomSeed = true;
        private string _videoSource = "";
        private bool _hasGeneratedVideo;
        private bool _isGenerating;
        private bool _isPlaying;
        private bool _isPaused;
        private double _generationProgress;
        private string _generationStatus = "";
        private double _currentFrame = 0;
        private double _totalFrames = 120;
        private string _previewFramePath = "";
        private long _estimatedFileSize = 0;
        private TimeSpan _estimatedTime = TimeSpan.Zero;
        private int _batchCount = 1;
        private string _exportFormat = "MP4";
        private string _exportQuality = "High";
        private CancellationTokenSource? _cancellationTokenSource;
        #endregion

        #region Properties
        public string Prompt
        {
            get => _prompt;
            set
            {
                if (SetProperty(ref _prompt, value))
                {
                    OnPropertyChanged(nameof(CanGenerate));
                    UpdateEstimates();
                }
            }
        }

        public string NegativePrompt
        {
            get => _negativePrompt;
            set => SetProperty(ref _negativePrompt, value);
        }

        public double DurationSeconds
        {
            get => _durationSeconds;
            set
            {
                if (SetProperty(ref _durationSeconds, value))
                {
                    TotalFrames = (int)(value * FrameRate);
                    UpdateEstimates();
                }
            }
        }

        public int FrameRate
        {
            get => _frameRate;
            set
            {
                if (SetProperty(ref _frameRate, value))
                {
                    TotalFrames = (int)(DurationSeconds * value);
                    UpdateEstimates();
                }
            }
        }

        public string AspectRatio
        {
            get => _aspectRatio;
            set
            {
                if (SetProperty(ref _aspectRatio, value))
                {
                    UpdateResolutionFromAspectRatio();
                }
            }
        }

        public string Resolution
        {
            get => _resolution;
            set => SetProperty(ref _resolution, value);
        }

        public int Steps
        {
            get => _steps;
            set
            {
                if (SetProperty(ref _steps, value))
                {
                    UpdateEstimates();
                }
            }
        }

        public double CfgScale
        {
            get => _cfgScale;
            set => SetProperty(ref _cfgScale, value);
        }

        public double MotionStrength
        {
            get => _motionStrength;
            set => SetProperty(ref _motionStrength, value);
        }

        public string CameraMovement
        {
            get => _cameraMovement;
            set => SetProperty(ref _cameraMovement, value);
        }

        public string SelectedModel
        {
            get => _selectedModel;
            set
            {
                if (SetProperty(ref _selectedModel, value))
                {
                    UpdateModelSettings();
                }
            }
        }

        public string SelectedStyle
        {
            get => _selectedStyle;
            set => SetProperty(ref _selectedStyle, value);
        }

        public int Seed
        {
            get => _seed;
            set => SetProperty(ref _seed, value);
        }

        public bool UseRandomSeed
        {
            get => _useRandomSeed;
            set
            {
                if (SetProperty(ref _useRandomSeed, value) && value)
                {
                    GenerateRandomSeed();
                }
            }
        }

        public string VideoSource
        {
            get => _videoSource;
            set => SetProperty(ref _videoSource, value);
        }

        public bool HasGeneratedVideo
        {
            get => _hasGeneratedVideo;
            set
            {
                if (SetProperty(ref _hasGeneratedVideo, value))
                {
                    OnPropertyChanged(nameof(CanPlay));
                    OnPropertyChanged(nameof(CanSave));
                    OnPropertyChanged(nameof(CanExport));
                }
            }
        }

        public bool IsGenerating
        {
            get => _isGenerating;
            set
            {
                if (SetProperty(ref _isGenerating, value))
                {
                    OnPropertyChanged(nameof(CanGenerate));
                    OnPropertyChanged(nameof(CanCancel));
                    OnPropertyChanged(nameof(CanPlay));
                }
            }
        }

        public bool IsPlaying
        {
            get => _isPlaying;
            set => SetProperty(ref _isPlaying, value);
        }

        public bool IsPaused
        {
            get => _isPaused;
            set => SetProperty(ref _isPaused, value);
        }

        public double GenerationProgress
        {
            get => _generationProgress;
            set => SetProperty(ref _generationProgress, value);
        }

        public string GenerationStatus
        {
            get => _generationStatus;
            set => SetProperty(ref _generationStatus, value);
        }

        public double CurrentFrame
        {
            get => _currentFrame;
            set
            {
                if (SetProperty(ref _currentFrame, value))
                {
                    OnPropertyChanged(nameof(CurrentTimeText));
                    OnPropertyChanged(nameof(PlaybackProgress));
                }
            }
        }

        public double TotalFrames
        {
            get => _totalFrames;
            set
            {
                if (SetProperty(ref _totalFrames, value))
                {
                    OnPropertyChanged(nameof(TotalTimeText));
                    OnPropertyChanged(nameof(PlaybackProgress));
                }
            }
        }

        public string PreviewFramePath
        {
            get => _previewFramePath;
            set => SetProperty(ref _previewFramePath, value);
        }

        public long EstimatedFileSize
        {
            get => _estimatedFileSize;
            set => SetProperty(ref _estimatedFileSize, value);
        }

        public TimeSpan EstimatedTime
        {
            get => _estimatedTime;
            set => SetProperty(ref _estimatedTime, value);
        }

        public int BatchCount
        {
            get => _batchCount;
            set => SetProperty(ref _batchCount, value);
        }

        public string ExportFormat
        {
            get => _exportFormat;
            set => SetProperty(ref _exportFormat, value);
        }

        public string ExportQuality
        {
            get => _exportQuality;
            set => SetProperty(ref _exportQuality, value);
        }

        // Computed Properties
        public bool CanGenerate => !IsGenerating && !string.IsNullOrWhiteSpace(Prompt);
        public bool CanCancel => IsGenerating;
        public bool CanPlay => HasGeneratedVideo && !IsGenerating;
        public bool CanSave => HasGeneratedVideo && !IsGenerating;
        public bool CanExport => HasGeneratedVideo && !IsGenerating;
        public string CurrentTimeText => TimeSpan.FromSeconds(CurrentFrame / FrameRate).ToString(@"mm\:ss");
        public string TotalTimeText => TimeSpan.FromSeconds(TotalFrames / FrameRate).ToString(@"mm\:ss");
        public double PlaybackProgress => TotalFrames > 0 ? (CurrentFrame / TotalFrames) * 100 : 0;
        public string FileSizeText => EstimatedFileSize < 1024 * 1024 
            ? $"~{EstimatedFileSize / 1024:F0} KB" 
            : $"~{EstimatedFileSize / 1024.0 / 1024.0:F1} MB";
        public string EstimatedTimeText => EstimatedTime.TotalMinutes < 1 
            ? $"~{EstimatedTime.TotalSeconds:F0}s" 
            : $"~{EstimatedTime.TotalMinutes:F1}m";
        public string VideoInfoText => HasGeneratedVideo 
            ? $"{Resolution} • {FrameRate}fps • {DurationSeconds:F1}s"
            : "No video generated";
        #endregion

        #region Collections
        public ObservableCollection<VideoGenerationHistoryItem> GenerationHistory { get; } = new();
        public ObservableCollection<VideoGenerationQueueItem> GenerationQueue { get; } = new();
        public ObservableCollection<string> FavoritePrompts { get; } = new();
        public ObservableCollection<string> PreviewFrames { get; } = new();

        public List<string> AvailableModels { get; } = new()
        {
            "Stable Video Diffusion",
            "AnimateDiff",
            "Zeroscope",
            "ModelScope",
            "VideoCrafter",
            "LaVie",
            "Show-1",
            "CogVideo"
        };

        public List<string> StylePresets { get; } = new()
        {
            "Cinematic",
            "Animation",
            "Realistic",
            "Documentary",
            "Artistic",
            "Vintage",
            "Sci-Fi",
            "Fantasy",
            "Horror",
            "Comedy"
        };

        public List<string> AspectRatios { get; } = new()
        {
            "16:9", "9:16", "1:1", "4:3", "3:4", "21:9", "2:1"
        };

        public List<double> DurationPresets { get; } = new() { 3.0, 5.0, 10.0, 15.0, 30.0 };
        public List<int> FrameRateOptions { get; } = new() { 12, 15, 24, 30, 60 };

        public List<string> CameraMovements { get; } = new()
        {
            "Static",
            "Pan Left",
            "Pan Right", 
            "Tilt Up",
            "Tilt Down",
            "Zoom In",
            "Zoom Out",
            "Dolly Forward",
            "Dolly Back",
            "Orbit Left",
            "Orbit Right"
        };

        public List<string> ExportFormats { get; } = new() { "MP4", "AVI", "MOV", "WebM", "GIF" };
        public List<string> ExportQualities { get; } = new() { "Low", "Medium", "High", "Ultra" };
        #endregion

        #region Commands
        public ICommand GenerateCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand PlayCommand { get; }
        public ICommand PauseCommand { get; }
        public ICommand StopCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand ExportCommand { get; }
        public ICommand SetDurationCommand { get; }
        public ICommand SetStyleCommand { get; }
        public ICommand RandomizeSeedCommand { get; }
        public ICommand AddToFavoritesCommand { get; }
        public ICommand LoadPromptCommand { get; }
        public ICommand ClearHistoryCommand { get; }
        public ICommand BatchGenerateCommand { get; }
        public ICommand SeekCommand { get; }
        public ICommand LoadVideoCommand { get; }
        #endregion

        public Text2VideoViewModel()
        {
            // Initialize commands
            GenerateCommand = new SimpleRelayCommand(async () => await GenerateVideoAsync(), () => CanGenerate);
            CancelCommand = new SimpleRelayCommand(CancelGeneration, () => CanCancel);
            PlayCommand = new SimpleRelayCommand(PlayVideo, () => CanPlay);
            PauseCommand = new SimpleRelayCommand(PauseVideo);
            StopCommand = new SimpleRelayCommand(StopVideo);
            SaveCommand = new SimpleRelayCommand(SaveVideo, () => CanSave);
            ExportCommand = new SimpleRelayCommand(ExportVideo, () => CanExport);
            SetDurationCommand = new SimpleRelayCommand<double>(SetDuration);
            SetStyleCommand = new SimpleRelayCommand<string>(SetStyle);
            RandomizeSeedCommand = new SimpleRelayCommand(GenerateRandomSeed);
            AddToFavoritesCommand = new SimpleRelayCommand(AddToFavorites, () => !string.IsNullOrWhiteSpace(Prompt));
            LoadPromptCommand = new SimpleRelayCommand<string>(LoadPrompt);
            ClearHistoryCommand = new SimpleRelayCommand(ClearHistory, () => GenerationHistory.Count > 0);
            BatchGenerateCommand = new SimpleRelayCommand(async () => await BatchGenerateAsync(), () => CanGenerate && BatchCount > 1);
            SeekCommand = new SimpleRelayCommand<double>(SeekToFrame);
            LoadVideoCommand = new SimpleRelayCommand(async () => await LoadVideoAsync());

            // Initialize sample data
            InitializeSampleData();
            GenerateRandomSeed();
            UpdateEstimates();
        }

        #region Command Implementations
        private async Task GenerateVideoAsync()
        {
            try
            {
                _cancellationTokenSource = new CancellationTokenSource();
                IsGenerating = true;
                GenerationProgress = 0;
                GenerationStatus = "Initializing video generation...";

                // Add to history
                var historyItem = new VideoGenerationHistoryItem
                {
                    Id = Guid.NewGuid(),
                    Prompt = Prompt,
                    NegativePrompt = NegativePrompt,
                    DurationSeconds = DurationSeconds,
                    FrameRate = FrameRate,
                    Resolution = Resolution,
                    Steps = Steps,
                    CfgScale = CfgScale,
                    MotionStrength = MotionStrength,
                    CameraMovement = CameraMovement,
                    Model = SelectedModel,
                    Style = SelectedStyle,
                    Seed = UseRandomSeed ? new Random().Next() : Seed,
                    Timestamp = DateTime.Now
                };

                GenerationHistory.Insert(0, historyItem);

                // Simulate video generation with realistic stages
                var stages = new[]
                {
                    ("Loading model...", 5),
                    ("Generating keyframes...", 25),
                    ("Interpolating frames...", 60),
                    ("Applying motion...", 80),
                    ("Encoding video...", 95),
                    ("Finalizing...", 100)
                };

                foreach (var (status, progress) in stages)
                {
                    if (_cancellationTokenSource.Token.IsCancellationRequested)
                        break;

                    GenerationStatus = status;
                    
                    // Simulate gradual progress within each stage
                    var startProgress = GenerationProgress;
                    var targetProgress = progress;
                    var steps = Math.Max(1, (targetProgress - startProgress) / 2);
                    
                    for (double p = startProgress; p <= targetProgress && !_cancellationTokenSource.Token.IsCancellationRequested; p += steps)
                    {
                        GenerationProgress = Math.Min(p, targetProgress);
                        
                        // Simulate preview frame generation
                        if (progress >= 25 && PreviewFrames.Count < 10)
                        {
                            PreviewFrames.Add($"frame_{PreviewFrames.Count:D3}.jpg");
                        }
                        
                        await Task.Delay(150, _cancellationTokenSource.Token);
                    }
                }

                if (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    GenerationStatus = "Video generation complete!";
                    HasGeneratedVideo = true;
                    VideoSource = $"generated_video_{DateTime.Now:yyyyMMdd_HHmmss}.mp4";
                    
                    // Update history item with result
                    historyItem.VideoPath = VideoSource;
                    historyItem.FileSizeBytes = EstimatedFileSize;
                    historyItem.GenerationTime = TimeSpan.FromSeconds(30); // Simulated
                }
            }
            catch (OperationCanceledException)
            {
                GenerationStatus = "Generation cancelled";
            }
            catch (Exception ex)
            {
                GenerationStatus = $"Error: {ex.Message}";
            }
            finally
            {
                IsGenerating = false;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        private void CancelGeneration()
        {
            _cancellationTokenSource?.Cancel();
        }

        private void PlayVideo()
        {
            IsPlaying = true;
            IsPaused = false;
            // TODO: Implement actual video playback
            GenerationStatus = "Playing video...";
        }

        private void PauseVideo()
        {
            IsPaused = !IsPaused;
            GenerationStatus = IsPaused ? "Video paused" : "Playing video...";
        }

        private void StopVideo()
        {
            IsPlaying = false;
            IsPaused = false;
            CurrentFrame = 0;
            GenerationStatus = "Video stopped";
        }

        private void SaveVideo()
        {
            var saveDialog = new SaveFileDialog
            {
                Filter = "MP4 Video|*.mp4|AVI Video|*.avi|MOV Video|*.mov|All files|*.*",
                DefaultExt = ".mp4",
                FileName = $"video_{DateTime.Now:yyyyMMdd_HHmmss}"
            };

            if (saveDialog.ShowDialog() == true)
            {
                // TODO: Implement actual save functionality
                GenerationStatus = $"Video saved to {Path.GetFileName(saveDialog.FileName)}";
            }
        }

        private void ExportVideo()
        {
            // TODO: Implement export with different formats/qualities
            GenerationStatus = $"Exporting as {ExportFormat} ({ExportQuality} quality)...";
        }

        private void SetDuration(double duration)
        {
            DurationSeconds = duration;
        }

        private void SetStyle(string? style)
        {
            if (!string.IsNullOrEmpty(style))
            {
                SelectedStyle = style;
            }
        }

        private void GenerateRandomSeed()
        {
            Seed = new Random().Next(1, 2147483647);
        }

        private void AddToFavorites()
        {
            if (!string.IsNullOrWhiteSpace(Prompt) && !FavoritePrompts.Contains(Prompt))
            {
                FavoritePrompts.Add(Prompt);
                GenerationStatus = "Prompt added to favorites";
            }
        }

        private void LoadPrompt(string? prompt)
        {
            if (!string.IsNullOrEmpty(prompt))
            {
                Prompt = prompt;
            }
        }

        private void ClearHistory()
        {
            GenerationHistory.Clear();
            PreviewFrames.Clear();
            GenerationStatus = "History cleared";
        }

        private async Task BatchGenerateAsync()
        {
            // TODO: Implement batch generation
            GenerationStatus = $"Starting batch generation of {BatchCount} videos...";
        }

        private void SeekToFrame(double frame)
        {
            CurrentFrame = Math.Max(0, Math.Min(frame, TotalFrames));
        }

        private async Task LoadVideoAsync()
        {
            var openDialog = new OpenFileDialog
            {
                Filter = "Video files|*.mp4;*.avi;*.mov;*.webm|All files|*.*",
                Title = "Load Video for Reference"
            };

            if (openDialog.ShowDialog() == true)
            {
                VideoSource = openDialog.FileName;
                HasGeneratedVideo = true;
                GenerationStatus = $"Loaded: {Path.GetFileName(openDialog.FileName)}";
            }
        }

        private void UpdateResolutionFromAspectRatio()
        {
            var ratios = AspectRatio.Split(':');
            if (ratios.Length == 2 && int.TryParse(ratios[0], out int width) && int.TryParse(ratios[1], out int height))
            {
                // Calculate resolution based on aspect ratio (target around 1024 width)
                var baseWidth = 1024;
                var calculatedHeight = (int)((double)height / width * baseWidth);
                
                // Round to nearest 8 for video encoding compatibility
                calculatedHeight = (calculatedHeight / 8) * 8;
                baseWidth = (baseWidth / 8) * 8;
                
                Resolution = $"{baseWidth}x{calculatedHeight}";
            }
        }

        private void UpdateModelSettings()
        {
            // Adjust default settings based on selected model
            switch (SelectedModel)
            {
                case "AnimateDiff":
                    Steps = 25;
                    CfgScale = 7.5;
                    break;
                case "Stable Video Diffusion":
                    Steps = 20;
                    CfgScale = 7.0;
                    break;
                case "Zeroscope":
                    Steps = 30;
                    CfgScale = 8.0;
                    break;
            }
        }

        private void UpdateEstimates()
        {
            // Estimate file size (rough calculation)
            var pixels = GetPixelCount();
            var frames = (int)(DurationSeconds * FrameRate);
            EstimatedFileSize = (long)(pixels * frames * 0.1); // Rough estimate

            // Estimate generation time
            var complexity = (Steps / 20.0) * (frames / 120.0) * (pixels / (1024.0 * 576.0));
            EstimatedTime = TimeSpan.FromSeconds(Math.Max(10, complexity * 60));
        }

        private int GetPixelCount()
        {
            var parts = Resolution.Split('x');
            if (parts.Length == 2 && int.TryParse(parts[0], out int width) && int.TryParse(parts[1], out int height))
            {
                return width * height;
            }
            return 1024 * 576; // Default
        }

        private void InitializeSampleData()
        {
            // Add sample history
            GenerationHistory.Add(new VideoGenerationHistoryItem
            {
                Id = Guid.NewGuid(),
                Prompt = "A serene ocean wave crashing on a beach at sunset",
                DurationSeconds = 5.0,
                FrameRate = 24,
                Resolution = "1024x576",
                Model = "Stable Video Diffusion",
                Style = "Cinematic",
                Timestamp = DateTime.Now.AddMinutes(-15),
                VideoPath = "sample_ocean.mp4",
                FileSizeBytes = 12 * 1024 * 1024,
                GenerationTime = TimeSpan.FromSeconds(45)
            });

            // Add sample favorite prompts
            FavoritePrompts.Add("A cat walking through a garden");
            FavoritePrompts.Add("Time-lapse of clouds moving across the sky");
            FavoritePrompts.Add("A person walking down a city street at night");
        }
        #endregion

        #region INotifyPropertyChanged
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
        #endregion
    }

    #region Data Models
    public class VideoGenerationHistoryItem
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Prompt { get; set; } = "";
        public string NegativePrompt { get; set; } = "";
        public double DurationSeconds { get; set; }
        public int FrameRate { get; set; }
        public string Resolution { get; set; } = "";
        public int Steps { get; set; }
        public double CfgScale { get; set; }
        public double MotionStrength { get; set; }
        public string CameraMovement { get; set; } = "";
        public string Model { get; set; } = "";
        public string Style { get; set; } = "";
        public int Seed { get; set; }
        public DateTime Timestamp { get; set; }
        public string VideoPath { get; set; } = "";
        public long FileSizeBytes { get; set; }
        public TimeSpan GenerationTime { get; set; }
        public bool IsFavorite { get; set; }
        
        // Display Properties
        public string TimeAgo
        {
            get
            {
                var span = DateTime.Now - Timestamp;
                if (span.TotalMinutes < 1) return "Just now";
                if (span.TotalHours < 1) return $"{(int)span.TotalMinutes}m ago";
                if (span.TotalDays < 1) return $"{(int)span.TotalHours}h ago";
                return Timestamp.ToString("MM/dd HH:mm");
            }
        }

        public string DurationText => $"{DurationSeconds:F1}s";
        public string FileSizeText => FileSizeBytes < 1024 * 1024 
            ? $"{FileSizeBytes / 1024:F0} KB" 
            : $"{FileSizeBytes / 1024.0 / 1024.0:F1} MB";
        public string GenerationTimeText => GenerationTime.TotalSeconds < 60 
            ? $"{GenerationTime.TotalSeconds:F0}s" 
            : $"{GenerationTime.TotalMinutes:F1}m";
        public string VideoInfo => $"{Resolution} • {FrameRate}fps";
        public string TruncatedPrompt => Prompt.Length > 50 ? Prompt.Substring(0, 47) + "..." : Prompt;
    }

    public class VideoGenerationQueueItem
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Prompt { get; set; } = "";
        public string Parameters { get; set; } = "";
        public VideoGenerationStatus Status { get; set; } = VideoGenerationStatus.Queued;
        public double Progress { get; set; } = 0;
        public DateTime QueuedAt { get; set; } = DateTime.Now;
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public enum VideoGenerationStatus
    {
        Queued,
        Processing,
        Completed,
        Failed,
        Cancelled
    }
    #endregion
}