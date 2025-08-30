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
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Lazarus.Desktop.Helpers;

namespace Lazarus.Desktop.ViewModels.Video
{
    public class Video2VideoViewModel : INotifyPropertyChanged
    {
        #region Private Fields
        private string _prompt = "";
        private string _negativePrompt = "";
        private string _sourceVideoPath = "";
        private BitmapSource? _videoThumbnail;
        private double _strength = 0.8;
        private double _denoisingStrength = 0.5;
        private double _guidanceScale = 7.0;
        private int _steps = 20;
        private string _selectedModel = "Stable Video Diffusion";
        private string _selectedStyle = "None";
        private string _styleTransferMode = "Full";
        private bool _maintainTemporalConsistency = true;
        private double _temporalConsistencyStrength = 0.8;
        private int _seed = -1;
        private bool _useRandomSeed = true;
        private string _outputVideoPath = "";
        private bool _hasSourceVideo;
        private bool _hasOutputVideo;
        private bool _isProcessing;
        private double _processingProgress;
        private string _processingStatus = "";
        private double _currentFrame = 0;
        private double _totalFrames = 0;
        private string _videoInfo = "";
        private long _estimatedFileSize = 0;
        private TimeSpan _estimatedProcessingTime = TimeSpan.Zero;
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
                    OnPropertyChanged(nameof(CanProcess));
                    UpdateEstimates();
                }
            }
        }

        public string NegativePrompt
        {
            get => _negativePrompt;
            set => SetProperty(ref _negativePrompt, value);
        }

        public string SourceVideoPath
        {
            get => _sourceVideoPath;
            set
            {
                if (SetProperty(ref _sourceVideoPath, value))
                {
                    HasSourceVideo = !string.IsNullOrEmpty(value);
                    OnPropertyChanged(nameof(CanProcess));
                    if (HasSourceVideo)
                    {
                        LoadVideoInfo();
                    }
                }
            }
        }

        public BitmapSource? VideoThumbnail
        {
            get => _videoThumbnail;
            set => SetProperty(ref _videoThumbnail, value);
        }

        public double Strength
        {
            get => _strength;
            set
            {
                if (SetProperty(ref _strength, value))
                {
                    UpdateEstimates();
                }
            }
        }

        public double DenoisingStrength
        {
            get => _denoisingStrength;
            set => SetProperty(ref _denoisingStrength, value);
        }

        public double GuidanceScale
        {
            get => _guidanceScale;
            set => SetProperty(ref _guidanceScale, value);
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

        public string SelectedModel
        {
            get => _selectedModel;
            set
            {
                if (SetProperty(ref _selectedModel, value))
                {
                    UpdateModelSettings();
                    UpdateEstimates();
                }
            }
        }

        public string SelectedStyle
        {
            get => _selectedStyle;
            set => SetProperty(ref _selectedStyle, value);
        }

        public string StyleTransferMode
        {
            get => _styleTransferMode;
            set => SetProperty(ref _styleTransferMode, value);
        }

        public bool MaintainTemporalConsistency
        {
            get => _maintainTemporalConsistency;
            set => SetProperty(ref _maintainTemporalConsistency, value);
        }

        public double TemporalConsistencyStrength
        {
            get => _temporalConsistencyStrength;
            set => SetProperty(ref _temporalConsistencyStrength, value);
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

        public string OutputVideoPath
        {
            get => _outputVideoPath;
            set => SetProperty(ref _outputVideoPath, value);
        }

        public bool HasSourceVideo
        {
            get => _hasSourceVideo;
            set => SetProperty(ref _hasSourceVideo, value);
        }

        public bool HasOutputVideo
        {
            get => _hasOutputVideo;
            set
            {
                if (SetProperty(ref _hasOutputVideo, value))
                {
                    OnPropertyChanged(nameof(CanSave));
                    OnPropertyChanged(nameof(CanExport));
                    OnPropertyChanged(nameof(CanCompare));
                }
            }
        }

        public bool IsProcessing
        {
            get => _isProcessing;
            set
            {
                if (SetProperty(ref _isProcessing, value))
                {
                    OnPropertyChanged(nameof(CanProcess));
                    OnPropertyChanged(nameof(CanCancel));
                }
            }
        }

        public double ProcessingProgress
        {
            get => _processingProgress;
            set => SetProperty(ref _processingProgress, value);
        }

        public string ProcessingStatus
        {
            get => _processingStatus;
            set => SetProperty(ref _processingStatus, value);
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

        public string VideoInfo
        {
            get => _videoInfo;
            set => SetProperty(ref _videoInfo, value);
        }

        public long EstimatedFileSize
        {
            get => _estimatedFileSize;
            set => SetProperty(ref _estimatedFileSize, value);
        }

        public TimeSpan EstimatedProcessingTime
        {
            get => _estimatedProcessingTime;
            set => SetProperty(ref _estimatedProcessingTime, value);
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
        public bool CanProcess => !IsProcessing && HasSourceVideo && !string.IsNullOrWhiteSpace(Prompt);
        public bool CanCancel => IsProcessing;
        public bool CanSave => HasOutputVideo && !IsProcessing;
        public bool CanExport => HasOutputVideo && !IsProcessing;
        public bool CanCompare => HasSourceVideo && HasOutputVideo && !IsProcessing;
        public string CurrentTimeText => TimeSpan.FromSeconds(CurrentFrame / 30.0).ToString(@"mm\:ss\.ff");
        public string TotalTimeText => TimeSpan.FromSeconds(TotalFrames / 30.0).ToString(@"mm\:ss\.ff");
        public double PlaybackProgress => TotalFrames > 0 ? (CurrentFrame / TotalFrames) * 100 : 0;
        public string FileSizeText => EstimatedFileSize < 1024 * 1024 
            ? $"~{EstimatedFileSize / 1024:F0} KB" 
            : $"~{EstimatedFileSize / 1024.0 / 1024.0:F1} MB";
        public string ProcessingTimeText => EstimatedProcessingTime.TotalMinutes < 1 
            ? $"~{EstimatedProcessingTime.TotalSeconds:F0}s" 
            : $"~{EstimatedProcessingTime.TotalMinutes:F1}m";
        public string SourceVideoInfo => HasSourceVideo 
            ? $"{Path.GetFileName(SourceVideoPath)} • {VideoInfo}"
            : "No video loaded";
        #endregion

        #region Collections
        public ObservableCollection<VideoTransformationHistoryItem> TransformationHistory { get; } = new();
        public ObservableCollection<VideoProcessingQueueItem> ProcessingQueue { get; } = new();
        public ObservableCollection<string> FavoritePrompts { get; } = new();
        public ObservableCollection<BitmapSource> PreviewFrames { get; } = new();

        public List<string> AvailableModels { get; } = new()
        {
            "Stable Video Diffusion",
            "AnimateDiff",
            "VideoCrafter",
            "LaVie",
            "Show-1",
            "CogVideo",
            "ModelScope",
            "Zeroscope"
        };

        public List<string> StylePresets { get; } = new()
        {
            "None",
            "Cartoon",
            "Realistic",
            "Art Style",
            "Anime",
            "Oil Painting",
            "Watercolor",
            "Sketch",
            "Photorealistic",
            "Cinematic"
        };

        public List<string> StyleTransferModes { get; } = new()
        {
            "Full", "Partial", "Selective", "Adaptive"
        };

        public List<string> ExportFormats { get; } = new() { "MP4", "AVI", "MOV", "WebM", "MKV" };
        public List<string> ExportQualities { get; } = new() { "Low", "Medium", "High", "Ultra", "Lossless" };
        #endregion

        #region Commands
        public ICommand LoadVideoCommand { get; }
        public ICommand LoadMultipleVideosCommand { get; }
        public ICommand ProcessVideoCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand PlaySourceCommand { get; }
        public ICommand PlayOutputCommand { get; }
        public ICommand PauseCommand { get; }
        public ICommand StopCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand ExportCommand { get; }
        public ICommand CompareCommand { get; }
        public ICommand ClearVideoCommand { get; }
        public ICommand SetStyleCommand { get; }
        public ICommand SetStrengthCommand { get; }
        public ICommand RandomizeSeedCommand { get; }
        public ICommand AddToFavoritesCommand { get; }
        public ICommand LoadPromptCommand { get; }
        public ICommand ClearHistoryCommand { get; }
        public ICommand BatchProcessCommand { get; }
        public ICommand SeekCommand { get; }
        #endregion

        public Video2VideoViewModel()
        {
            // Initialize commands
            LoadVideoCommand = new SimpleRelayCommand(async () => await LoadVideoAsync());
            LoadMultipleVideosCommand = new SimpleRelayCommand(async () => await LoadMultipleVideosAsync());
            ProcessVideoCommand = new SimpleRelayCommand(async () => await ProcessVideoAsync(), () => CanProcess);
            CancelCommand = new SimpleRelayCommand(CancelProcessing, () => CanCancel);
            PlaySourceCommand = new SimpleRelayCommand(PlaySourceVideo);
            PlayOutputCommand = new SimpleRelayCommand(PlayOutputVideo);
            PauseCommand = new SimpleRelayCommand(PauseVideo);
            StopCommand = new SimpleRelayCommand(StopVideo);
            SaveCommand = new SimpleRelayCommand(SaveVideo, () => CanSave);
            ExportCommand = new SimpleRelayCommand(ExportVideo, () => CanExport);
            CompareCommand = new SimpleRelayCommand(CompareVideos, () => CanCompare);
            ClearVideoCommand = new SimpleRelayCommand(ClearVideo, () => HasSourceVideo);
            SetStyleCommand = new SimpleRelayCommand<string>(SetStyle);
            SetStrengthCommand = new SimpleRelayCommand<double>(SetStrength);
            RandomizeSeedCommand = new SimpleRelayCommand(GenerateRandomSeed);
            AddToFavoritesCommand = new SimpleRelayCommand(AddToFavorites, () => !string.IsNullOrWhiteSpace(Prompt));
            LoadPromptCommand = new SimpleRelayCommand<string>(LoadPrompt);
            ClearHistoryCommand = new SimpleRelayCommand(ClearHistory, () => TransformationHistory.Count > 0);
            BatchProcessCommand = new SimpleRelayCommand(async () => await BatchProcessAsync(), () => ProcessingQueue.Count > 0);
            SeekCommand = new SimpleRelayCommand<double>(SeekToFrame);

            // Initialize sample data
            InitializeSampleData();
            GenerateRandomSeed();
        }

        #region Command Implementations
        private async Task LoadVideoAsync()
        {
            var openDialog = new OpenFileDialog
            {
                Filter = "Video files|*.mp4;*.avi;*.mov;*.mkv;*.webm;*.wmv|All files|*.*",
                Title = "Select Source Video"
            };

            if (openDialog.ShowDialog() == true)
            {
                try
                {
                    SourceVideoPath = openDialog.FileName;
                    ProcessingStatus = $"Video loaded: {Path.GetFileName(openDialog.FileName)}";
                    
                    // TODO: Generate actual thumbnail
                    // For now, simulate thumbnail generation
                    await Task.Delay(500);
                    ProcessingStatus = "Video analyzed successfully";
                }
                catch (Exception ex)
                {
                    ProcessingStatus = $"Error loading video: {ex.Message}";
                }
            }
        }

        private async Task LoadMultipleVideosAsync()
        {
            var openDialog = new OpenFileDialog
            {
                Filter = "Video files|*.mp4;*.avi;*.mov;*.mkv;*.webm;*.wmv",
                Title = "Select Videos for Batch Processing",
                Multiselect = true
            };

            if (openDialog.ShowDialog() == true)
            {
                ProcessingQueue.Clear();
                foreach (var file in openDialog.FileNames)
                {
                    ProcessingQueue.Add(new VideoProcessingQueueItem
                    {
                        SourcePath = file,
                        FileName = Path.GetFileName(file),
                        Status = VideoProcessingStatus.Queued,
                        QueuedAt = DateTime.Now
                    });
                }
                ProcessingStatus = $"Added {ProcessingQueue.Count} videos to batch queue";
            }
        }

        private async Task ProcessVideoAsync()
        {
            try
            {
                _cancellationTokenSource = new CancellationTokenSource();
                IsProcessing = true;
                ProcessingProgress = 0;
                ProcessingStatus = "Initializing video processing...";

                // Add to history
                var historyItem = new VideoTransformationHistoryItem
                {
                    Id = Guid.NewGuid(),
                    SourcePath = SourceVideoPath,
                    Prompt = Prompt,
                    NegativePrompt = NegativePrompt,
                    Strength = Strength,
                    DenoisingStrength = DenoisingStrength,
                    GuidanceScale = GuidanceScale,
                    Steps = Steps,
                    Model = SelectedModel,
                    Style = SelectedStyle,
                    StyleTransferMode = StyleTransferMode,
                    TemporalConsistency = MaintainTemporalConsistency,
                    Seed = UseRandomSeed ? new Random().Next() : Seed,
                    Timestamp = DateTime.Now
                };

                TransformationHistory.Insert(0, historyItem);

                // Simulate realistic video processing stages
                var stages = new[]
                {
                    ("Loading model...", 5),
                    ("Analyzing source video...", 15),
                    ("Extracting frames...", 25),
                    ("Processing frames...", 70),
                    ("Applying temporal consistency...", 85),
                    ("Encoding output video...", 95),
                    ("Finalizing...", 100)
                };

                foreach (var (status, progress) in stages)
                {
                    if (_cancellationTokenSource.Token.IsCancellationRequested)
                        break;

                    ProcessingStatus = status;
                    
                    // Simulate gradual progress within each stage
                    var startProgress = ProcessingProgress;
                    var targetProgress = progress;
                    var steps = Math.Max(1, (targetProgress - startProgress) / 3);
                    
                    for (double p = startProgress; p <= targetProgress && !_cancellationTokenSource.Token.IsCancellationRequested; p += steps)
                    {
                        ProcessingProgress = Math.Min(p, targetProgress);
                        
                        // Simulate preview frame generation
                        if (progress >= 25 && PreviewFrames.Count < 20)
                        {
                            // TODO: Generate actual preview frames
                        }
                        
                        await Task.Delay(200, _cancellationTokenSource.Token);
                    }
                }

                if (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    ProcessingStatus = "Video transformation complete!";
                    HasOutputVideo = true;
                    OutputVideoPath = $"transformed_{DateTime.Now:yyyyMMdd_HHmmss}.mp4";
                    
                    // Update history item with result
                    historyItem.OutputPath = OutputVideoPath;
                    historyItem.FileSizeBytes = EstimatedFileSize;
                    historyItem.ProcessingTime = TimeSpan.FromSeconds(45); // Simulated
                }
            }
            catch (OperationCanceledException)
            {
                ProcessingStatus = "Processing cancelled";
            }
            catch (Exception ex)
            {
                ProcessingStatus = $"Error: {ex.Message}";
            }
            finally
            {
                IsProcessing = false;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        private void CancelProcessing()
        {
            _cancellationTokenSource?.Cancel();
        }

        private void PlaySourceVideo()
        {
            ProcessingStatus = "Playing source video...";
        }

        private void PlayOutputVideo()
        {
            ProcessingStatus = "Playing transformed video...";
        }

        private void PauseVideo()
        {
            ProcessingStatus = "Video paused";
        }

        private void StopVideo()
        {
            CurrentFrame = 0;
            ProcessingStatus = "Video stopped";
        }

        private void SaveVideo()
        {
            var saveDialog = new SaveFileDialog
            {
                Filter = "MP4 Video|*.mp4|AVI Video|*.avi|MOV Video|*.mov|All files|*.*",
                DefaultExt = ".mp4",
                FileName = $"transformed_video_{DateTime.Now:yyyyMMdd_HHmmss}"
            };

            if (saveDialog.ShowDialog() == true)
            {
                ProcessingStatus = $"Video saved to {Path.GetFileName(saveDialog.FileName)}";
            }
        }

        private void ExportVideo()
        {
            ProcessingStatus = $"Exporting as {ExportFormat} ({ExportQuality} quality)...";
        }

        private void CompareVideos()
        {
            ProcessingStatus = "Opening comparison view...";
        }

        private void ClearVideo()
        {
            SourceVideoPath = "";
            OutputVideoPath = "";
            VideoThumbnail = null;
            HasOutputVideo = false;
            VideoInfo = "";
            CurrentFrame = 0;
            TotalFrames = 0;
            ProcessingStatus = "Videos cleared";
        }

        private void SetStyle(string? style)
        {
            if (!string.IsNullOrEmpty(style))
            {
                SelectedStyle = style;
            }
        }

        private void SetStrength(double strength)
        {
            Strength = strength;
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
                ProcessingStatus = "Prompt added to favorites";
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
            TransformationHistory.Clear();
            PreviewFrames.Clear();
            ProcessingStatus = "History cleared";
        }

        private async Task BatchProcessAsync()
        {
            ProcessingStatus = $"Starting batch processing of {ProcessingQueue.Count} videos...";
            // TODO: Implement batch processing logic
        }

        private void SeekToFrame(double frame)
        {
            CurrentFrame = Math.Max(0, Math.Min(frame, TotalFrames));
        }

        private void LoadVideoInfo()
        {
            if (!HasSourceVideo) return;

            // TODO: Load actual video metadata
            // For now, simulate video info
            VideoInfo = "1920x1080 • 30fps • 00:15";
            TotalFrames = 450; // 15 seconds at 30fps
            UpdateEstimates();
        }

        private void UpdateModelSettings()
        {
            // Adjust default settings based on selected model
            switch (SelectedModel)
            {
                case "Stable Video Diffusion":
                    Steps = 20;
                    GuidanceScale = 7.0;
                    break;
                case "AnimateDiff":
                    Steps = 25;
                    GuidanceScale = 7.5;
                    break;
                case "VideoCrafter":
                    Steps = 30;
                    GuidanceScale = 8.0;
                    break;
            }
        }

        private void UpdateEstimates()
        {
            if (!HasSourceVideo) return;

            // Estimate processing time based on complexity
            var complexity = (Steps / 20.0) * (Strength * 2) * (TotalFrames / 300.0);
            EstimatedProcessingTime = TimeSpan.FromSeconds(Math.Max(15, complexity * 120));

            // Estimate file size (rough calculation)
            EstimatedFileSize = (long)(TotalFrames * 1920 * 1080 * 0.05); // Rough estimate
        }

        private void InitializeSampleData()
        {
            // Add sample history
            TransformationHistory.Add(new VideoTransformationHistoryItem
            {
                Id = Guid.NewGuid(),
                SourcePath = "sample_video.mp4",
                Prompt = "Transform to cartoon style",
                Strength = 0.8,
                Model = "AnimateDiff",
                Style = "Cartoon",
                Timestamp = DateTime.Now.AddMinutes(-20),
                OutputPath = "cartoon_video.mp4",
                FileSizeBytes = 25 * 1024 * 1024,
                ProcessingTime = TimeSpan.FromSeconds(65)
            });

            // Add sample favorite prompts
            FavoritePrompts.Add("Transform to anime style");
            FavoritePrompts.Add("Make it look like oil painting");
            FavoritePrompts.Add("Convert to realistic CGI");
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
    public class VideoTransformationHistoryItem
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string SourcePath { get; set; } = "";
        public string Prompt { get; set; } = "";
        public string NegativePrompt { get; set; } = "";
        public double Strength { get; set; }
        public double DenoisingStrength { get; set; }
        public double GuidanceScale { get; set; }
        public int Steps { get; set; }
        public string Model { get; set; } = "";
        public string Style { get; set; } = "";
        public string StyleTransferMode { get; set; } = "";
        public bool TemporalConsistency { get; set; }
        public int Seed { get; set; }
        public DateTime Timestamp { get; set; }
        public string OutputPath { get; set; } = "";
        public long FileSizeBytes { get; set; }
        public TimeSpan ProcessingTime { get; set; }
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

        public string SourceFileName => Path.GetFileName(SourcePath);
        public string FileSizeText => FileSizeBytes < 1024 * 1024 
            ? $"{FileSizeBytes / 1024:F0} KB" 
            : $"{FileSizeBytes / 1024.0 / 1024.0:F1} MB";
        public string ProcessingTimeText => ProcessingTime.TotalSeconds < 60 
            ? $"{ProcessingTime.TotalSeconds:F0}s" 
            : $"{ProcessingTime.TotalMinutes:F1}m";
        public string TruncatedPrompt => Prompt.Length > 40 ? Prompt.Substring(0, 37) + "..." : Prompt;
        public string ParametersText => $"{Model}, {Style}, {Strength:F1}";
    }

    public class VideoProcessingQueueItem
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string SourcePath { get; set; } = "";
        public string FileName { get; set; } = "";
        public string Parameters { get; set; } = "";
        public VideoProcessingStatus Status { get; set; } = VideoProcessingStatus.Queued;
        public double Progress { get; set; } = 0;
        public DateTime QueuedAt { get; set; } = DateTime.Now;
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? ErrorMessage { get; set; }
        public string? OutputPath { get; set; }
    }

    public enum VideoProcessingStatus
    {
        Queued,
        Processing,
        Completed,
        Failed,
        Cancelled
    }
    #endregion
}

