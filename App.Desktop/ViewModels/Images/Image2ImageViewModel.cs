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
    public class Image2ImageViewModel : INotifyPropertyChanged
    {
        #region Private Fields
        private string _prompt = "";
        private string _negativePrompt = "";
        private double _strength = 0.75;
        private int _steps = 25;
        private double _cfgScale = 7.5;
        private string _selectedSampler = "DPM++ 2M Karras";
        private int _seed = -1;
        private string _selectedPreset = "Enhance";
        private string _selectedModel = "Default";
        private BitmapSource? _sourceImageSource;
        private BitmapSource? _generatedImageSource;
        private bool _hasSourceImage;
        private bool _hasGeneratedImage;
        private bool _isGenerating;
        private double _generationProgress;
        private string _generationStatus = "";
        private bool _useInpainting = false;
        private bool _useOutpainting = false;
        private bool _showAdvancedSettings = false;
        private string _selectedEditMode = "Transform";
        private double _maskBlur = 4.0;
        private int _batchSize = 1;
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

        public string SelectedSampler
        {
            get => _selectedSampler;
            set => SetProperty(ref _selectedSampler, value);
        }

        public int Seed
        {
            get => _seed;
            set => SetProperty(ref _seed, value);
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

        public string SelectedModel
        {
            get => _selectedModel;
            set => SetProperty(ref _selectedModel, value);
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

        public bool UseInpainting
        {
            get => _useInpainting;
            set => SetProperty(ref _useInpainting, value);
        }

        public bool UseOutpainting
        {
            get => _useOutpainting;
            set => SetProperty(ref _useOutpainting, value);
        }

        public bool ShowAdvancedSettings
        {
            get => _showAdvancedSettings;
            set => SetProperty(ref _showAdvancedSettings, value);
        }

        public string SelectedEditMode
        {
            get => _selectedEditMode;
            set => SetProperty(ref _selectedEditMode, value);
        }

        public double MaskBlur
        {
            get => _maskBlur;
            set => SetProperty(ref _maskBlur, value);
        }

        public int BatchSize
        {
            get => _batchSize;
            set => SetProperty(ref _batchSize, value);
        }

        // Computed Properties
        public bool CanGenerate => !IsGenerating && HasSourceImage && !string.IsNullOrWhiteSpace(Prompt);
        public bool CanCancel => IsGenerating;
        public string SeedDisplay => Seed == -1 ? "Random" : Seed.ToString();
        public string SourceImageInfo => HasSourceImage && SourceImageSource != null 
            ? $"{SourceImageSource.PixelWidth}x{SourceImageSource.PixelHeight}" 
            : "No image loaded";
        public string BatchEstimate => $"~{BatchSize * Steps * 0.15:F1}s";
        #endregion

        #region Collections
        public ObservableCollection<Image2ImageHistoryItem> GenerationHistory { get; } = new();
        public ObservableCollection<GeneratedImageResult> ResultGallery { get; } = new();
        public ObservableCollection<string> FavoritePrompts { get; } = new();

        public List<string> AvailableSamplers { get; } = new()
        {
            "DPM++ 2M Karras",
            "Euler A",
            "Euler",
            "LMS",
            "Heun",
            "DPM2",
            "DPM2 a",
            "DPM++ 2S a",
            "DPM++ SDE",
            "DDIM",
            "PLMS"
        };

        public List<string> TransformationPresets { get; } = new()
        {
            "Enhance",
            "Stylize", 
            "Fix",
            "Artistic",
            "Realistic",
            "Anime",
            "Photography",
            "Painting",
            "Sketch",
            "Custom"
        };

        public List<string> EditModes { get; } = new()
        {
            "Transform",
            "Inpaint",
            "Outpaint",
            "Upscale"
        };

        public List<string> AvailableModels { get; } = new()
        {
            "Default",
            "Stable Diffusion 1.5",
            "Stable Diffusion XL",
            "ControlNet",
            "Real-ESRGAN"
        };
        #endregion

        #region Commands
        public ICommand LoadImageCommand { get; }
        public ICommand GenerateCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand SaveResultCommand { get; }
        public ICommand ClearImageCommand { get; }
        public ICommand RandomizeSeedCommand { get; }
        public ICommand CopyPromptCommand { get; }
        public ICommand AddToFavoritesCommand { get; }
        public ICommand LoadPresetCommand { get; }
        public ICommand ExportResultCommand { get; }
        public ICommand SwapImagesCommand { get; }
        public ICommand CropImageCommand { get; }
        public ICommand RotateImageCommand { get; }
        public ICommand ResizeImageCommand { get; }
        public ICommand ClearHistoryCommand { get; }
        #endregion

        public Image2ImageViewModel()
        {
            // Initialize commands
            LoadImageCommand = new SimpleRelayCommand(async () => await LoadImageAsync());
            GenerateCommand = new SimpleRelayCommand(async () => await GenerateImageAsync(), () => CanGenerate);
            CancelCommand = new SimpleRelayCommand(CancelGeneration, () => CanCancel);
            SaveResultCommand = new SimpleRelayCommand(SaveResult, () => HasGeneratedImage);
            ClearImageCommand = new SimpleRelayCommand(ClearImage, () => HasSourceImage);
            RandomizeSeedCommand = new SimpleRelayCommand(RandomizeSeed);
            CopyPromptCommand = new SimpleRelayCommand<string>(CopyPrompt);
            AddToFavoritesCommand = new SimpleRelayCommand(AddToFavorites, () => !string.IsNullOrWhiteSpace(Prompt));
            LoadPresetCommand = new SimpleRelayCommand<string>(LoadPreset);
            ExportResultCommand = new SimpleRelayCommand(ExportResult, () => HasGeneratedImage);
            SwapImagesCommand = new SimpleRelayCommand(SwapImages, () => HasSourceImage && HasGeneratedImage);
            CropImageCommand = new SimpleRelayCommand(CropImage, () => HasSourceImage);
            RotateImageCommand = new SimpleRelayCommand(RotateImage, () => HasSourceImage);
            ResizeImageCommand = new SimpleRelayCommand(ResizeImage, () => HasSourceImage);
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
                Title = "Select Source Image"
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

        private async Task GenerateImageAsync()
        {
            try
            {
                _cancellationTokenSource = new CancellationTokenSource();
                IsGenerating = true;
                GenerationProgress = 0;
                GenerationStatus = "Starting image transformation...";

                // Add to history
                var historyItem = new Image2ImageHistoryItem
                {
                    Id = Guid.NewGuid(),
                    Prompt = Prompt,
                    NegativePrompt = NegativePrompt,
                    Strength = Strength,
                    Steps = Steps,
                    CfgScale = CfgScale,
                    Sampler = SelectedSampler,
                    Seed = Seed == -1 ? new Random().Next() : Seed,
                    Timestamp = DateTime.Now,
                    Model = SelectedModel,
                    EditMode = SelectedEditMode
                };

                GenerationHistory.Insert(0, historyItem);

                // Simulate generation progress
                for (int i = 0; i <= Steps; i++)
                {
                    if (_cancellationTokenSource.Token.IsCancellationRequested)
                        break;

                    GenerationProgress = (double)i / Steps * 100;
                    GenerationStatus = $"Transforming image - Step {i}/{Steps} ({GenerationProgress:F0}%)";
                    await Task.Delay(75, _cancellationTokenSource.Token);
                }

                if (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    GenerationStatus = "Transformation complete!";
                    HasGeneratedImage = true;
                    
                    // Create result entry
                    var result = new GeneratedImageResult
                    {
                        Id = historyItem.Id,
                        SourceImage = SourceImageSource,
                        ResultImage = null, // Would be actual result
                        Prompt = Prompt,
                        Timestamp = DateTime.Now,
                        Parameters = $"Strength {Strength:F2}, {Steps} steps, CFG {CfgScale:F1}"
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

        private void ClearImage()
        {
            SourceImageSource = null;
            GeneratedImageSource = null;
            HasGeneratedImage = false;
            GenerationStatus = "Images cleared";
        }

        private void RandomizeSeed()
        {
            Seed = new Random().Next(0, int.MaxValue);
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

        private void ExportResult()
        {
            // TODO: Implement export functionality
            GenerationStatus = "Result exported";
        }

        private void SwapImages()
        {
            if (HasSourceImage && HasGeneratedImage)
            {
                var temp = SourceImageSource;
                SourceImageSource = GeneratedImageSource;
                GeneratedImageSource = temp;
                GenerationStatus = "Images swapped";
            }
        }

        private void CropImage()
        {
            // TODO: Implement crop functionality
            GenerationStatus = "Crop tool activated";
        }

        private void RotateImage()
        {
            // TODO: Implement rotate functionality
            GenerationStatus = "Image rotated";
        }

        private void ResizeImage()
        {
            // TODO: Implement resize functionality
            GenerationStatus = "Resize tool activated";
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
                case "Enhance":
                    Strength = 0.3;
                    Steps = 20;
                    CfgScale = 7.0;
                    if (string.IsNullOrEmpty(Prompt))
                        Prompt = "high quality, detailed, enhanced, sharp";
                    NegativePrompt = "blurry, low quality, artifacts, noise";
                    break;
                case "Stylize":
                    Strength = 0.7;
                    Steps = 30;
                    CfgScale = 8.0;
                    if (string.IsNullOrEmpty(Prompt))
                        Prompt = "artistic style, creative interpretation";
                    break;
                case "Fix":
                    Strength = 0.5;
                    Steps = 25;
                    CfgScale = 6.0;
                    if (string.IsNullOrEmpty(Prompt))
                        Prompt = "restored, repaired, clean, high quality";
                    NegativePrompt = "damaged, broken, artifacts, distorted";
                    break;
                case "Artistic":
                    Strength = 0.8;
                    Steps = 35;
                    CfgScale = 9.0;
                    if (string.IsNullOrEmpty(Prompt))
                        Prompt = "artistic masterpiece, painting style, creative";
                    break;
                case "Realistic":
                    Strength = 0.6;
                    Steps = 30;
                    CfgScale = 7.5;
                    if (string.IsNullOrEmpty(Prompt))
                        Prompt = "photorealistic, detailed, natural lighting";
                    NegativePrompt = "cartoon, anime, painting, artistic";
                    break;
                case "Anime":
                    Strength = 0.7;
                    Steps = 28;
                    CfgScale = 8.5;
                    if (string.IsNullOrEmpty(Prompt))
                        Prompt = "anime style, detailed, vibrant colors";
                    NegativePrompt = "realistic, photograph, 3d";
                    break;
            }
        }

        private void InitializeSampleData()
        {
            // Add sample favorite prompts
            FavoritePrompts.Add("enhance quality and detail");
            FavoritePrompts.Add("artistic oil painting style");
            FavoritePrompts.Add("photorealistic with natural lighting");
            FavoritePrompts.Add("anime style with vibrant colors");

            // Add sample history
            GenerationHistory.Add(new Image2ImageHistoryItem
            {
                Id = Guid.NewGuid(),
                Prompt = "enhance quality and detail",
                Strength = 0.3,
                Steps = 20,
                CfgScale = 7.0,
                Sampler = "DPM++ 2M Karras",
                Seed = 123456,
                Timestamp = DateTime.Now.AddMinutes(-10),
                Model = "Default",
                EditMode = "Transform"
            });

            GenerationHistory.Add(new Image2ImageHistoryItem
            {
                Id = Guid.NewGuid(),
                Prompt = "artistic oil painting style",
                Strength = 0.7,
                Steps = 30,
                CfgScale = 8.0,
                Sampler = "Euler A",
                Seed = 789012,
                Timestamp = DateTime.Now.AddMinutes(-25),
                Model = "Stable Diffusion XL",
                EditMode = "Stylize"
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
    public class Image2ImageHistoryItem
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Prompt { get; set; } = "";
        public string NegativePrompt { get; set; } = "";
        public double Strength { get; set; }
        public int Steps { get; set; }
        public double CfgScale { get; set; }
        public string Sampler { get; set; } = "";
        public int Seed { get; set; }
        public DateTime Timestamp { get; set; }
        public string Model { get; set; } = "";
        public string EditMode { get; set; } = "";
        
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

        public string PromptPreview => Prompt.Length > 40 ? Prompt.Substring(0, 37) + "..." : Prompt;
        public string ParametersText => $"Str {Strength:F1}, {Steps} steps, CFG {CfgScale:F1}";
        public string EditModeIcon => EditMode switch
        {
            "Transform" => "🔄",
            "Inpaint" => "🖌️",
            "Outpaint" => "🖼️",
            "Upscale" => "🔍",
            _ => "⚙️"
        };
    }

    public class GeneratedImageResult
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public BitmapSource? SourceImage { get; set; }
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