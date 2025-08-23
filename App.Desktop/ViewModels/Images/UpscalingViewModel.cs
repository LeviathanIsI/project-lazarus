using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Lazarus.Desktop.Helpers;

namespace Lazarus.Desktop.ViewModels.Images
{
    public class UpscalingViewModel : INotifyPropertyChanged
    {
        private int _upscaleFactor = 4;
        private string _upscalerModel = "RealESRGAN_x4plus";
        private bool _faceEnhancement = false;
        private double _upscaleProgress = 0;
        private BitmapSource? _sourceImageSource;
        private BitmapSource? _upscaledImageSource;
        private bool _hasUpscaledImage;
        private bool _isUpscaling;

        public UpscalingViewModel()
        {
            LoadImageCommand = new SimpleRelayCommand(LoadImage);
            UpscaleCommand = new SimpleRelayCommand(UpscaleImage, CanUpscale);
            UpscalingHistory = new ObservableCollection<UpscalingHistoryItem>();
        }

        public int UpscaleFactor
        {
            get => _upscaleFactor;
            set => SetProperty(ref _upscaleFactor, value);
        }

        public string UpscalerModel
        {
            get => _upscalerModel;
            set => SetProperty(ref _upscalerModel, value);
        }

        public bool FaceEnhancement
        {
            get => _faceEnhancement;
            set => SetProperty(ref _faceEnhancement, value);
        }

        public double UpscaleProgress
        {
            get => _upscaleProgress;
            set => SetProperty(ref _upscaleProgress, value);
        }

        public BitmapSource? SourceImageSource
        {
            get => _sourceImageSource;
            set => SetProperty(ref _sourceImageSource, value);
        }

        public BitmapSource? UpscaledImageSource
        {
            get => _upscaledImageSource;
            set => SetProperty(ref _upscaledImageSource, value);
        }

        public bool HasUpscaledImage
        {
            get => _hasUpscaledImage;
            set => SetProperty(ref _hasUpscaledImage, value);
        }

        public bool IsUpscaling
        {
            get => _isUpscaling;
            set => SetProperty(ref _isUpscaling, value);
        }

        public ObservableCollection<UpscalingHistoryItem> UpscalingHistory { get; }

        public ICommand LoadImageCommand { get; }
        public ICommand UpscaleCommand { get; }

        private void LoadImage()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Image files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|All files (*.*)|*.*",
                Title = "Select Image to Upscale"
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

        private bool CanUpscale()
        {
            return !IsUpscaling && SourceImageSource != null;
        }

        private async void UpscaleImage()
        {
            try
            {
                IsUpscaling = true;
                UpscaleProgress = 0;

                // TODO: Implement actual upscaling via API
                // For now, just simulate progress and add to history
                for (int i = 0; i <= 100; i += 10)
                {
                    UpscaleProgress = i;
                    await Task.Delay(200); // Simulate upscaling time
                }

                UpscalingHistory.Insert(0, new UpscalingHistoryItem
                {
                    UpscalerModel = UpscalerModel,
                    UpscaleFactor = UpscaleFactor,
                    FaceEnhancement = FaceEnhancement,
                    Timestamp = DateTime.Now.ToString("HH:mm:ss")
                });

                // Placeholder: Set HasUpscaledImage to true for UI testing
                HasUpscaledImage = true;
            }
            catch (Exception ex)
            {
                // TODO: Handle upscaling errors
                System.Diagnostics.Debug.WriteLine($"Upscaling error: {ex.Message}");
            }
            finally
            {
                IsUpscaling = false;
                UpscaleProgress = 0;
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

    public class UpscalingHistoryItem
    {
        public string UpscalerModel { get; set; } = "";
        public int UpscaleFactor { get; set; }
        public bool FaceEnhancement { get; set; }
        public string Timestamp { get; set; } = "";

        public string FactorText => $"{UpscaleFactor}x {(FaceEnhancement ? "with Face Enhancement" : "")}".Trim();
    }
}