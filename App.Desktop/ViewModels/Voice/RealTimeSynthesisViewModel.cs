using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Lazarus.Desktop.Helpers;

namespace Lazarus.Desktop.ViewModels.Voice
{
    public class RealTimeSynthesisViewModel : INotifyPropertyChanged
    {
        private bool _isSpeaking;
        private string _quickText = string.Empty;
        private VoiceInfo _currentVoice = new("Default Voice");

        public bool IsSpeaking
        {
            get => _isSpeaking;
            set { if (_isSpeaking != value) { _isSpeaking = value; OnPropertyChanged(); } }
        }

        public string QuickText
        {
            get => _quickText;
            set { if (_quickText != value) { _quickText = value; OnPropertyChanged(); } }
        }

        public VoiceInfo CurrentVoice
        {
            get => _currentVoice;
            set { if (_currentVoice != value) { _currentVoice = value; OnPropertyChanged(); } }
        }

        public ICommand ToggleSpeechCommand { get; }

        public RealTimeSynthesisViewModel()
        {
            ToggleSpeechCommand = new RelayCommand(_ => ToggleSpeech());
        }

        private void ToggleSpeech()
        {
            IsSpeaking = !IsSpeaking;
            // Placeholder: integrate audio IO later
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public record VoiceInfo(string DisplayName);
}