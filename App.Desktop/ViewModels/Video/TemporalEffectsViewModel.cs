using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Lazarus.Desktop.Helpers;

namespace Lazarus.Desktop.ViewModels.Video
{
    public class TemporalEffectsViewModel : INotifyPropertyChanged
    {
        private string _selectedEffectType = "Time Stretch";
        private double _speedMultiplier = 1.0;
        private int _targetFrameRate = 240;
        private int _interpolationQuality = 5;
        private double _blurIntensity = 0.5;
        private bool _useAdaptiveBlur = true;
        private double _stabilizationStrength = 0.8;
        private bool _cropToFit = true;
        private bool _preserveOriginalDuration = false;
        private bool _highQualityMode = true;
        private string _processingPriority = "Normal";
        private bool _hasPreview = false;
        private bool _isProcessing = false;
        private string _processingStatus = "";
        private double _processingProgress = 0;

        public TemporalEffectsViewModel()
        {
            ApplyEffectCommand = new SimpleRelayCommand(ApplyEffect, CanApplyEffect);
            PreviewEffectCommand = new SimpleRelayCommand(PreviewEffect);
            ResetEffectCommand = new SimpleRelayCommand(ResetEffect);
            ExportEffectCommand = new SimpleRelayCommand(ExportEffect, CanExportEffect);
            ApplyTemplateCommand = new SimpleRelayCommand<EffectTemplate>(ApplyTemplate);
            PreviewTemplateCommand = new SimpleRelayCommand<EffectTemplate>(PreviewTemplate);

            EffectTemplates = new ObservableCollection<EffectTemplate>
            {
                new() { Name = "Slow Motion", Description = "Classic 50% slow motion effect", Icon = "🐌" },
                new() { Name = "Speed Ramp", Description = "Fast-slow-fast speed variation", Icon = "⚡" },
                new() { Name = "Smooth 60fps", Description = "AI frame interpolation to 60fps", Icon = "📽️" },
                new() { Name = "Film Stabilization", Description = "Professional stabilization preset", Icon = "🎯" },
                new() { Name = "Motion Blur", Description = "Realistic motion blur effect", Icon = "💫" },
                new() { Name = "Time Freeze", Description = "Freeze frame with smooth transition", Icon = "⏸️" }
            };
        }

        public string SelectedEffectType
        {
            get => _selectedEffectType;
            set
            {
                if (SetProperty(ref _selectedEffectType, value))
                {
                    OnPropertyChanged(nameof(IsTimeStretchSelected));
                    OnPropertyChanged(nameof(IsFrameInterpolationSelected));
                    OnPropertyChanged(nameof(IsMotionBlurSelected));
                    OnPropertyChanged(nameof(IsStabilizationSelected));
                }
            }
        }

        public bool IsTimeStretchSelected => SelectedEffectType == "Time Stretch";
        public bool IsFrameInterpolationSelected => SelectedEffectType == "Frame Interpolation";
        public bool IsMotionBlurSelected => SelectedEffectType == "Motion Blur";
        public bool IsStabilizationSelected => SelectedEffectType == "Stabilization";

        public double SpeedMultiplier
        {
            get => _speedMultiplier;
            set => SetProperty(ref _speedMultiplier, value);
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

        public bool PreserveOriginalDuration
        {
            get => _preserveOriginalDuration;
            set => SetProperty(ref _preserveOriginalDuration, value);
        }

        public bool HighQualityMode
        {
            get => _highQualityMode;
            set => SetProperty(ref _highQualityMode, value);
        }

        public string ProcessingPriority
        {
            get => _processingPriority;
            set => SetProperty(ref _processingPriority, value);
        }

        public bool HasPreview
        {
            get => _hasPreview;
            set => SetProperty(ref _hasPreview, value);
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

        public double ProcessingProgress
        {
            get => _processingProgress;
            set => SetProperty(ref _processingProgress, value);
        }

        public ObservableCollection<EffectTemplate> EffectTemplates { get; }

        public ICommand ApplyEffectCommand { get; }
        public ICommand PreviewEffectCommand { get; }
        public ICommand ResetEffectCommand { get; }
        public ICommand ExportEffectCommand { get; }
        public ICommand ApplyTemplateCommand { get; }
        public ICommand PreviewTemplateCommand { get; }

        private bool CanApplyEffect()
        {
            return !IsProcessing;
        }

        private bool CanExportEffect()
        {
            return !IsProcessing && HasPreview;
        }

        private async void ApplyEffect()
        {
            try
            {
                IsProcessing = true;
                ProcessingProgress = 0;
                ProcessingStatus = "Initializing effect processing...";
                await Task.Delay(500);

                // Simulate processing with progress updates
                for (int i = 0; i <= 100; i += 10)
                {
                    ProcessingProgress = i;
                    ProcessingStatus = $"Applying {SelectedEffectType}... {i}%";
                    await Task.Delay(300);
                }

                HasPreview = true;
                ProcessingStatus = "Effect applied successfully!";
                await Task.Delay(1000);
                ProcessingStatus = "";
            }
            catch (Exception ex)
            {
                ProcessingStatus = $"Error: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Effect processing error: {ex.Message}");
            }
            finally
            {
                IsProcessing = false;
                ProcessingProgress = 0;
            }
        }

        private async void PreviewEffect()
        {
            try
            {
                IsProcessing = true;
                ProcessingStatus = "Generating preview...";
                await Task.Delay(1500);

                HasPreview = true;
                ProcessingStatus = "Preview ready!";
                await Task.Delay(1000);
                ProcessingStatus = "";
            }
            catch (Exception ex)
            {
                ProcessingStatus = $"Preview error: {ex.Message}";
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private void ResetEffect()
        {
            HasPreview = false;
            SpeedMultiplier = 1.0;
            BlurIntensity = 0.5;
            StabilizationStrength = 0.8;
            InterpolationQuality = 5;
            System.Diagnostics.Debug.WriteLine("Effect reset");
        }

        private async void ExportEffect()
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
                await Task.Delay(1000);
                ProcessingStatus = "";
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
                    SelectedEffectType = "Time Stretch";
                    SpeedMultiplier = 0.5;
                    break;
                case "Speed Ramp":
                    SelectedEffectType = "Time Stretch";
                    SpeedMultiplier = 2.0;
                    break;
                case "Smooth 60fps":
                    SelectedEffectType = "Frame Interpolation";
                    TargetFrameRate = 60;
                    break;
                case "Film Stabilization":
                    SelectedEffectType = "Stabilization";
                    StabilizationStrength = 0.9;
                    CropToFit = true;
                    break;
                case "Motion Blur":
                    SelectedEffectType = "Motion Blur";
                    BlurIntensity = 0.7;
                    UseAdaptiveBlur = true;
                    break;
            }

            System.Diagnostics.Debug.WriteLine($"Applied template: {template.Name}");
        }

        private void PreviewTemplate(EffectTemplate? template)
        {
            if (template == null) return;
            ApplyTemplate(template);
            PreviewEffect();
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

    public class EffectTemplate
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Icon { get; set; } = "";
    }
}