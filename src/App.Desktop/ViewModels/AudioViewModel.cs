using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Lazarus.Desktop.ViewModels
{
    public sealed class AudioViewModel : INotifyPropertyChanged
    {
        // ===== Models =====
        public sealed class AudioItemVm
        {
            public Guid Id { get; init; } = Guid.NewGuid();
            public string Name { get; init; } = "";
            public string Duration { get; init; } = "00:00";
            public string Size { get; init; } = "—";
            public DateTime Modified { get; init; } = DateTime.Now;
            public string Format { get; init; } = "wav";
            public string Path { get; init; } = "";
            public string Hash { get; init; } = "";
            // Optional audio props for the info grid:
            public string SampleRate { get; init; } = "44,100 Hz";
            public string Channels { get; init; } = "Mono";
            public string Bitrate { get; init; } = "—";
        }

        public sealed class JobVm
        {
            public string Title { get; init; } = "";
            public double ProgressPercent { get; set; }
            public string StatusText { get; set; } = "Pending";
        }

        // ===== Observable State =====
        public ObservableCollection<AudioItemVm> Items { get; } = new();
        public AudioItemVm? ActiveItem
        {
            get => _activeItem;
            set { _activeItem = value; OnPropertyChanged(); }
        }
        private AudioItemVm? _activeItem;

        public ObservableCollection<JobVm> Jobs { get; } = new();

        public bool IsPreviewEnabled
        {
            get => _isPreviewEnabled;
            set { _isPreviewEnabled = value; OnPropertyChanged(); }
        }
        private bool _isPreviewEnabled = true;

        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(); /* hook filter later */ }
        }
        private string _searchText = "";

        public string SortMode
        {
            get => _sortMode;
            set { _sortMode = value; OnPropertyChanged(); /* hook sort later */ }
        }
        private string _sortMode = "Name";

        // Transport
        public string PlayPauseLabel => _isPlaying ? "⏸ Pause" : "▶ Play";
        public double DurationSeconds
        {
            get => _durationSeconds;
            set { _durationSeconds = value; OnPropertyChanged(); DurationText = TimeSpan.FromSeconds(value).ToString(@"mm\:ss"); }
        }
        private double _durationSeconds = 180;

        public double PositionSeconds
        {
            get => _positionSeconds;
            set { _positionSeconds = value; OnPropertyChanged(); PositionText = TimeSpan.FromSeconds(value).ToString(@"mm\:ss"); }
        }
        private double _positionSeconds;

        public string PositionText
        {
            get => _positionText;
            private set { _positionText = value; OnPropertyChanged(); }
        }
        private string _positionText = "00:00";

        public string DurationText
        {
            get => _durationText;
            private set { _durationText = value; OnPropertyChanged(); }
        }
        private string _durationText = "03:00";

        public ObservableCollection<string> OutputDevices { get; } = new() { "Default Output", "Speakers (WASAPI)", "Headset (WASAPI)" };
        public string? SelectedOutputDevice
        {
            get => _selectedOutputDevice;
            set { _selectedOutputDevice = value; OnPropertyChanged(); }
        }
        private string? _selectedOutputDevice = "Default Output";

        // ===== Commands =====
        public ICommand RecordCmd { get; }
        public ICommand StopRecordCmd { get; }
        public ICommand ImportCmd { get; }
        public ICommand PlayPauseCmd { get; }
        public ICommand StopCmd { get; }
        public ICommand OpenFolderCmd { get; }
        public ICommand DeleteCmd { get; }
        public ICommand TranscribeCmd { get; }
        public ICommand NoiseReduceCmd { get; }
        public ICommand VadTrimCmd { get; }
        public ICommand NormalizeCmd { get; }
        public ICommand ConvertCmd { get; }
        public ICommand SplitOnSilenceCmd { get; }
        public ICommand SynthesizeCmd { get; }
        public ICommand CloneVoiceCmd { get; }

        private bool _isPlaying;

        public AudioViewModel()
        {
            // Placeholder rows so the whole UI is visible instantly
            var now = DateTime.Now;
            Items.Add(new AudioItemVm { Name = "Morning Coffee Jazz.mp3", Duration = "02:24", Size = "13.6 MB", Modified = now.AddDays(-2), Format = "mp3", Path = "C:\\Audio\\morning.mp3" });
            Items.Add(new AudioItemVm { Name = "Ambient Rain Sounds.wav", Duration = "08:31", Size = "12.7 MB", Modified = now.AddDays(-1), Format = "wav", Path = "C:\\Audio\\rain.wav" });
            Items.Add(new AudioItemVm { Name = "Voice Memo - Project Ideas.m4a", Duration = "07:03", Size = "29 MB", Modified = now, Format = "m4a", Path = "C:\\Audio\\memo.m4a" });
            ActiveItem = Items.Count > 0 ? Items[0] : null;

            Jobs.Add(new JobVm { Title = "Transcribe: Voice Memo - Project Ideas", ProgressPercent = 72, StatusText = "Running…" });

            // Commands (stubs; replace with real services)
            RecordCmd = new RelayCommand(_ => AddJob("Recording…"));
            StopRecordCmd = new RelayCommand(_ => AddJob("Stop Recording"));
            ImportCmd = new RelayCommand(_ => AddJob("Import File"));
            PlayPauseCmd = new RelayCommand(_ => TogglePlay());
            StopCmd = new RelayCommand(_ => { _isPlaying = false; PositionSeconds = 0; Raise(nameof(PlayPauseLabel)); });
            OpenFolderCmd = new RelayCommand(_ => AddJob("Open Folder"));
            DeleteCmd = new RelayCommand(_ => AddJob("Delete File"));
            TranscribeCmd = new RelayCommand(_ => AddJob("ASR: Transcribe"));
            NoiseReduceCmd = new RelayCommand(_ => AddJob("Noise Reduction"));
            VadTrimCmd = new RelayCommand(_ => AddJob("VAD Trim"));
            NormalizeCmd = new RelayCommand(_ => AddJob("Normalize"));
            ConvertCmd = new RelayCommand(_ => AddJob("Convert"));
            SplitOnSilenceCmd = new RelayCommand(_ => AddJob("Split on Silence"));
            SynthesizeCmd = new RelayCommand(_ => AddJob("TTS Synthesize"));
            CloneVoiceCmd = new RelayCommand(_ => AddJob("Voice Clone"));
        }

        private void TogglePlay()
        {
            _isPlaying = !_isPlaying;
            Raise(nameof(PlayPauseLabel));
            // Timer hookup for PositionSeconds can be added later
        }

        private void AddJob(string title)
        {
            Jobs.Add(new JobVm { Title = title, ProgressPercent = 0, StatusText = "Queued" });
        }

        // ===== INotifyPropertyChanged =====
        public event PropertyChangedEventHandler? PropertyChanged;
        private void Raise(string? name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name ?? string.Empty));
        private void OnPropertyChanged([CallerMemberName] string? name = null) => Raise(name);

        // ===== Minimal RelayCommand =====
        public sealed class RelayCommand : ICommand
        {
            private readonly Action<object?> _exec;
            private readonly Func<object?, bool>? _can;
            public RelayCommand(Action<object?> exec, Func<object?, bool>? can = null) { _exec = exec; _can = can; }
            public bool CanExecute(object? p) => _can?.Invoke(p) ?? true;
            public void Execute(object? p) => _exec(p);
            public event EventHandler? CanExecuteChanged { add { CommandManager.RequerySuggested += value; } remove { CommandManager.RequerySuggested -= value; } }
        }
    }
}
