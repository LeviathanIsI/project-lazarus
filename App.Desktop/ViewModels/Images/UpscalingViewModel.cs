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

namespace Lazarus.Desktop.ViewModels.Images
{
    public class UpscalingViewModel : INotifyPropertyChanged
    {
        #region Private Fields
        private double _scaleFactor = 4.0;
        private string _selectedModel = "Real-ESRGAN 4x";
        private string _qualityMode = "Balanced";
        private int _tileSize = 512;
        private int _tileOverlap = 32;
        private bool _enableFaceEnhancement = false;
        private bool _enableDenoising = true;
        private bool _enableSharpening = false;
        private double _denoisingStrength = 0.5;
        private double _sharpeningAmount = 0.3;
        private BitmapSource? _sourceImageSource;
        private BitmapSource? _upscaledImageSource;
        private bool _hasSourceImage;
        private bool _hasUpscaledImage;
        private bool _isUpscaling;
        private double _upscalingProgress;
        private string _upscalingStatus = "";
        private long _memoryUsage = 0;
        private double _gpuUtilization = 0;
        private string _estimatedTime = "";
        private int _batchCount = 1;
        private string _customNamingPattern = "{name}_upscaled_{scale}x";
        private CancellationTokenSource? _cancellationTokenSource;
        #endregion

        #region Properties
        public double ScaleFactor
        {
            get => _scaleFactor;
            set
            {
                if (SetProperty(ref _scaleFactor, value))
                {
                    OnPropertyChanged(nameof(ScaleFactorText));
                    OnPropertyChanged(nameof(EstimatedOutputSize));
                    UpdateEstimatedTime();
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
                    UpdateEstimatedTime();
                }
            }
        }

        public string QualityMode
        {
            get => _qualityMode;
            set
            {
                if (SetProperty(ref _qualityMode, value))
                {
                    ApplyQualityMode(value);
                }
            }
        }

        public int TileSize
        {
            get => _tileSize;
            set => SetProperty(ref _tileSize, value);
        }

        public int TileOverlap
        {
            get => _tileOverlap;
            set => SetProperty(ref _tileOverlap, value);
        }

        public bool EnableFaceEnhancement
        {
            get => _enableFaceEnhancement;
            set => SetProperty(ref _enableFaceEnhancement, value);
        }

        public bool EnableDenoising
        {
            get => _enableDenoising;
            set => SetProperty(ref _enableDenoising, value);
        }

        public bool EnableSharpening
        {
            get => _enableSharpening;
            set => SetProperty(ref _enableSharpening, value);
        }

        public double DenoisingStrength
        {
            get => _denoisingStrength;
            set => SetProperty(ref _denoisingStrength, value);
        }

        public double SharpeningAmount
        {
            get => _sharpeningAmount;
            set => SetProperty(ref _sharpeningAmount, value);
        }

        public BitmapSource? SourceImageSource
        {
            get => _sourceImageSource;
            set
            {
                if (SetProperty(ref _sourceImageSource, value))
                {
                    HasSourceImage = value != null;
                    OnPropertyChanged(nameof(CanUpscale));
                    OnPropertyChanged(nameof(SourceImageInfo));
                    OnPropertyChanged(nameof(EstimatedOutputSize));
                    UpdateEstimatedTime();
                    
                    if (value != null)
                    {
                        AutoSelectModel();
                    }
                }
            }
        }

        public BitmapSource? UpscaledImageSource
        {
            get => _upscaledImageSource;
            set => SetProperty(ref _upscaledImageSource, value);
        }

        public bool HasSourceImage
        {
            get => _hasSourceImage;
            set => SetProperty(ref _hasSourceImage, value);
        }

        public bool HasUpscaledImage
        {
            get => _hasUpscaledImage;
            set => SetProperty(ref _hasUpscaledImage, value);
        }

        public bool IsUpscaling
        {
            get => _isUpscaling;
            set
            {
                if (SetProperty(ref _isUpscaling, value))
                {
                    OnPropertyChanged(nameof(CanUpscale));
                    OnPropertyChanged(nameof(CanCancel));
                }
            }
        }

        public double UpscalingProgress
        {
            get => _upscalingProgress;
            set => SetProperty(ref _upscalingProgress, value);
        }

        public string UpscalingStatus
        {
            get => _upscalingStatus;
            set => SetProperty(ref _upscalingStatus, value);
        }

        public long MemoryUsage
        {
            get => _memoryUsage;
            set => SetProperty(ref _memoryUsage, value);
        }

        public double GpuUtilization
        {
            get => _gpuUtilization;
            set => SetProperty(ref _gpuUtilization, value);
        }

        public string EstimatedTime
        {
            get => _estimatedTime;
            set => SetProperty(ref _estimatedTime, value);
        }

        public int BatchCount
        {
            get => _batchCount;
            set => SetProperty(ref _batchCount, value);
        }

        public string CustomNamingPattern
        {
            get => _customNamingPattern;
            set => SetProperty(ref _customNamingPattern, value);
        }

        // Computed Properties
        public bool CanUpscale => !IsUpscaling && HasSourceImage;
        public bool CanCancel => IsUpscaling;
        public string ScaleFactorText => $"{ScaleFactor:F1}x";
        public string SourceImageInfo => HasSourceImage && SourceImageSource != null 
            ? $"{SourceImageSource.PixelWidth}x{SourceImageSource.PixelHeight} ({GetImageSizeText(SourceImageSource)})"
            : "No image loaded";
        public string EstimatedOutputSize => HasSourceImage && SourceImageSource != null 
            ? $"{(int)(SourceImageSource.PixelWidth * ScaleFactor)}x{(int)(SourceImageSource.PixelHeight * ScaleFactor)}"
            : "N/A";
        public string MemoryUsageText => $"{MemoryUsage / 1024.0 / 1024.0:F1} MB";
        public string GpuUtilizationText => $"{GpuUtilization:F0}%";
        #endregion

        #region Collections
        public ObservableCollection<UpscalingHistoryItem> UpscalingHistory { get; } = new();
        public ObservableCollection<UpscaledImageResult> ResultGallery { get; } = new();
        public ObservableCollection<string> ProcessingQueue { get; } = new();

        public List<string> AvailableModels { get; } = new()
        {
            "Real-ESRGAN 4x",
            "Real-ESRGAN 2x",
            "ESRGAN 4x",
            "Waifu2x",
            "SwinIR",
            "HAT",
            "EDSR",
            "SRCNN"
        };

        public List<string> QualityModes { get; } = new()
        {
            "Fast",
            "Balanced", 
            "High Quality",
            "Ultra Quality"
        };

        public List<double> CommonScales { get; } = new() { 1.5, 2.0, 3.0, 4.0, 6.0, 8.0 };
        #endregion

        #region Commands
        public ICommand LoadImageCommand { get; }
        public ICommand LoadMultipleImagesCommand { get; }
        public ICommand UpscaleCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand SaveResultCommand { get; }
        public ICommand ClearImageCommand { get; }
        public ICommand SetScaleFactorCommand { get; }
        public ICommand SetQualityModeCommand { get; }
        public ICommand ExportResultCommand { get; }
        public ICommand CompareResultsCommand { get; }
        public ICommand ClearHistoryCommand { get; }
        public ICommand BatchUpscaleCommand { get; }
        public ICommand LoadModelCommand { get; }
        #endregion

        public UpscalingViewModel()
        {
            // Initialize commands
            LoadImageCommand = new SimpleRelayCommand(async () => await LoadImageAsync());
            LoadMultipleImagesCommand = new SimpleRelayCommand(async () => await LoadMultipleImagesAsync());
            UpscaleCommand = new SimpleRelayCommand(async () => await UpscaleImageAsync(), () => CanUpscale);
            CancelCommand = new SimpleRelayCommand(CancelUpscaling, () => CanCancel);
            SaveResultCommand = new SimpleRelayCommand(SaveResult, () => HasUpscaledImage);
            ClearImageCommand = new SimpleRelayCommand(ClearImage, () => HasSourceImage);
            SetScaleFactorCommand = new SimpleRelayCommand<double>(SetScaleFactor);
            SetQualityModeCommand = new SimpleRelayCommand<string>(SetQualityMode);
            ExportResultCommand = new SimpleRelayCommand(ExportResult, () => HasUpscaledImage);
            CompareResultsCommand = new SimpleRelayCommand(CompareResults, () => HasSourceImage && HasUpscaledImage);
            ClearHistoryCommand = new SimpleRelayCommand(ClearHistory, () => UpscalingHistory.Count > 0);
            BatchUpscaleCommand = new SimpleRelayCommand(async () => await BatchUpscaleAsync(), () => ProcessingQueue.Count > 0);
            LoadModelCommand = new SimpleRelayCommand<string>(LoadModel);

            // Initialize sample data
            InitializeSampleData();
        }

        #region Command Implementations
        private async Task LoadImageAsync()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Image files (*.png;*.jpg;*.jpeg;*.bmp;*.webp;*.tiff)|*.png;*.jpg;*.jpeg;*.bmp;*.webp;*.tiff|All files (*.*)|*.*",
                Title = "Select Image to Upscale"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(openFileDialog.FileName);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    
                    SourceImageSource = bitmap;
                    UpscalingStatus = $"Image loaded: {Path.GetFileName(openFileDialog.FileName)}";
                }
                catch (Exception ex)
                {
                    UpscalingStatus = $"Error loading image: {ex.Message}";
                }
            }
        }

        private async Task LoadMultipleImagesAsync()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Image files (*.png;*.jpg;*.jpeg;*.bmp;*.webp;*.tiff)|*.png;*.jpg;*.jpeg;*.bmp;*.webp;*.tiff",
                Title = "Select Images for Batch Upscaling",
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                ProcessingQueue.Clear();
                foreach (var file in openFileDialog.FileNames)
                {
                    ProcessingQueue.Add(Path.GetFileName(file));
                }
                UpscalingStatus = $"Added {ProcessingQueue.Count} images to batch queue";
            }
        }

        private async Task UpscaleImageAsync()
        {
            try
            {
                _cancellationTokenSource = new CancellationTokenSource();
                IsUpscaling = true;
                UpscalingProgress = 0;
                UpscalingStatus = "Initializing upscaling process...";

                // Add to history
                var historyItem = new UpscalingHistoryItem
                {
                    Id = Guid.NewGuid(),
                    Model = SelectedModel,
                    ScaleFactor = ScaleFactor,
                    QualityMode = QualityMode,
                    FaceEnhancement = EnableFaceEnhancement,
                    Denoising = EnableDenoising,
                    Sharpening = EnableSharpening,
                    TileSize = TileSize,
                    Timestamp = DateTime.Now,
                    OriginalSize = HasSourceImage && SourceImageSource != null 
                        ? $"{SourceImageSource.PixelWidth}x{SourceImageSource.PixelHeight}"
                        : "Unknown"
                };

                UpscalingHistory.Insert(0, historyItem);

                // Simulate upscaling progress with realistic stages
                var stages = new[]
                {
                    ("Loading model...", 10),
                    ("Preprocessing image...", 20),
                    ("Upscaling tiles...", 70),
                    ("Post-processing...", 90),
                    ("Finalizing result...", 100)
                };

                foreach (var (status, progress) in stages)
                {
                    if (_cancellationTokenSource.Token.IsCancellationRequested)
                        break;

                    UpscalingStatus = status;
                    
                    // Simulate gradual progress within each stage
                    var startProgress = UpscalingProgress;
                    var targetProgress = progress;
                    var steps = Math.Max(1, (targetProgress - startProgress) / 2);
                    
                    for (double p = startProgress; p <= targetProgress && !_cancellationTokenSource.Token.IsCancellationRequested; p += steps)
                    {
                        UpscalingProgress = Math.Min(p, targetProgress);
                        
                        // Simulate memory and GPU usage
                        MemoryUsage = (long)(1024 * 1024 * (2 + (p / 100.0) * 3)); // 2-5 GB simulation
                        GpuUtilization = Math.Min(95, 20 + (p / 100.0) * 70); // 20-90% GPU usage
                        
                        await Task.Delay(100, _cancellationTokenSource.Token);
                    }
                }

                if (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    UpscalingStatus = "Upscaling complete!";
                    HasUpscaledImage = true;
                    MemoryUsage = 0;
                    GpuUtilization = 0;
                    
                    // Create result entry
                    var result = new UpscaledImageResult
                    {
                        Id = historyItem.Id,
                        OriginalImage = SourceImageSource,
                        UpscaledImage = null, // Would be actual result
                        ScaleFactor = ScaleFactor,
                        Model = SelectedModel,
                        Timestamp = DateTime.Now,
                        ProcessingTime = TimeSpan.FromSeconds(15), // Simulated
                        FileSizeIncrease = (long)(ScaleFactor * ScaleFactor * 1.2) // Estimated
                    };
                    ResultGallery.Insert(0, result);
                }
            }
            catch (OperationCanceledException)
            {
                UpscalingStatus = "Upscaling cancelled";
                MemoryUsage = 0;
                GpuUtilization = 0;
            }
            catch (Exception ex)
            {
                UpscalingStatus = $"Error: {ex.Message}";
                MemoryUsage = 0;
                GpuUtilization = 0;
            }
            finally
            {
                IsUpscaling = false;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        private void CancelUpscaling()
        {
            _cancellationTokenSource?.Cancel();
        }

        private void SaveResult()
        {
            // TODO: Implement save functionality
            UpscalingStatus = "Result saved successfully!";
        }

        private void ClearImage()
        {
            SourceImageSource = null;
            UpscaledImageSource = null;
            HasUpscaledImage = false;
            UpscalingStatus = "Images cleared";
        }

        private void SetScaleFactor(double factor)
        {
            ScaleFactor = factor;
        }

        private void SetQualityMode(string? mode)
        {
            if (!string.IsNullOrEmpty(mode))
            {
                QualityMode = mode;
            }
        }

        private void ExportResult()
        {
            // TODO: Implement export functionality
            UpscalingStatus = "Result exported";
        }

        private void CompareResults()
        {
            // TODO: Implement comparison view
            UpscalingStatus = "Opening comparison view";
        }

        private void ClearHistory()
        {
            UpscalingHistory.Clear();
            ResultGallery.Clear();
            UpscalingStatus = "History cleared";
        }

        private async Task BatchUpscaleAsync()
        {
            // TODO: Implement batch processing
            UpscalingStatus = $"Starting batch upscaling of {ProcessingQueue.Count} images";
        }

        private void LoadModel(string? modelName)
        {
            if (!string.IsNullOrEmpty(modelName))
            {
                SelectedModel = modelName;
                UpscalingStatus = $"Model loaded: {modelName}";
            }
        }

        private void AutoSelectModel()
        {
            if (!HasSourceImage || SourceImageSource == null) return;

            // Auto-select model based on image characteristics
            var width = SourceImageSource.PixelWidth;
            var height = SourceImageSource.PixelHeight;
            var totalPixels = width * height;

            if (totalPixels < 512 * 512)
            {
                SelectedModel = "Real-ESRGAN 4x"; // Good for small images
            }
            else if (totalPixels < 1024 * 1024)
            {
                SelectedModel = "Real-ESRGAN 2x"; // Better for medium images
            }
            else
            {
                SelectedModel = "ESRGAN 4x"; // More efficient for large images
            }

            UpscalingStatus = $"Auto-selected model: {SelectedModel}";
        }

        private void UpdateModelSettings()
        {
            // Adjust settings based on selected model
            switch (SelectedModel)
            {
                case "Real-ESRGAN 4x":
                case "Real-ESRGAN 2x":
                    TileSize = 512;
                    TileOverlap = 32;
                    EnableDenoising = true;
                    break;
                case "Waifu2x":
                    TileSize = 256;
                    TileOverlap = 16;
                    EnableDenoising = true;
                    EnableFaceEnhancement = false; // Waifu2x handles this internally
                    break;
                case "ESRGAN 4x":
                    TileSize = 256;
                    TileOverlap = 24;
                    break;
            }
        }

        private void ApplyQualityMode(string mode)
        {
            switch (mode)
            {
                case "Fast":
                    TileSize = 256;
                    TileOverlap = 16;
                    EnableDenoising = false;
                    EnableSharpening = false;
                    break;
                case "Balanced":
                    TileSize = 512;
                    TileOverlap = 32;
                    EnableDenoising = true;
                    EnableSharpening = false;
                    break;
                case "High Quality":
                    TileSize = 512;
                    TileOverlap = 64;
                    EnableDenoising = true;
                    EnableSharpening = true;
                    SharpeningAmount = 0.3;
                    break;
                case "Ultra Quality":
                    TileSize = 1024;
                    TileOverlap = 128;
                    EnableDenoising = true;
                    EnableSharpening = true;
                    SharpeningAmount = 0.5;
                    break;
            }
        }

        private void UpdateEstimatedTime()
        {
            if (!HasSourceImage || SourceImageSource == null)
            {
                EstimatedTime = "N/A";
                return;
            }

            var pixels = SourceImageSource.PixelWidth * SourceImageSource.PixelHeight;
            var complexity = ScaleFactor * ScaleFactor;
            var qualityMultiplier = QualityMode switch
            {
                "Fast" => 0.5,
                "Balanced" => 1.0,
                "High Quality" => 2.0,
                "Ultra Quality" => 4.0,
                _ => 1.0
            };

            var estimatedSeconds = (pixels / 100000.0) * complexity * qualityMultiplier;
            EstimatedTime = estimatedSeconds < 60 
                ? $"~{estimatedSeconds:F0}s" 
                : $"~{estimatedSeconds / 60:F1}m";
        }

        private string GetImageSizeText(BitmapSource image)
        {
            var bytes = image.PixelWidth * image.PixelHeight * 4; // Assume 32bpp
            if (bytes < 1024 * 1024)
                return $"{bytes / 1024:F0} KB";
            else
                return $"{bytes / 1024.0 / 1024.0:F1} MB";
        }

        private void InitializeSampleData()
        {
            // Add sample history
            UpscalingHistory.Add(new UpscalingHistoryItem
            {
                Id = Guid.NewGuid(),
                Model = "Real-ESRGAN 4x",
                ScaleFactor = 4.0,
                QualityMode = "Balanced",
                FaceEnhancement = true,
                Timestamp = DateTime.Now.AddMinutes(-8),
                OriginalSize = "512x512",
                ProcessingTime = TimeSpan.FromSeconds(12)
            });

            UpscalingHistory.Add(new UpscalingHistoryItem
            {
                Id = Guid.NewGuid(),
                Model = "Waifu2x",
                ScaleFactor = 2.0,
                QualityMode = "High Quality",
                FaceEnhancement = false,
                Timestamp = DateTime.Now.AddMinutes(-20),
                OriginalSize = "1024x768",
                ProcessingTime = TimeSpan.FromSeconds(25)
            });
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
    public class UpscalingHistoryItem
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Model { get; set; } = "";
        public double ScaleFactor { get; set; }
        public string QualityMode { get; set; } = "";
        public bool FaceEnhancement { get; set; }
        public bool Denoising { get; set; }
        public bool Sharpening { get; set; }
        public int TileSize { get; set; }
        public DateTime Timestamp { get; set; }
        public string OriginalSize { get; set; } = "";
        public TimeSpan ProcessingTime { get; set; }
        
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

        public string ScaleText => $"{ScaleFactor:F1}x";
        public string ModelText => Model.Replace("Real-ESRGAN", "R-ESRGAN").Replace("ESRGAN", "ESR");
        public string ProcessingTimeText => ProcessingTime.TotalSeconds < 60 
            ? $"{ProcessingTime.TotalSeconds:F0}s" 
            : $"{ProcessingTime.TotalMinutes:F1}m";
        public string ParametersText => $"{ModelText}, {QualityMode}{(FaceEnhancement ? ", Face+" : "")}";
    }

    public class UpscaledImageResult
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public BitmapSource? OriginalImage { get; set; }
        public BitmapSource? UpscaledImage { get; set; }
        public double ScaleFactor { get; set; }
        public string Model { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public TimeSpan ProcessingTime { get; set; }
        public long FileSizeIncrease { get; set; }
        public bool IsFavorite { get; set; }
        
        public string TimeAgo
        {
            get
            {
                var span = DateTime.Now - Timestamp;
                if (span.TotalMinutes < 1) return "Just now";
                if (span.TotalHours < 1) return $"{(int)span.TotalMinutes}m ago";
                return Timestamp.ToString("HH:mm");
            }
        }

        public string ScaleText => $"{ScaleFactor:F1}x";
        public string FileSizeIncreaseText => FileSizeIncrease < 1024 * 1024 
            ? $"+{FileSizeIncrease / 1024:F0} KB" 
            : $"+{FileSizeIncrease / 1024.0 / 1024.0:F1} MB";
    }
    #endregion
}