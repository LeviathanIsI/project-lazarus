using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Lazarus.Desktop.ViewModels.Images
{
    public class ImagesViewModel : INotifyPropertyChanged
    {
        public Text2ImageViewModel Text2Image { get; }
        public Image2ImageViewModel Image2Image { get; }
        public InpaintingViewModel Inpainting { get; }
        public UpscalingViewModel Upscaling { get; }

        public ImagesViewModel()
        {
            Text2Image = new Text2ImageViewModel();
            Image2Image = new Image2ImageViewModel();
            Inpainting = new InpaintingViewModel();
            Upscaling = new UpscalingViewModel();

            // Wire up communication between ViewModels if needed
            Text2Image.PropertyChanged += OnSubViewModelPropertyChanged;
            Image2Image.PropertyChanged += OnSubViewModelPropertyChanged;
            Inpainting.PropertyChanged += OnSubViewModelPropertyChanged;
            Upscaling.PropertyChanged += OnSubViewModelPropertyChanged;
        }

        private void OnSubViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // Handle cross-communication between image generation sub-tabs if needed
            // For example, passing generated images between tabs for further processing
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}