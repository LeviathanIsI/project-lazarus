using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Win32;
using Lazarus.Desktop.Helpers;

namespace Lazarus.Desktop.ViewModels.Video
{
    public class TemporalEffectsViewModel : INotifyPropertyChanged
    {
        #region Private Fields
        private string _sourceVideoPath = "";
        private bool _hasSourceVideo;
        private string _selectedEffect = "Speed Change";
        private double _speedMultiplier = 1.0;
        private string _speedPreset = "Normal";
        private int _targetFrameRate = 60;
        private int _interpolationQuality = 5;
        private string _interpolationMethod = "Optical Flow";
        private double _blurIntensity = 0.5;
        private bool _useAdaptiveBlur = true;
        private double _stabilizationStrength = 0.8;
        private bool _cropToFit = true;
        private string _transitionType = "Fade";
        private double _transitionDuration = 1.0;
        private string _colorGrading = "None";
        private double _saturation = 1.0;
        private double _brightness = 0.0;
        private double _contrast = 1.0;
        private double _exposure = 0.0;
        private bool _enableTemporalFiltering = true;
        private double _temporalFilterStrength = 0.5;
        private bool _preserveOriginalDuration = false;
        private string _processingPriority = "Normal";
        private bool _hasPreview;
        private bool _isProcessing;
        private double _processingProgress;
        private string _processingStatus = "";
        private double _timelinePosition = 0;
        private double _timelineDuration = 100;
        private string _exportFormat = "MP4";
        private string _exportQuality = "High";
        private CancellationTokenSource? _cancellationTokenSource;
        #endregion

        #region Properties
        public string SourceVideoPath
        {
            get => _sourceVideoPath;
            set
            {
                if (SetProperty(ref _sourceVideoPath, value))
                {
                    HasSourceVideo = !string.IsNullOrEmpty(value);
                    OnPropertyChanged(nameof(CanApplyEffect));
                    if (HasSourceVideo)
                    {
                        LoadVideoForEffects();
                    }
                }
            }
        }

        public bool HasSourceVideo
        {
            get => _hasSourceVideo;
            set => SetProperty(ref _hasSourceVideo, value);
        }

        public string SelectedEffect
        {
            get => _selectedEffect;
            set
            {
                if (SetProperty(ref _selectedEffect, value))
                {
                    OnPropertyChanged(nameof(IsSpeedChangeSelected));
                    OnPropertyChanged(nameof(IsFrameInterpolationSelected));
                    OnPropertyChanged(nameof(IsMotionBlurSelected));
                    OnPropertyChanged(nameof(IsStabilizationSelected));
                    OnPropertyChanged(nameof(IsColorGradingSelected));
                    OnPropertyChanged(nameof(IsTransitionSelected));
                }
            }
        }

        public double SpeedMultiplier
        {
            get => _speedMultiplier;
            set
            {
                if (SetProperty(ref _speedMultiplier, value))
                {
                    UpdateSpeedPreset();
                }
            }
        }

        public string SpeedPreset
        {
            get => _speedPreset;
            set
            {
                if (SetProperty(ref _speedPreset, value))
                {
                    ApplySpeedPreset();
                }
            }
        }

        public int TargetFrameRate
        {
            get => _targetFrameRate;
            set => SetProperty(ref _targetFrameRate, value);
        }

        public int InterpolationQuality
        {
            get => _interpolationQuality;
            set => SetProperty(ref _interpolationQuality, value);
        }

        public string InterpolationMethod
        {
            get => _interpolationMethod;
            set => SetProperty(ref _interpolationMethod, value);
        }

        public double BlurIntensity
        {
            get => _blurIntensity;
            set => SetProperty(ref _blurIntensity, value);
        }

        public bool UseAdaptiveBlur
        {
            get => _useAdaptiveBlur;
            set => SetProperty(ref _useAdaptiveBlur, value);
        }

        public double StabilizationStrength
        {
            get => _stabilizationStrength;
            set => SetProperty(ref _stabilizationStrength, value);
        }

        public bool CropToFit
        {
            get => _cropToFit;
            set => SetProperty(ref _cropToFit, value);
        }

        public string TransitionType
        {
            get => _transitionType;
            set => SetProperty(ref _transitionType, value);
        }

        public double TransitionDuration
        {
            get => _transitionDuration;
            set => SetProperty(ref _transitionDuration, value);
        }

        public string ColorGrading
        {
            get => _colorGrading;
            set => SetProperty(ref _colorGrading, value);
        }

        public double Saturation
        {
            get => _saturation;
            set => SetProperty(ref _saturation, value);
        }

        public double Brightness
        {
            get => _brightness;
            set => SetProperty(ref _brightness, value);
        }

        public double Contrast
        {
            get => _contrast;
            set => SetProperty(ref _contrast, value);
        }

        public double Exposure
        {
            get => _exposure;
            set => SetProperty(ref _exposure, value);
        }

        public bool EnableTemporalFiltering
        {
            get => _enableTemporalFiltering;
            set => SetProperty(ref _enableTemporalFiltering, value);
        }

        public double TemporalFilterStrength
        {
            get => _temporalFilterStrength;
            set => SetProperty(ref _temporalFilterStrength, value);
        }

        public bool PreserveOriginalDuration
        {
            get => _preserveOriginalDuration;
            set => SetProperty(ref _preserveOriginalDuration, value);
        }

        public string ProcessingPriority
        {
            get => _processingPriority;
            set => SetProperty(ref _processingPriority, value);
        }

        public bool HasPreview
        {
            get => _hasPreview;
            set
            {
                if (SetProperty(ref _hasPreview, value))
                {
                    OnPropertyChanged(nameof(CanExport));
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
                    OnPropertyChanged(nameof(CanApplyEffect));
                    OnPropertyChanged(nameof(CanCancel));
                    OnPropertyChanged(nameof(CanExport));
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

        public double TimelinePosition
        {
            get => _timelinePosition;
            set => SetProperty(ref _timelinePosition, value);
        }

        public double TimelineDuration
        {
            get => _timelineDuration;
            set => SetProperty(ref _timelineDuration, value);
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
        public bool CanApplyEffect => !IsProcessing && HasSourceVideo;
        public bool CanCancel => IsProcessing;
        public bool CanExport => !IsProcessing && HasPreview;
        public bool IsSpeedChangeSelected => SelectedEffect == "Speed Change";
        public bool IsFrameInterpolationSelected => SelectedEffect == "Frame Interpolation";
        public bool IsMotionBlurSelected => SelectedEffect == "Motion Blur";
        public bool IsStabilizationSelected => SelectedEffect == "Stabilization";
        public bool IsColorGradingSelected => SelectedEffect == "Color Grading";
        public bool IsTransitionSelected => SelectedEffect == "Transition";
        public string TimelinePositionText => $"{TimelinePosition:F1}s";
        public string SpeedText => $"{SpeedMultiplier:F1}x";
        public string FrameRateText => $"{TargetFrameRate}fps";
        #endregion

        #region Collections
        public ObservableCollection<EffectTemplate> EffectTemplates { get; } = new();
        public ObservableCollection<TemporalEffectHistoryItem> EffectHistory { get; } = new();
        public ObservableCollection<VideoClip> TimelineClips { get; } = new();

        public List<string> AvailableEffects { get; } = new()
        {
            "Speed Change", "Frame Interpolation", "Motion Blur", 
            "Stabilization", "Color Grading", "Transition"
        };

        public List<string> SpeedPresets { get; } = new()
        {
            "Slow Motion (0.5x)", "Normal (1x)", "Fast (2x)", "Time Lapse (4x)", "Hyper Speed (8x)"
        };

        public List<int> FrameRateOptions { get; } = new() { 24, 30, 60, 120, 240 };
        
        public List<string> InterpolationMethods { get; } = new()
        {
            "Optical Flow", "Frame Blending", "AI Interpolation", "Motion Estimation"
        };

        public List<string> TransitionTypes { get; } = new()
        {
            "Fade", "Dissolve", "Wipe", "Slide", "Zoom", "Blur", "Custom"
        };

        public List<string> ColorGradingPresets { get; } = new()
        {
            "None", "Cinematic", "Warm", "Cool", "Vintage", "High Contrast", "Desaturated"
        };

        public List<string> ProcessingPriorities { get; } = new() { "Low", "Normal", "High", "Real-time" };
        public List<string> ExportFormats { get; } = new() { "MP4", "AVI", "MOV", "WebM", "MKV" };
        public List<string> ExportQualities { get; } = new() { "Low", "Medium", "High", "Ultra", "Lossless" };
        #endregion

        #region Commands
        public ICommand LoadVideoCommand { get; }
        public ICommand ApplyEffectCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand PreviewEffectCommand { get; }
        public ICommand ResetEffectCommand { get; }
        public ICommand ExportEffectCommand { get; }
        public ICommand ApplyTemplateCommand { get; }
        public ICommand SetSpeedPresetCommand { get; }
        public ICommand AddToTimelineCommand { get; }
        public ICommand RemoveFromTimelineCommand { get; }
        public ICommand SaveTemplateCommand { get; }
        public ICommand LoadTemplateCommand { get; }
        #endregion

        public TemporalEffectsViewModel()
        {
            // Initialize commands
            LoadVideoCommand = new SimpleRelayCommand(async () => await LoadVideoAsync());
            ApplyEffectCommand = new SimpleRelayCommand(async () => await ApplyEffectAsync(), () => CanApplyEffect);
            CancelCommand = new SimpleRelayCommand(CancelProcessing, () => CanCancel);
            PreviewEffectCommand = new SimpleRelayCommand(async () => await PreviewEffectAsync());
            ResetEffectCommand = new SimpleRelayCommand(ResetEffect);
            ExportEffectCommand = new SimpleRelayCommand(async () => await ExportEffectAsync(), () => CanExport);
            ApplyTemplateCommand = new SimpleRelayCommand<EffectTemplate>(ApplyTemplate);
            SetSpeedPresetCommand = new SimpleRelayCommand<string>(SetSpeedPreset);
            AddToTimelineCommand = new SimpleRelayCommand(AddToTimeline);
            RemoveFromTimelineCommand = new SimpleRelayCommand<VideoClip>(RemoveFromTimeline);
            SaveTemplateCommand = new SimpleRelayCommand(SaveTemplate);
            LoadTemplateCommand = new SimpleRelayCommand<EffectTemplate>(LoadTemplate);

            // Initialize sample data
            InitializeSampleData();
        }

        #region Command Implementations
        private async Task LoadVideoAsync()
        {
            var openDialog = new OpenFileDialog
            {
                Filter = "Video files|*.mp4;*.avi;*.mov;*.mkv;*.webm|All files|*.*",
                Title = "Select Video for Temporal Effects"
            };

            if (openDialog.ShowDialog() == true)
            {
                SourceVideoPath = openDialog.FileName;
                ProcessingStatus = $"Video loaded: {Path.GetFileName(openDialog.FileName)}";
            }
        }

        private async Task ApplyEffectAsync()
        {
            try
            {
                _cancellationTokenSource = new CancellationTokenSource();
                IsProcessing = true;
                ProcessingProgress = 0;
                ProcessingStatus = "Initializing temporal effect processing...";

                // Add to history
                var historyItem = new TemporalEffectHistoryItem
                {
                    Id = Guid.NewGuid(),
                    SourcePath = SourceVideoPath,
                    EffectType = SelectedEffect,
                    SpeedMultiplier = SpeedMultiplier,
                    TargetFrameRate = TargetFrameRate,
                    BlurIntensity = BlurIntensity,
                    StabilizationStrength = StabilizationStrength,
                    TransitionType = TransitionType,
                    ColorGrading = ColorGrading,
                    Timestamp = DateTime.Now
                };

                EffectHistory.Insert(0, historyItem);

                var stages = new[]
                {
                    ("Analyzing video structure...", 10),
                    ("Extracting temporal data...", 25),
                    ("Applying temporal effects...", 60),
                    ("Processing frame interpolation...", 80),
                    ("Encoding result...", 95),
                    ("Finalizing...", 100)
                };

                foreach (var (status, progress) in stages)
                {
                    if (_cancellationTokenSource.Token.IsCancellationRequested)
                        break;

                    ProcessingStatus = status;
                    ProcessingProgress = progress;
                    await Task.Delay(250, _cancellationTokenSource.Token);
                }

                if (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    ProcessingStatus = "Temporal effects applied successfully!";
                    HasPreview = true;
                    
                    // Update history item
                    historyItem.ProcessingTime = TimeSpan.FromSeconds(30);
                    historyItem.OutputPath = $"temporal_effect_{DateTime.Now:yyyyMMdd_HHmmss}.mp4";
                }
            }
            catch (OperationCanceledException)
            {
                ProcessingStatus = "Effect processing cancelled";
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

        private async Task PreviewEffectAsync()
        {
            try
            {
                IsProcessing = true;
                ProcessingStatus = "Generating effect preview...";
                ProcessingProgress = 0;

                for (int i = 0; i <= 100; i += 20)
                {
                    ProcessingProgress = i;
                    await Task.Delay(200);
                }

                HasPreview = true;
                ProcessingStatus = "Preview ready!";
            }
            catch (Exception ex)
            {
                ProcessingStatus = $"Preview error: {ex.Message}";
            }
            finally
            {
                IsProcessing = false;
                ProcessingProgress = 0;
            }
        }

        private void ResetEffect()
        {
            SpeedMultiplier = 1.0;
            TargetFrameRate = 60;
            BlurIntensity = 0.5;
            StabilizationStrength = 0.8;
            TransitionDuration = 1.0;
            Saturation = 1.0;
            Brightness = 0.0;
            Contrast = 1.0;
            Exposure = 0.0;
            HasPreview = false;
            ProcessingStatus = "Effects reset to defaults";
        }

        private async Task ExportEffectAsync()
        {
            try
            {
                IsProcessing = true;
                ProcessingStatus = "Exporting processed video...";
                ProcessingProgress = 0;

                for (int i = 0; i <= 100; i += 5)
                {
                    ProcessingProgress = i;
                    await Task.Delay(100);
                }

                ProcessingStatus = "Export completed!";
            }
            catch (Exception ex)
            {
                ProcessingStatus = $"Export error: {ex.Message}";
            }
            finally
            {
                IsProcessing = false;
                ProcessingProgress = 0;
            }
        }

        private void ApplyTemplate(EffectTemplate? template)
        {
            if (template == null) return;

            switch (template.Name)
            {
                case "Slow Motion":
                    SelectedEffect = "Speed Change";
                    SpeedMultiplier = 0.5;
                    SpeedPreset = "Slow Motion (0.5x)";
                    break;
                case "Time Lapse":
                    SelectedEffect = "Speed Change";
                    SpeedMultiplier = 4.0;
                    SpeedPreset = "Time Lapse (4x)";
                    break;
                case "Smooth 60fps":
                    SelectedEffect = "Frame Interpolation";
                    TargetFrameRate = 60;
                    InterpolationQuality = 8;
                    break;
                case "Film Stabilization":
                    SelectedEffect = "Stabilization";
                    StabilizationStrength = 0.9;
                    CropToFit = true;
                    break;
                case "Motion Blur":
                    SelectedEffect = "Motion Blur";
                    BlurIntensity = 0.7;
                    UseAdaptiveBlur = true;
                    break;
                case "Cinematic Grade":
                    SelectedEffect = "Color Grading";
                    ColorGrading = "Cinematic";
                    Contrast = 1.2;
                    Saturation = 0.9;
                    break;
            }

            ProcessingStatus = $"Applied template: {template.Name}";
        }

        private void SetSpeedPreset(string? preset)
        {
            if (string.IsNullOrEmpty(preset)) return;

            SpeedPreset = preset;
        }

        private void AddToTimeline()
        {
            if (!HasSourceVideo) return;

            var clip = new VideoClip
            {
                Name = Path.GetFileName(SourceVideoPath),
                SourcePath = SourceVideoPath,
                StartTime = TimelineClips.Count * 5.0, // 5 second intervals
                Duration = 5.0,
                EffectType = SelectedEffect,
                Parameters = GetCurrentEffectParameters()
            };

            TimelineClips.Add(clip);
            ProcessingStatus = "Clip added to timeline";
        }

        private void RemoveFromTimeline(VideoClip? clip)
        {
            if (clip != null && TimelineClips.Contains(clip))
            {
                TimelineClips.Remove(clip);
                ProcessingStatus = "Clip removed from timeline";
            }
        }

        private void SaveTemplate()
        {
            var template = new EffectTemplate
            {
                Name = $"Custom Effect {DateTime.Now:HHmm}",
                Description = "User-defined effect template",
                Icon = "⚙️",
                EffectType = SelectedEffect,
                Parameters = GetCurrentEffectParameters()
            };

            EffectTemplates.Add(template);
            ProcessingStatus = "Effect template saved";
        }

        private void LoadTemplate(EffectTemplate? template)
        {
            if (template == null) return;
            ApplyTemplate(template);
        }

        private void LoadVideoForEffects()
        {
            // TODO: Load video metadata for effects analysis
            TimelineDuration = 20.0; // 20 seconds simulation
            ProcessingStatus = "Video loaded for temporal effects";
        }

        private void UpdateSpeedPreset()
        {
            SpeedPreset = SpeedMultiplier switch
            {
                <= 0.6 => "Slow Motion (0.5x)",
                <= 1.1 => "Normal (1x)",
                <= 2.5 => "Fast (2x)",
                <= 5.0 => "Time Lapse (4x)",
                _ => "Hyper Speed (8x)"
            };
        }

        private void ApplySpeedPreset()
        {
            SpeedMultiplier = SpeedPreset switch
            {
                "Slow Motion (0.5x)" => 0.5,
                "Normal (1x)" => 1.0,
                "Fast (2x)" => 2.0,
                "Time Lapse (4x)" => 4.0,
                "Hyper Speed (8x)" => 8.0,
                _ => 1.0
            };
        }

        private string GetCurrentEffectParameters()
        {
            return SelectedEffect switch
            {
                "Speed Change" => $"Speed: {SpeedMultiplier:F1}x",
                "Frame Interpolation" => $"Target: {TargetFrameRate}fps, Quality: {InterpolationQuality}",
                "Motion Blur" => $"Intensity: {BlurIntensity:F1}, Adaptive: {UseAdaptiveBlur}",
                "Stabilization" => $"Strength: {StabilizationStrength:F1}, Crop: {CropToFit}",
                "Color Grading" => $"Preset: {ColorGrading}, Sat: {Saturation:F1}",
                "Transition" => $"Type: {TransitionType}, Duration: {TransitionDuration:F1}s",
                _ => "Default"
            };
        }

        private void InitializeSampleData()
        {
            // Add sample effect templates
            EffectTemplates.Add(new EffectTemplate
            {
                Name = "Slow Motion",
                Description = "Classic 50% slow motion effect",
                Icon = "🐌",
                EffectType = "Speed Change",
                Parameters = "Speed: 0.5x"
            });

            EffectTemplates.Add(new EffectTemplate
            {
                Name = "Time Lapse",
                Description = "4x speed time lapse effect",
                Icon = "⚡",
                EffectType = "Speed Change",
                Parameters = "Speed: 4.0x"
            });

            EffectTemplates.Add(new EffectTemplate
            {
                Name = "Smooth 60fps",
                Description = "AI frame interpolation to 60fps",
                Icon = "📽️",
                EffectType = "Frame Interpolation",
                Parameters = "Target: 60fps, Quality: 8"
            });

            EffectTemplates.Add(new EffectTemplate
            {
                Name = "Film Stabilization",
                Description = "Professional stabilization preset",
                Icon = "🎯",
                EffectType = "Stabilization",
                Parameters = "Strength: 0.9, Crop: True"
            });

            EffectTemplates.Add(new EffectTemplate
            {
                Name = "Motion Blur",
                Description = "Realistic motion blur effect",
                Icon = "💫",
                EffectType = "Motion Blur",
                Parameters = "Intensity: 0.7, Adaptive: True"
            });

            EffectTemplates.Add(new EffectTemplate
            {
                Name = "Cinematic Grade",
                Description = "Professional color grading",
                Icon = "🎨",
                EffectType = "Color Grading",
                Parameters = "Preset: Cinematic"
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
    public class EffectTemplate
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Icon { get; set; } = "⚙️";
        public string EffectType { get; set; } = "";
        public string Parameters { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsBuiltIn { get; set; } = true;
    }

    public class TemporalEffectHistoryItem
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string SourcePath { get; set; } = "";
        public string EffectType { get; set; } = "";
        public double SpeedMultiplier { get; set; }
        public int TargetFrameRate { get; set; }
        public double BlurIntensity { get; set; }
        public double StabilizationStrength { get; set; }
        public string TransitionType { get; set; } = "";
        public string ColorGrading { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public string OutputPath { get; set; } = "";
        public TimeSpan ProcessingTime { get; set; }
        
        // Display Properties
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

        public string SourceFileName => Path.GetFileName(SourcePath);
        public string EffectSummary => EffectType switch
        {
            "Speed Change" => $"Speed: {SpeedMultiplier:F1}x",
            "Frame Interpolation" => $"Target: {TargetFrameRate}fps",
            "Motion Blur" => $"Blur: {BlurIntensity:F1}",
            "Stabilization" => $"Stabilize: {StabilizationStrength:F1}",
            _ => EffectType
        };
    }

    public class VideoClip
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = "";
        public string SourcePath { get; set; } = "";
        public double StartTime { get; set; }
        public double Duration { get; set; }
        public string EffectType { get; set; } = "";
        public string Parameters { get; set; } = "";
        public bool IsSelected { get; set; }
        
        public double EndTime => StartTime + Duration;
        public string TimeRange => $"{StartTime:F1}s - {EndTime:F1}s";
    }
    #endregion
}
