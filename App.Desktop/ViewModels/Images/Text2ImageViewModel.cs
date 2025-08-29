using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Lazarus.Desktop.Helpers;

namespace Lazarus.Desktop.ViewModels.Images
{
    public class Text2ImageViewModel : INotifyPropertyChanged
    {
        #region Private Fields
        private string _prompt = "";
        private string _negativePrompt = "";
        private int _steps = 25;
        private double _cfgScale = 7.5;
        private int _width = 512;
        private int _height = 512;
        private string _selectedSampler = "DPM++ 2M Karras";
        private int _seed = -1;
        private int _batchSize = 1;
        private double _denoisingStrength = 1.0;
        private string _selectedStylePreset = "None";
        private string _selectedModel = "Default";
        private BitmapSource? _generatedImageSource;
        private bool _hasGeneratedImage;
        private bool _isGenerating;
        private double _generationProgress;
        private string _generationStatus = "";
        private string _selectedSize = "512x512";
        private bool _useNegativePrompt = false;
        private bool _showAdvancedSettings = false;
        private GenerationHistoryItem? _selectedHistoryItem;
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
                    OnPropertyChanged(nameof(TokenCount));
                }
            }
        }

        public string NegativePrompt
        {
            get => _negativePrompt;
            set => SetProperty(ref _negativePrompt, value);
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

        public int Width
        {
            get => _width;
            set => SetProperty(ref _width, value);
        }

        public int Height
        {
            get => _height;
            set => SetProperty(ref _height, value);
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

        public int BatchSize
        {
            get => _batchSize;
            set => SetProperty(ref _batchSize, value);
        }

        public double DenoisingStrength
        {
            get => _denoisingStrength;
            set => SetProperty(ref _denoisingStrength, value);
        }

        public string SelectedStylePreset
        {
            get => _selectedStylePreset;
            set
            {
                if (SetProperty(ref _selectedStylePreset, value))
                {
                    ApplyStylePreset(value);
                }
            }
        }

        public string SelectedModel
        {
            get => _selectedModel;
            set => SetProperty(ref _selectedModel, value);
        }

        public BitmapSource? GeneratedImageSource
        {
            get => _generatedImageSource;
            set => SetProperty(ref _generatedImageSource, value);
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

        public string SelectedSize
        {
            get => _selectedSize;
            set
            {
                if (SetProperty(ref _selectedSize, value))
                {
                    ApplySize(value);
                }
            }
        }

        public bool UseNegativePrompt
        {
            get => _useNegativePrompt;
            set => SetProperty(ref _useNegativePrompt, value);
        }

        public bool ShowAdvancedSettings
        {
            get => _showAdvancedSettings;
            set => SetProperty(ref _showAdvancedSettings, value);
        }

        public GenerationHistoryItem? SelectedHistoryItem
        {
            get => _selectedHistoryItem;
            set
            {
                if (SetProperty(ref _selectedHistoryItem, value) && value != null)
                {
                    LoadFromHistory(value);
                }
            }
        }

        // Computed Properties
        public bool CanGenerate => !IsGenerating && !string.IsNullOrWhiteSpace(Prompt);
        public bool CanCancel => IsGenerating;
        public int TokenCount => Prompt?.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length ?? 0;
        public string SeedDisplay => Seed == -1 ? "Random" : Seed.ToString();
        public string BatchCostEstimate => $"~{BatchSize * Steps * 0.1:F1}s";
        #endregion

        #region Collections
        public ObservableCollection<GenerationHistoryItem> GenerationHistory { get; } = new();
        public ObservableCollection<string> FavoritePrompts { get; } = new();
        public ObservableCollection<GeneratedImage> ImageGallery { get; } = new();

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

        public List<string> StylePresets { get; } = new()
        {
            "None",
            "Portrait",
            "Landscape",
            "Art",
            "Photography",
            "Digital Art",
            "Anime",
            "Realistic",
            "Fantasy",
            "Sci-Fi"
        };

        public List<string> SizePresets { get; } = new()
        {
            "512x512",
            "512x768",
            "768x512",
            "768x768",
            "1024x768",
            "768x1024",
            "1024x1024"
        };

        public List<string> AvailableModels { get; } = new()
        {
            "Default",
            "Stable Diffusion 1.5",
            "Stable Diffusion XL",
            "Midjourney",
            "DALL-E 3"
        };
        #endregion

        #region Commands
        public ICommand GenerateCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand SaveImageCommand { get; }
        public ICommand LoadImageCommand { get; }
        public ICommand RandomizeSeedCommand { get; }
        public ICommand CopyPromptCommand { get; }
        public ICommand AddToFavoritesCommand { get; }
        public ICommand ClearHistoryCommand { get; }
        public ICommand LoadPresetCommand { get; }
        public ICommand ExportImageCommand { get; }
        public ICommand UseAsInitImageCommand { get; }
        #endregion

        public Text2ImageViewModel()
        {
            // Initialize commands
            GenerateCommand = new SimpleRelayCommand(async () => await GenerateImageAsync(), () => CanGenerate);
            CancelCommand = new SimpleRelayCommand(CancelGeneration, () => CanCancel);
            SaveImageCommand = new SimpleRelayCommand(SaveImage, () => HasGeneratedImage);
            LoadImageCommand = new SimpleRelayCommand(LoadImage);
            RandomizeSeedCommand = new SimpleRelayCommand(RandomizeSeed);
            CopyPromptCommand = new SimpleRelayCommand<string>(CopyPrompt);
            AddToFavoritesCommand = new SimpleRelayCommand(AddToFavorites, () => !string.IsNullOrWhiteSpace(Prompt));
            ClearHistoryCommand = new SimpleRelayCommand(ClearHistory, () => GenerationHistory.Count > 0);
            LoadPresetCommand = new SimpleRelayCommand<string>(LoadPreset);
            ExportImageCommand = new SimpleRelayCommand(ExportImage, () => HasGeneratedImage);
            UseAsInitImageCommand = new SimpleRelayCommand(UseAsInitImage, () => HasGeneratedImage);

            // Initialize sample data
            InitializeSampleData();
        }

        #region Command Implementations
        private async Task GenerateImageAsync()
        {
            try
            {
                _cancellationTokenSource = new CancellationTokenSource();
                IsGenerating = true;
                GenerationProgress = 0;
                GenerationStatus = "Initializing generation...";

                // Add to history
                var historyItem = new GenerationHistoryItem
                {
                    Id = Guid.NewGuid(),
                    Prompt = Prompt,
                    NegativePrompt = NegativePrompt,
                    Steps = Steps,
                    CfgScale = CfgScale,
                    Width = Width,
                    Height = Height,
                    Sampler = SelectedSampler,
                    Seed = Seed == -1 ? new Random().Next() : Seed,
                    Timestamp = DateTime.Now,
                    Model = SelectedModel
                };

                GenerationHistory.Insert(0, historyItem);

                // Simulate generation progress
                for (int i = 0; i <= Steps; i++)
                {
                    if (_cancellationTokenSource.Token.IsCancellationRequested)
                        break;

                    GenerationProgress = (double)i / Steps * 100;
                    GenerationStatus = $"Step {i}/{Steps} - {GenerationProgress:F0}%";
                    await Task.Delay(50, _cancellationTokenSource.Token);
                }

                if (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    // TODO: Implement actual image generation API call
                    GenerationStatus = "Generation complete!";
                    HasGeneratedImage = true;
                    
                    // Create sample generated image for UI testing
                    var generatedImage = new GeneratedImage
                    {
                        Id = historyItem.Id,
                        Source = null, // Would be actual BitmapSource
                        Prompt = Prompt,
                        Timestamp = DateTime.Now,
                        Parameters = $"{Steps} steps, CFG {CfgScale:F1}, {SelectedSampler}"
                    };
                    ImageGallery.Insert(0, generatedImage);
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

        private void SaveImage()
        {
            // TODO: Implement save image functionality
            GenerationStatus = "Image saved successfully!";
        }

        private void LoadImage()
        {
            // TODO: Implement load image functionality
        }

        private void RandomizeSeed()
        {
            Seed = new Random().Next(0, int.MaxValue);
        }

        private void CopyPrompt(string? prompt)
        {
            if (!string.IsNullOrEmpty(prompt))
            {
                // TODO: Copy to clipboard
                System.Windows.Clipboard.SetText(prompt);
            }
        }

        private void AddToFavorites()
        {
            if (!string.IsNullOrWhiteSpace(Prompt) && !FavoritePrompts.Contains(Prompt))
            {
                FavoritePrompts.Add(Prompt);
            }
        }

        private void ClearHistory()
        {
            GenerationHistory.Clear();
            ImageGallery.Clear();
        }

        private void LoadPreset(string? presetName)
        {
            if (string.IsNullOrEmpty(presetName)) return;

            // TODO: Load actual presets from configuration
            switch (presetName)
            {
                case "Portrait":
                    Width = 512;
                    Height = 768;
                    CfgScale = 7.0;
                    Steps = 25;
                    break;
                case "Landscape":
                    Width = 768;
                    Height = 512;
                    CfgScale = 7.5;
                    Steps = 25;
                    break;
                case "Art":
                    Width = 768;
                    Height = 768;
                    CfgScale = 8.0;
                    Steps = 30;
                    break;
            }
        }

        private void ExportImage()
        {
            // TODO: Implement export functionality
        }

        private void UseAsInitImage()
        {
            // TODO: Implement init image functionality
        }

        private void LoadFromHistory(GenerationHistoryItem item)
        {
            Prompt = item.Prompt;
            NegativePrompt = item.NegativePrompt;
            Steps = item.Steps;
            CfgScale = item.CfgScale;
            Width = item.Width;
            Height = item.Height;
            SelectedSampler = item.Sampler;
            Seed = item.Seed;
        }

        private void ApplyStylePreset(string preset)
        {
            switch (preset)
            {
                case "Portrait":
                    if (string.IsNullOrEmpty(Prompt))
                        Prompt = "portrait of a person, professional photography, high quality";
                    break;
                case "Landscape":
                    if (string.IsNullOrEmpty(Prompt))
                        Prompt = "beautiful landscape, natural lighting, high detail";
                    break;
                case "Art":
                    if (string.IsNullOrEmpty(Prompt))
                        Prompt = "artistic masterpiece, detailed, vibrant colors";
                    break;
                case "Photography":
                    NegativePrompt = "blurry, low quality, artifacts, distorted";
                    CfgScale = 7.0;
                    break;
                case "Digital Art":
                    NegativePrompt = "photograph, realistic, low quality";
                    CfgScale = 8.0;
                    break;
                case "Anime":
                    if (string.IsNullOrEmpty(Prompt))
                        Prompt = "anime style, detailed, high quality";
                    NegativePrompt = "realistic, photograph, 3d, low quality";
                    CfgScale = 7.5;
                    break;
            }
        }

        private void ApplySize(string size)
        {
            var parts = size.Split('x');
            if (parts.Length == 2 && int.TryParse(parts[0], out int w) && int.TryParse(parts[1], out int h))
            {
                Width = w;
                Height = h;
            }
        }

        private void InitializeSampleData()
        {
            // Add sample prompts
            FavoritePrompts.Add("beautiful landscape with mountains and lake");
            FavoritePrompts.Add("portrait of a person with natural lighting");
            FavoritePrompts.Add("futuristic cityscape at sunset");
            FavoritePrompts.Add("magical forest with glowing mushrooms");

            // Add sample history
            GenerationHistory.Add(new GenerationHistoryItem
            {
                Id = Guid.NewGuid(),
                Prompt = "beautiful sunset over mountains",
                Steps = 25,
                CfgScale = 7.5,
                Width = 768,
                Height = 512,
                Sampler = "DPM++ 2M Karras",
                Seed = 123456,
                Timestamp = DateTime.Now.AddMinutes(-5),
                Model = "Stable Diffusion XL"
            });

            GenerationHistory.Add(new GenerationHistoryItem
            {
                Id = Guid.NewGuid(),
                Prompt = "portrait of a wizard, fantasy art",
                Steps = 30,
                CfgScale = 8.0,
                Width = 512,
                Height = 768,
                Sampler = "Euler A",
                Seed = 789012,
                Timestamp = DateTime.Now.AddMinutes(-15),
                Model = "Stable Diffusion 1.5"
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
    public class GenerationHistoryItem
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Prompt { get; set; } = "";
        public string NegativePrompt { get; set; } = "";
        public int Steps { get; set; }
        public double CfgScale { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public string Sampler { get; set; } = "";
        public int Seed { get; set; }
        public DateTime Timestamp { get; set; }
        public string Model { get; set; } = "";
        
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

        public string PromptPreview => Prompt.Length > 50 ? Prompt.Substring(0, 47) + "..." : Prompt;
        public string SizeText => $"{Width}x{Height}";
        public string ParametersText => $"{Steps} steps, CFG {CfgScale:F1}";
    }

    public class GeneratedImage
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public BitmapSource? Source { get; set; }
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