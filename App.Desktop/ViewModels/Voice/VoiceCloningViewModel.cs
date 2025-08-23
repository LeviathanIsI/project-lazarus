using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Lazarus.Desktop.Helpers;

namespace Lazarus.Desktop.ViewModels.Voice
{
    public class VoiceCloningViewModel : INotifyPropertyChanged
    {
        private string _voiceName = "";
        private string _qualityLevel = "Balanced";
        private double _trainingDuration = 15;
        private bool _isTraining = false;
        private double _trainingProgress = 0;
        private string _trainingStatus = "Ready to train voice model";

        public VoiceCloningViewModel()
        {
            BrowseAudioCommand = new SimpleRelayCommand(BrowseAudio);
            StartTrainingCommand = new SimpleRelayCommand(StartTraining, CanStartTraining);
            ClonedVoices = new ObservableCollection<ClonedVoice>
            {
                new() { Name = "John Clone", Quality = "High Quality" },
                new() { Name = "Emma Clone", Quality = "Medium Quality" }
            };
        }

        public string VoiceName
        {
            get => _voiceName;
            set => SetProperty(ref _voiceName, value);
        }

        public string QualityLevel
        {
            get => _qualityLevel;
            set => SetProperty(ref _qualityLevel, value);
        }

        public double TrainingDuration
        {
            get => _trainingDuration;
            set => SetProperty(ref _trainingDuration, value);
        }

        public bool IsTraining
        {
            get => _isTraining;
            set => SetProperty(ref _isTraining, value);
        }

        public double TrainingProgress
        {
            get => _trainingProgress;
            set => SetProperty(ref _trainingProgress, value);
        }

        public string TrainingStatus
        {
            get => _trainingStatus;
            set => SetProperty(ref _trainingStatus, value);
        }

        public ObservableCollection<ClonedVoice> ClonedVoices { get; }

        public ICommand BrowseAudioCommand { get; }
        public ICommand StartTrainingCommand { get; }

        private void BrowseAudio()
        {
            System.Diagnostics.Debug.WriteLine("Browse audio for training");
        }

        private bool CanStartTraining()
        {
            return !IsTraining && !string.IsNullOrWhiteSpace(VoiceName);
        }

        private async void StartTraining()
        {
            try
            {
                IsTraining = true;
                TrainingProgress = 0;

                for (int i = 0; i <= 100; i += 5)
                {
                    TrainingProgress = i;
                    TrainingStatus = $"Training voice model... {i}%";
                    await Task.Delay(200);
                }

                TrainingStatus = "Training complete!";
                ClonedVoices.Add(new ClonedVoice { Name = VoiceName, Quality = QualityLevel });
            }
            finally
            {
                IsTraining = false;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }

    public class ClonedVoice
    {
        public string Name { get; set; } = "";
        public string Quality { get; set; } = "";
    }
}