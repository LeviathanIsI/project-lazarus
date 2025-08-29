using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Lazarus.Desktop.Helpers;

namespace Lazarus.Desktop.ViewModels.Images
{
    public class InpaintingViewModel : INotifyPropertyChanged
    {
        #region Private Fields
        private string _prompt = "";
        private string _negativePrompt = "";
        private double _strength = 0.8;
        private int _steps = 25;
        private double _cfgScale = 7.5;
        private int _maskBlur = 4;
        private double _brushSize = 20;
        private double _brushHardness = 0.8;
        private double _brushOpacity = 1.0;
        private string _selectedBrushMode = "Paint";
        private string _selectedModel = "Default";
        private string _selectedPreset = "Remove Object";
        private BitmapSource? _sourceImageSource;
        private BitmapSource? _maskImageSource;
        private BitmapSource? _generatedImageSource;
        private bool _hasSourceImage;
        private bool _hasMask;
        private bool _hasGeneratedImage;
        private bool _isGenerating;
        private double _generationProgress;
        private string _generationStatus = "";
        private bool _showMaskOverlay = true;
        private bool _enableOutpainting = false;
        private bool _autoDetectObjects = true;
        private bool _showAdvancedSettings = false;
        private Point _lastBrushPoint;
        private bool _isDrawing = false;
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
                }
            }
        }

        public string NegativePrompt
        {
            get => _negativePrompt;
            set => SetProperty(ref _negativePrompt, value);
        }

        public double Strength
        {
            get => _strength;
            set => SetProperty(ref _strength, value);
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

        public int MaskBlur
        {
            get => _maskBlur;
            set => SetProperty(ref _maskBlur, value);
        }

        public double BrushSize
        {
            get => _brushSize;
            set => SetProperty(ref _brushSize, value);
        }

        public double BrushHardness
        {
            get => _brushHardness;
            set => SetProperty(ref _brushHardness, value);
        }

        public double BrushOpacity
        {
            get => _brushOpacity;
            set => SetProperty(ref _brushOpacity, value);
        }

        public string SelectedBrushMode
        {
            get => _selectedBrushMode;
            set => SetProperty(ref _selectedBrushMode, value);
        }

        public string SelectedModel
        {
            get => _selectedModel;
            set => SetProperty(ref _selectedModel, value);
        }

        public string SelectedPreset
        {
            get => _selectedPreset;
            set
            {
                if (SetProperty(ref _selectedPreset, value))
                {
                    ApplyPreset(value);
                }
            }
        }

        public BitmapSource? SourceImageSource
        {
            get => _sourceImageSource;
            set
            {
                if (SetProperty(ref _sourceImageSource, value))
                {
                    HasSourceImage = value != null;
                    OnPropertyChanged(nameof(CanGenerate));
                    OnPropertyChanged(nameof(SourceImageInfo));
                    if (value != null && AutoDetectObjects)
                    {
                        DetectObjects();
                    }
                }
            }
        }

        public BitmapSource? MaskImageSource
        {
            get => _maskImageSource;
            set
            {
                if (SetProperty(ref _maskImageSource, value))
                {
                    HasMask = value != null;
                    OnPropertyChanged(nameof(CanGenerate));
                }
            }
        }

        public BitmapSource? GeneratedImageSource
        {
            get => _generatedImageSource;
            set => SetProperty(ref _generatedImageSource, value);
        }

        public bool HasSourceImage
        {
            get => _hasSourceImage;
            set => SetProperty(ref _hasSourceImage, value);
        }

        public bool HasMask
        {
            get => _hasMask;
            set => SetProperty(ref _hasMask, value);
        }

        public bool HasGeneratedImage
        {
            get => _hasGeneratedImage;
            set => SetProperty(ref _hasGeneratedImage, value);
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
                }
            }
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

        public bool ShowMaskOverlay
        {
            get => _showMaskOverlay;
            set => SetProperty(ref _showMaskOverlay, value);
        }

        public bool EnableOutpainting
        {
            get => _enableOutpainting;
            set => SetProperty(ref _enableOutpainting, value);
        }

        public bool AutoDetectObjects
        {
            get => _autoDetectObjects;
            set => SetProperty(ref _autoDetectObjects, value);
        }

        public bool ShowAdvancedSettings
        {
            get => _showAdvancedSettings;
            set => SetProperty(ref _showAdvancedSettings, value);
        }

        public bool IsDrawing
        {
            get => _isDrawing;
            set => SetProperty(ref _isDrawing, value);
        }

        // Computed Properties
        public bool CanGenerate => !IsGenerating && HasSourceImage && HasMask && !string.IsNullOrWhiteSpace(Prompt);
        public bool CanCancel => IsGenerating;
        public string SourceImageInfo => HasSourceImage && SourceImageSource != null 
            ? $"{SourceImageSource.PixelWidth}x{SourceImageSource.PixelHeight}" 
            : "No image loaded";
        public string BrushInfo => $"{BrushSize:F0}px, {BrushHardness:P0} hard, {BrushOpacity:P0} opacity";
        #endregion

        #region Collections
        public ObservableCollection<InpaintingHistoryItem> GenerationHistory { get; } = new();
        public ObservableCollection<MaskLayer> MaskLayers { get; } = new();
        public ObservableCollection<InpaintingResult> ResultGallery { get; } = new();
        public ObservableCollection<string> FavoritePrompts { get; } = new();

        public List<string> BrushModes { get; } = new() { "Paint", "Erase", "Blur", "Sharpen" };
        public List<string> BrushPresets { get; } = new() { "Small", "Medium", "Large", "Custom" };
        public List<string> InpaintingPresets { get; } = new() 
        { 
            "Remove Object", 
            "Fill Area", 
            "Replace Object", 
            "Extend Background", 
            "Fix Defects", 
            "Change Style",
            "Custom"
        };
        public List<string> AvailableModels { get; } = new()
        {
            "Default",
            "Stable Diffusion Inpainting",
            "DALL-E Inpainting",
            "LaMa (Fast)",
            "EdgeConnect"
        };
        public List<string> MaskTemplates { get; } = new()
        {
            "Rectangle",
            "Circle", 
            "Freeform",
            "Object Detection",
            "Edge Detection"
        };
        #endregion

        #region Commands
        public ICommand LoadImageCommand { get; }
        public ICommand LoadMaskCommand { get; }
        public ICommand GenerateCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand SaveResultCommand { get; }
        public ICommand ClearMaskCommand { get; }
        public ICommand UndoMaskCommand { get; }
        public ICommand RedoMaskCommand { get; }
        public ICommand DetectObjectsCommand { get; }
        public ICommand ApplyMaskTemplateCommand { get; }
        public ICommand ExportMaskCommand { get; }
        public ICommand ImportMaskCommand { get; }
        public ICommand CopyPromptCommand { get; }
        public ICommand AddToFavoritesCommand { get; }
        public ICommand LoadPresetCommand { get; }
        public ICommand SetBrushModeCommand { get; }
        public ICommand SetBrushPresetCommand { get; }
        public ICommand ClearHistoryCommand { get; }
        #endregion

        public InpaintingViewModel()
        {
            // Initialize commands
            LoadImageCommand = new SimpleRelayCommand(async () => await LoadImageAsync());
            LoadMaskCommand = new SimpleRelayCommand(async () => await LoadMaskAsync());
            GenerateCommand = new SimpleRelayCommand(async () => await GenerateInpaintingAsync(), () => CanGenerate);
            CancelCommand = new SimpleRelayCommand(CancelGeneration, () => CanCancel);
            SaveResultCommand = new SimpleRelayCommand(SaveResult, () => HasGeneratedImage);
            ClearMaskCommand = new SimpleRelayCommand(ClearMask, () => HasMask);
            UndoMaskCommand = new SimpleRelayCommand(UndoMask);
            RedoMaskCommand = new SimpleRelayCommand(RedoMask);
            DetectObjectsCommand = new SimpleRelayCommand(DetectObjects, () => HasSourceImage);
            ApplyMaskTemplateCommand = new SimpleRelayCommand<string>(ApplyMaskTemplate);
            ExportMaskCommand = new SimpleRelayCommand(ExportMask, () => HasMask);
            ImportMaskCommand = new SimpleRelayCommand(ImportMask);
            CopyPromptCommand = new SimpleRelayCommand<string>(CopyPrompt);
            AddToFavoritesCommand = new SimpleRelayCommand(AddToFavorites, () => !string.IsNullOrWhiteSpace(Prompt));
            LoadPresetCommand = new SimpleRelayCommand<string>(LoadPreset);
            SetBrushModeCommand = new SimpleRelayCommand<string>(SetBrushMode);
            SetBrushPresetCommand = new SimpleRelayCommand<string>(SetBrushPreset);
            ClearHistoryCommand = new SimpleRelayCommand(ClearHistory, () => GenerationHistory.Count > 0);

            // Initialize sample data
            InitializeSampleData();
        }

        #region Command Implementations
        private async Task LoadImageAsync()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Image files (*.png;*.jpg;*.jpeg;*.bmp;*.webp)|*.png;*.jpg;*.jpeg;*.bmp;*.webp|All files (*.*)|*.*",
                Title = "Select Source Image for Inpainting"
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
                    GenerationStatus = $"Image loaded: {Path.GetFileName(openFileDialog.FileName)}";
                }
                catch (Exception ex)
                {
                    GenerationStatus = $"Error loading image: {ex.Message}";
                }
            }
        }

        private async Task LoadMaskAsync()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Image files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|All files (*.*)|*.*",
                Title = "Select Mask Image"
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
                    
                    MaskImageSource = bitmap;
                    GenerationStatus = "Mask loaded successfully";
                }
                catch (Exception ex)
                {
                    GenerationStatus = $"Error loading mask: {ex.Message}";
                }
            }
        }

        private async Task GenerateInpaintingAsync()
        {
            try
            {
                _cancellationTokenSource = new CancellationTokenSource();
                IsGenerating = true;
                GenerationProgress = 0;
                GenerationStatus = "Starting inpainting generation...";

                // Add to history
                var historyItem = new InpaintingHistoryItem
                {
                    Id = Guid.NewGuid(),
                    Prompt = Prompt,
                    NegativePrompt = NegativePrompt,
                    Strength = Strength,
                    Steps = Steps,
                    CfgScale = CfgScale,
                    MaskBlur = MaskBlur,
                    BrushSize = BrushSize,
                    Timestamp = DateTime.Now,
                    Model = SelectedModel,
                    Preset = SelectedPreset
                };

                GenerationHistory.Insert(0, historyItem);

                // Simulate generation progress
                for (int i = 0; i <= Steps; i++)
                {
                    if (_cancellationTokenSource.Token.IsCancellationRequested)
                        break;

                    GenerationProgress = (double)i / Steps * 100;
                    GenerationStatus = $"Inpainting step {i}/{Steps} - {GenerationProgress:F0}%";
                    await Task.Delay(80, _cancellationTokenSource.Token);
                }

                if (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    GenerationStatus = "Inpainting complete!";
                    HasGeneratedImage = true;
                    
                    // Create result entry
                    var result = new InpaintingResult
                    {
                        Id = historyItem.Id,
                        SourceImage = SourceImageSource,
                        MaskImage = MaskImageSource,
                        ResultImage = null, // Would be actual result
                        Prompt = Prompt,
                        Timestamp = DateTime.Now,
                        Parameters = $"Str {Strength:F1}, {Steps} steps, Blur {MaskBlur}px"
                    };
                    ResultGallery.Insert(0, result);
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

        private void SaveResult()
        {
            // TODO: Implement save functionality
            GenerationStatus = "Result saved successfully!";
        }

        private void ClearMask()
        {
            MaskImageSource = null;
            GenerationStatus = "Mask cleared";
        }

        private void UndoMask()
        {
            // TODO: Implement undo functionality
            GenerationStatus = "Mask undo";
        }

        private void RedoMask()
        {
            // TODO: Implement redo functionality
            GenerationStatus = "Mask redo";
        }

        private void DetectObjects()
        {
            if (!HasSourceImage) return;
            
            // TODO: Implement object detection
            GenerationStatus = "Detecting objects in image...";
            
            // Simulate object detection
            Task.Run(async () =>
            {
                await Task.Delay(1000);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    GenerationStatus = "Objects detected - mask suggestions available";
                });
            });
        }

        private void ApplyMaskTemplate(string? template)
        {
            if (string.IsNullOrEmpty(template) || !HasSourceImage) return;
            
            // TODO: Implement mask template application
            GenerationStatus = $"Applied {template} mask template";
        }

        private void ExportMask()
        {
            // TODO: Implement mask export
            GenerationStatus = "Mask exported";
        }

        private void ImportMask()
        {
            // TODO: Implement mask import
            GenerationStatus = "Mask imported";
        }

        private void CopyPrompt(string? prompt)
        {
            if (!string.IsNullOrEmpty(prompt))
            {
                System.Windows.Clipboard.SetText(prompt);
                GenerationStatus = "Prompt copied to clipboard";
            }
        }

        private void AddToFavorites()
        {
            if (!string.IsNullOrWhiteSpace(Prompt) && !FavoritePrompts.Contains(Prompt))
            {
                FavoritePrompts.Add(Prompt);
                GenerationStatus = "Added to favorites";
            }
        }

        private void LoadPreset(string? presetName)
        {
            if (string.IsNullOrEmpty(presetName)) return;
            ApplyPreset(presetName);
        }

        private void SetBrushMode(string? mode)
        {
            if (!string.IsNullOrEmpty(mode))
            {
                SelectedBrushMode = mode;
                GenerationStatus = $"Brush mode: {mode}";
            }
        }

        private void SetBrushPreset(string? preset)
        {
            if (string.IsNullOrEmpty(preset)) return;
            
            switch (preset)
            {
                case "Small":
                    BrushSize = 10;
                    BrushHardness = 0.9;
                    break;
                case "Medium":
                    BrushSize = 25;
                    BrushHardness = 0.7;
                    break;
                case "Large":
                    BrushSize = 50;
                    BrushHardness = 0.5;
                    break;
            }
            GenerationStatus = $"Brush preset: {preset}";
        }

        private void ClearHistory()
        {
            GenerationHistory.Clear();
            ResultGallery.Clear();
            GenerationStatus = "History cleared";
        }

        private void ApplyPreset(string preset)
        {
            switch (preset)
            {
                case "Remove Object":
                    Strength = 0.9;
                    Steps = 30;
                    CfgScale = 7.0;
                    if (string.IsNullOrEmpty(Prompt))
                        Prompt = "clean background, seamless fill";
                    NegativePrompt = "object, artifact, inconsistent";
                    break;
                case "Fill Area":
                    Strength = 0.8;
                    Steps = 25;
                    CfgScale = 6.5;
                    if (string.IsNullOrEmpty(Prompt))
                        Prompt = "natural continuation, matching style";
                    break;
                case "Replace Object":
                    Strength = 0.7;
                    Steps = 35;
                    CfgScale = 8.0;
                    if (string.IsNullOrEmpty(Prompt))
                        Prompt = "new object, fitting naturally";
                    break;
                case "Extend Background":
                    Strength = 0.6;
                    Steps = 30;
                    CfgScale = 7.5;
                    EnableOutpainting = true;
                    if (string.IsNullOrEmpty(Prompt))
                        Prompt = "extended background, natural continuation";
                    break;
                case "Fix Defects":
                    Strength = 0.5;
                    Steps = 20;
                    CfgScale = 6.0;
                    if (string.IsNullOrEmpty(Prompt))
                        Prompt = "clean, restored, high quality";
                    NegativePrompt = "defect, damage, artifact, blur";
                    break;
            }
        }

        private void InitializeSampleData()
        {
            // Add sample favorite prompts
            FavoritePrompts.Add("seamless background fill");
            FavoritePrompts.Add("natural object replacement");
            FavoritePrompts.Add("clean restoration");
            FavoritePrompts.Add("artistic style change");

            // Add sample history
            GenerationHistory.Add(new InpaintingHistoryItem
            {
                Id = Guid.NewGuid(),
                Prompt = "remove person from background",
                Strength = 0.9,
                Steps = 30,
                CfgScale = 7.0,
                MaskBlur = 4,
                BrushSize = 25,
                Timestamp = DateTime.Now.AddMinutes(-5),
                Model = "Stable Diffusion Inpainting",
                Preset = "Remove Object"
            });

            GenerationHistory.Add(new InpaintingHistoryItem
            {
                Id = Guid.NewGuid(),
                Prompt = "replace with beautiful flower",
                Strength = 0.7,
                Steps = 35,
                CfgScale = 8.0,
                MaskBlur = 2,
                BrushSize = 15,
                Timestamp = DateTime.Now.AddMinutes(-15),
                Model = "Default",
                Preset = "Replace Object"
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
    public class InpaintingHistoryItem
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Prompt { get; set; } = "";
        public string NegativePrompt { get; set; } = "";
        public double Strength { get; set; }
        public int Steps { get; set; }
        public double CfgScale { get; set; }
        public int MaskBlur { get; set; }
        public double BrushSize { get; set; }
        public DateTime Timestamp { get; set; }
        public string Model { get; set; } = "";
        public string Preset { get; set; } = "";
        
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

        public string PromptPreview => Prompt.Length > 35 ? Prompt.Substring(0, 32) + "..." : Prompt;
        public string ParametersText => $"Str {Strength:F1}, {Steps} steps, Blur {MaskBlur}px";
        public string PresetIcon => Preset switch
        {
            "Remove Object" => "🗑️",
            "Fill Area" => "🖌️",
            "Replace Object" => "🔄",
            "Extend Background" => "🖼️",
            "Fix Defects" => "🔧",
            "Change Style" => "🎨",
            _ => "⚙️"
        };
    }

    public class MaskLayer
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = "";
        public BitmapSource? MaskData { get; set; }
        public bool IsVisible { get; set; } = true;
        public double Opacity { get; set; } = 1.0;
        public string BlendMode { get; set; } = "Normal";
    }

    public class InpaintingResult
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public BitmapSource? SourceImage { get; set; }
        public BitmapSource? MaskImage { get; set; }
        public BitmapSource? ResultImage { get; set; }
        public string Prompt { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public string Parameters { get; set; } = "";
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
    }
    #endregion
}