using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Lazarus.Desktop.ViewModels.Voice
{
    public class VoiceViewModel : INotifyPropertyChanged
    {
        public TTSConfigurationViewModel TTSConfiguration { get; }
        public VoiceCloningViewModel VoiceCloning { get; }
        public RealTimeSynthesisViewModel RealTimeSynthesis { get; }
        public AudioProcessingViewModel AudioProcessing { get; }

        public VoiceViewModel()
        {
            TTSConfiguration = new TTSConfigurationViewModel();
            VoiceCloning = new VoiceCloningViewModel();
            RealTimeSynthesis = new RealTimeSynthesisViewModel();
            AudioProcessing = new AudioProcessingViewModel();

            // Wire up communication between ViewModels if needed
            TTSConfiguration.PropertyChanged += OnSubViewModelPropertyChanged;
            VoiceCloning.PropertyChanged += OnSubViewModelPropertyChanged;
            RealTimeSynthesis.PropertyChanged += OnSubViewModelPropertyChanged;
            AudioProcessing.PropertyChanged += OnSubViewModelPropertyChanged;
        }

        private void OnSubViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // Handle cross-communication between voice sub-tabs if needed
            // For example, sharing voice models between TTS and cloning
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}