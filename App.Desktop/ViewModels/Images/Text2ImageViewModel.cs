using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Lazarus.Desktop.Helpers;

namespace Lazarus.Desktop.ViewModels.Images
{
    public class Text2ImageViewModel : INotifyPropertyChanged
    {
        private string _prompt = "";
        private string _negativePrompt = "";
        private int _steps = 20;
        private double _cfgScale = 7.0;
        private int _width = 512;
        private int _height = 512;
        private BitmapSource? _generatedImageSource;
        private bool _hasGeneratedImage;
        private bool _isGenerating;

        public Text2ImageViewModel()
        {
            GenerateCommand = new SimpleRelayCommand(GenerateImage, CanGenerate);
            GenerationHistory = new ObservableCollection<GenerationHistoryItem>();
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
            set => SetProperty(ref _isGenerating, value);
        }

        public ObservableCollection<GenerationHistoryItem> GenerationHistory { get; }

        public ICommand GenerateCommand { get; }

        private bool CanGenerate()
        {
            return !IsGenerating && !string.IsNullOrWhiteSpace(Prompt);
        }

        private async void GenerateImage()
        {
            try
            {
                IsGenerating = true;

                // TODO: Implement actual image generation via API
                // For now, just add to history
                GenerationHistory.Insert(0, new GenerationHistoryItem
                {
                    Prompt = Prompt,
                    NegativePrompt = NegativePrompt,
                    Steps = Steps,
                    CfgScale = CfgScale,
                    Width = Width,
                    Height = Height,
                    Timestamp = DateTime.Now.ToString("HH:mm:ss")
                });

                // Placeholder: Set HasGeneratedImage to true for UI testing
                await Task.Delay(1000); // Simulate generation time
                HasGeneratedImage = true;
            }
            catch (Exception ex)
            {
                // TODO: Handle generation errors
                System.Diagnostics.Debug.WriteLine($"Generation error: {ex.Message}");
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

    public class GenerationHistoryItem
    {
        public string Prompt { get; set; } = "";
        public string NegativePrompt { get; set; } = "";
        public int Steps { get; set; }
        public double CfgScale { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public string Timestamp { get; set; } = "";
    }
}