using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using Lazarus.Desktop.Services;
using Lazarus.Shared.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace Lazarus.Desktop.ViewModels;

public sealed class AudioViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly IAudioService _audioService;
    private readonly ILogger<AudioViewModel> _logger;
    private readonly IAppState _appState;
    private readonly DispatcherTimer _playbackTimer;
    private CancellationTokenSource? _cts = null;
    
    // Collections
    public ObservableCollection<AudioItem> Items { get; }

    // Commands
    public ICommand ImportCommand { get; }
    public ICommand RecordCommand { get; }
    public ICommand PlayPauseCommand { get; }
    public ICommand StopCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand RefreshCommand { get; }

    // Properties
    private AudioItem? _selectedItem;
    public AudioItem? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (_selectedItem != value)
            {
                _selectedItem = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedItemDisplay));
                OnPropertyChanged(nameof(CanPlay));
                OnPropertyChanged(nameof(CanDelete));
            }
        }
    }

    private bool _isPlaying;
    public bool IsPlaying
    {
        get => _isPlaying;
        private set
        {
            if (_isPlaying != value)
            {
                _isPlaying = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanPlay));
            }
        }
    }

    private TimeSpan _currentPosition;
    public TimeSpan CurrentPosition
    {
        get => _currentPosition;
        private set
        {
            if (_currentPosition != value)
            {
                _currentPosition = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CurrentPositionDisplay));
                OnPropertyChanged(nameof(PlaybackProgress));
            }
        }
    }

    private TimeSpan _currentDuration;
    public TimeSpan CurrentDuration
    {
        get => _currentDuration;
        private set
        {
            if (_currentDuration != value)
            {
                _currentDuration = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CurrentDurationDisplay));
                OnPropertyChanged(nameof(PlaybackProgress));
            }
        }
    }

    private int _totalFiles;
    public int TotalFiles
    {
        get => _totalFiles;
        private set
        {
            if (_totalFiles != value)
            {
                _totalFiles = value;
                OnPropertyChanged();
            }
        }
    }

    private string _totalSizeDisplay = "0 MB";
    public string TotalSizeDisplay
    {
        get => _totalSizeDisplay;
        private set
        {
            if (_totalSizeDisplay != value)
            {
                _totalSizeDisplay = value;
                OnPropertyChanged();
            }
        }
    }

    private string _statusText = "Ready";
    public string StatusText
    {
        get => _statusText;
        private set
        {
            if (_statusText != value)
            {
                _statusText = value;
                OnPropertyChanged();
            }
        }
    }

    // Computed Properties
    public bool HasItems => Items.Count > 0;
    public bool CanPlay => SelectedItem != null && !IsPlaying;
    public bool CanDelete => SelectedItem != null && !IsPlaying;
    
    public string SelectedItemDisplay => SelectedItem?.FileName ?? "No file selected";
    public string CurrentPositionDisplay => FormatTimeSpan(CurrentPosition);
    public string CurrentDurationDisplay => FormatTimeSpan(CurrentDuration);
    public double PlaybackProgress => CurrentDuration.TotalSeconds > 0 
        ? (CurrentPosition.TotalSeconds / CurrentDuration.TotalSeconds) * 100 
        : 0;

    // Constructor for DI
    public AudioViewModel(IAudioService audioService, ILogger<AudioViewModel> logger, IAppState appState)
    {
        _audioService = audioService ?? throw new ArgumentNullException(nameof(audioService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _appState = appState ?? throw new ArgumentNullException(nameof(appState));
        
        Items = new ObservableCollection<AudioItem>();
        
        // Initialize commands
        ImportCommand = new AsyncRelayCommand(ImportAsync);
        RecordCommand = new AsyncRelayCommand(RecordAsync);
        PlayPauseCommand = new AsyncRelayCommand<AudioItem>(PlayPauseAsync);
        StopCommand = new RelayCommand(Stop, () => IsPlaying);
        DeleteCommand = new AsyncRelayCommand<AudioItem>(DeleteAsync);
        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        
        // Setup playback timer
        _playbackTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        _playbackTimer.Tick += OnPlaybackTimerTick;
        
        // Load initial data
        _ = RefreshAsync();
    }

    // Design-time constructor
    public AudioViewModel() : this(
        new DesignAudioService(),
        Microsoft.Extensions.Logging.Abstractions.NullLogger<AudioViewModel>.Instance,
        new DesignAppState())
    {
        // Add sample data for designer
        Items.Add(new AudioItem
        {
            Id = Guid.NewGuid(),
            FileName = "sample_audio.mp3",
            FilePath = @"C:\Audio\sample_audio.mp3",
            Duration = TimeSpan.FromMinutes(3.5),
            FileSize = 5242880, // 5 MB
            Format = "MP3",
            SampleRate = 44100,
            LastModified = DateTime.Now
        });
        TotalFiles = 1;
        TotalSizeDisplay = "5.0 MB";
    }

    // Public Methods
    public async Task RefreshAsync()
    {
        try
        {
            StatusText = "Loading audio files...";
            
            var files = await _audioService.GetAudioFilesAsync();
            
            Items.Clear();
            foreach (var file in files)
            {
                Items.Add(file);
            }
            
            TotalFiles = Items.Count;
            var totalBytes = Items.Sum(f => f.FileSize);
            TotalSizeDisplay = FormatFileSize(totalBytes);
            
            OnPropertyChanged(nameof(HasItems));
            StatusText = $"Ready - {TotalFiles} files";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh audio files");
            StatusText = "Error loading files";
        }
    }

    // Private Methods
    private async Task ImportAsync()
    {
        try
        {
            var dialog = new OpenFileDialog
            {
                Title = "Import Audio Files",
                Filter = "Audio Files|*.mp3;*.wav;*.flac;*.ogg;*.m4a;*.wma;*.aac|All Files|*.*",
                Multiselect = true
            };
            
            if (dialog.ShowDialog() == true)
            {
                StatusText = "Importing files...";
                
                foreach (var file in dialog.FileNames)
                {
                    await _audioService.ImportFileAsync(file);
                }
                
                await RefreshAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import audio files");
            StatusText = "Import failed";
        }
    }

    private async Task RecordAsync()
    {
        try
        {
            StatusText = "Recording feature coming soon";
            await _audioService.StartRecordingAsync();
        }
        catch (NotImplementedException)
        {
            StatusText = "Recording not yet implemented";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start recording");
            StatusText = "Recording failed";
        }
    }

    private async Task PlayPauseAsync(AudioItem? item)
    {
        try
        {
            if (item == null) item = SelectedItem;
            if (item == null) return;
            
            if (IsPlaying)
            {
                await _audioService.PauseAsync();
                IsPlaying = false;
                _playbackTimer.Stop();
                StatusText = "Paused";
            }
            else
            {
                StatusText = $"Playing {item.FileName}";
                CurrentDuration = item.Duration;
                CurrentPosition = TimeSpan.Zero;
                
                await _audioService.PlayAsync(item.FilePath);
                IsPlaying = true;
                
                // Mark item as playing
                foreach (var audioItem in Items)
                {
                    audioItem.IsCurrentlyPlaying = audioItem == item;
                }
                
                _playbackTimer.Start();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to play/pause audio");
            StatusText = "Playback failed";
            IsPlaying = false;
            _playbackTimer.Stop();
        }
    }

    private void Stop()
    {
        try
        {
            _audioService.Stop();
            IsPlaying = false;
            _playbackTimer.Stop();
            CurrentPosition = TimeSpan.Zero;
            
            foreach (var item in Items)
            {
                item.IsCurrentlyPlaying = false;
            }
            
            StatusText = "Stopped";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop playback");
        }
    }

    private async Task DeleteAsync(AudioItem? item)
    {
        try
        {
            if (item == null) item = SelectedItem;
            if (item == null) return;
            
            if (IsPlaying && item.IsCurrentlyPlaying)
            {
                Stop();
            }
            
            await _audioService.DeleteFileAsync(item.FilePath);
            Items.Remove(item);
            
            TotalFiles = Items.Count;
            var totalBytes = Items.Sum(f => f.FileSize);
            TotalSizeDisplay = FormatFileSize(totalBytes);
            
            OnPropertyChanged(nameof(HasItems));
            StatusText = $"Deleted {item.FileName}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete audio file");
            StatusText = "Delete failed";
        }
    }

    private void OnPlaybackTimerTick(object? sender, EventArgs e)
    {
        try
        {
            var position = _audioService.GetPlaybackPosition();
            if (position.HasValue)
            {
                CurrentPosition = position.Value;
                
                // Check if playback completed
                if (CurrentPosition >= CurrentDuration)
                {
                    Stop();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update playback position");
        }
    }

    private static string FormatTimeSpan(TimeSpan ts)
    {
        if (ts.TotalHours >= 1)
            return $"{(int)ts.TotalHours}:{ts.Minutes:00}:{ts.Seconds:00}";
        return $"{(int)ts.TotalMinutes}:{ts.Seconds:00}";
    }

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.#} {sizes[order]}";
    }

    public void Dispose()
    {
        try
        {
            _playbackTimer?.Stop();
            _audioService?.Dispose();
            _cts?.Cancel();
            _cts?.Dispose();
        }
        catch { }
    }

    // INotifyPropertyChanged
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    // Command implementations
    private sealed class AsyncRelayCommand : ICommand
    {
        private readonly Func<Task> _execute;
        private bool _isExecuting;

        public AsyncRelayCommand(Func<Task> execute) => _execute = execute;

        public bool CanExecute(object? parameter) => !_isExecuting;

        public async void Execute(object? parameter)
        {
            if (_isExecuting) return;
            _isExecuting = true;
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            try { await _execute(); }
            finally 
            { 
                _isExecuting = false; 
                CanExecuteChanged?.Invoke(this, EventArgs.Empty); 
            }
        }

        public event EventHandler? CanExecuteChanged;
    }

    private sealed class AsyncRelayCommand<T> : ICommand
    {
        private readonly Func<T?, Task> _execute;
        private bool _isExecuting;

        public AsyncRelayCommand(Func<T?, Task> execute) => _execute = execute;

        public bool CanExecute(object? parameter) => !_isExecuting;

        public async void Execute(object? parameter)
        {
            if (_isExecuting) return;
            _isExecuting = true;
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            try { await _execute((T?)parameter); }
            finally 
            { 
                _isExecuting = false; 
                CanExecuteChanged?.Invoke(this, EventArgs.Empty); 
            }
        }

        public event EventHandler? CanExecuteChanged;
    }

    private sealed class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;
        public void Execute(object? parameter) => _execute();
        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}

// AudioItem model
public sealed class AudioItem : INotifyPropertyChanged
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public long FileSize { get; set; }
    public string Format { get; set; } = string.Empty;
    public int SampleRate { get; set; }
    public DateTime LastModified { get; set; }
    
    private bool _isCurrentlyPlaying;
    public bool IsCurrentlyPlaying
    {
        get => _isCurrentlyPlaying;
        set
        {
            if (_isCurrentlyPlaying != value)
            {
                _isCurrentlyPlaying = value;
                OnPropertyChanged();
            }
        }
    }
    
    // Display properties
    public string DurationDisplay => FormatDuration(Duration);
    public string SizeDisplay => FormatSize(FileSize);
    public string SampleRateDisplay => $"{SampleRate / 1000.0:0.#} kHz";
    public string LastModifiedDisplay => LastModified.ToString("yyyy-MM-dd HH:mm");
    
    private static string FormatDuration(TimeSpan ts)
    {
        if (ts.TotalHours >= 1)
            return $"{(int)ts.TotalHours}:{ts.Minutes:00}:{ts.Seconds:00}";
        return $"{(int)ts.TotalMinutes}:{ts.Seconds:00}";
    }
    
    private static string FormatSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.#} {sizes[order]}";
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

// Design-time services
internal sealed class DesignAudioService : IAudioService
{
    public Task<IReadOnlyList<AudioItem>> GetAudioFilesAsync() => Task.FromResult<IReadOnlyList<AudioItem>>(Array.Empty<AudioItem>());
    public Task ImportFileAsync(string filePath) => Task.CompletedTask;
    public Task<string> StartRecordingAsync() => throw new NotImplementedException();
    public Task StopRecordingAsync() => throw new NotImplementedException();
    public Task PlayAsync(string filePath) => Task.CompletedTask;
    public Task PauseAsync() => Task.CompletedTask;
    public void Stop() { }
    public TimeSpan? GetPlaybackPosition() => TimeSpan.Zero;
    public Task DeleteFileAsync(string filePath) => Task.CompletedTask;
    public void Dispose() { }
}

internal sealed class DesignAppState : IAppState
{
#pragma warning disable CS0414
    public event EventHandler? Changed = null;
    public event PropertyChangedEventHandler? PropertyChanged = null;
#pragma warning restore CS0414
    
    public string? LoadedModelPath { get; set; }
    public int? RunnerPid { get; set; }
    public int? RunnerPort { get; set; }
    public bool IsRunnerRunning { get; set; }
    public double? LoraScale { get; set; }
    public string? LoadedEmbedding { get; set; }
    public string? LoadedTokenizer { get; set; }
    public string? LoadedLora { get; set; }
    
    public T? GetValue<T>(string key) => default;
    public void SetValue<T>(string key, T value) { }
    public Task SaveAsync(CancellationToken ct = default) => Task.CompletedTask;
    public Task LoadAsync(CancellationToken ct = default) => Task.CompletedTask;
}