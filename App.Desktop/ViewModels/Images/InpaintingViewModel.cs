using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Lazarus.Desktop.Helpers;

namespace Lazarus.Desktop.ViewModels.Images
{
    public class InpaintingViewModel : INotifyPropertyChanged
    {
        private string _prompt = "";
        private int _maskBlur = 4;
        private BitmapSource? _sourceImageSource;
        private BitmapSource? _maskImageSource;
        private BitmapSource? _generatedImageSource;
        private bool _hasGeneratedImage;
        private bool _isGenerating;

        public InpaintingViewModel()
        {
            LoadImageCommand = new SimpleRelayCommand(LoadImage);
            LoadMaskCommand = new SimpleRelayCommand(LoadMask);
            GenerateCommand = new SimpleRelayCommand(GenerateImage, CanGenerate);
            GenerationHistory = new ObservableCollection<InpaintingHistoryItem>();
        }

        public string Prompt
        {
            get => _prompt;
            set => SetProperty(ref _prompt, value);
        }

        public int MaskBlur
        {
            get => _maskBlur;
            set => SetProperty(ref _maskBlur, value);
        }

        public BitmapSource? SourceImageSource
        {
            get => _sourceImageSource;
            set => SetProperty(ref _sourceImageSource, value);
        }

        public BitmapSource? MaskImageSource
        {
            get => _maskImageSource;
            set => SetProperty(ref _maskImageSource, value);
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

        public ObservableCollection<InpaintingHistoryItem> GenerationHistory { get; }

        public ICommand LoadImageCommand { get; }
        public ICommand LoadMaskCommand { get; }
        public ICommand GenerateCommand { get; }

        private void LoadImage()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Image files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|All files (*.*)|*.*",
                Title = "Select Source Image"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    var bitmap = new BitmapImage(new Uri(openFileDialog.FileName));
                    SourceImageSource = bitmap;
                }
                catch (Exception ex)
                {
                    // TODO: Handle image loading errors
                    System.Diagnostics.Debug.WriteLine($"Image loading error: {ex.Message}");
                }
            }
        }

        private void LoadMask()
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
                    var bitmap = new BitmapImage(new Uri(openFileDialog.FileName));
                    MaskImageSource = bitmap;
                }
                catch (Exception ex)
                {
                    // TODO: Handle mask loading errors
                    System.Diagnostics.Debug.WriteLine($"Mask loading error: {ex.Message}");
                }
            }
        }

        private bool CanGenerate()
        {
            return !IsGenerating && SourceImageSource != null && MaskImageSource != null && !string.IsNullOrWhiteSpace(Prompt);
        }

        private async void GenerateImage()
        {
            try
            {
                IsGenerating = true;

                // TODO: Implement actual inpainting generation via API
                // For now, just add to history
                GenerationHistory.Insert(0, new InpaintingHistoryItem
                {
                    Prompt = Prompt,
                    MaskBlur = MaskBlur,
                    Timestamp = DateTime.Now.ToString("HH:mm:ss")
                });

                // Placeholder: Set HasGeneratedImage to true for UI testing
                await Task.Delay(2000); // Simulate generation time
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

    public class InpaintingHistoryItem
    {
        public string Prompt { get; set; } = "";
        public int MaskBlur { get; set; }
        public string Timestamp { get; set; } = "";
    }
}