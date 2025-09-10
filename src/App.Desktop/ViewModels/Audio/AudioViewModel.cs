using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Lazarus.Backend.Services.Audio;
using Lazarus.Desktop.Services;
using Lazarus.Shared.Contracts.Audio;
using Lazarus.Shared.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace Lazarus.Desktop.ViewModels.Audio;

public sealed class AudioViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly IAudioService _audioService;
    private readonly ISettingsService? _settings;
    private readonly ILogger<AudioViewModel> _logger;
    private readonly DispatcherTimer _positionTimer;
    private IPlaybackSession? _currentSession;
    private CancellationTokenSource? _loadCts;
    private bool _disposed;

    // Collections
    public ObservableCollection<AudioRowVm> Items { get; } = new();

    // Commands
    public ICommand ImportCommand { get; }
    public ICommand RecordCommand { get; }
    public ICommand PlayPauseCommand { get; }
    public ICommand StopCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand AnalyzeCommand { get; }
    public ICommand OpenInFolderCommand { get; }
    public ICommand RefreshCommand { get; }

    // Selection
    private AudioRowVm? _selectedItem;
    public AudioRowVm? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (_selectedItem != value)
            {
                _selectedItem = value;
                OnPropertyChanged();
                UpdateCommands();
            }
        }
    }

    // Status properties
    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        private set { _isBusy = value; OnPropertyChanged(); }
    }

    private string? _nowPlayingId;
    public string? NowPlayingId
    {
        get => _nowPlayingId;
        private set 
        { 
            _nowPlayingId = value; 
            OnPropertyChanged();
            UpdatePlayingStates();
        }
    }

    private TimeSpan _currentPosition;
    public TimeSpan CurrentPosition
    {
        get => _currentPosition;
        set { _currentPosition = value; OnPropertyChanged(); }
    }

    private TimeSpan _currentDuration;
    public TimeSpan CurrentDuration
    {
        get => _currentDuration;
        set { _currentDuration = value; OnPropertyChanged(); }
    }

    private string _transportState = "Stopped";
    public string TransportState
    {
        get => _transportState;
        private set { _transportState = value; OnPropertyChanged(); }
    }

    private float _volume = 1.0f;
    public float Volume
    {
        get => _volume;
        set 
        { 
            _volume = Math.Max(0, Math.Min(1, value));
            OnPropertyChanged();
            if (_currentSession != null)
                _currentSession.Volume = _volume;
            _ = _settings?.SetValueAsync("AudioVolume", _volume);
        }
    }

    public AudioViewModel(IAudioService audioService, ISettingsService settings, ILogger<AudioViewModel> logger)
    {
        _audioService = audioService;
        _settings = settings;
        _logger = logger;

        // Load saved volume
        _volume = _settings?.GetValue<float>("AudioVolume", 1.0f) ?? 1.0f;

        // Initialize commands
        ImportCommand = new AsyncRelayCommand(ImportAsync);
        RecordCommand = new AsyncRelayCommand(RecordAsync);
        PlayPauseCommand = new AsyncRelayCommand(PlayPauseAsync, () => SelectedItem != null);
        StopCommand = new RelayCommand(Stop, () => _currentSession != null);
        DeleteCommand = new AsyncRelayCommand(DeleteAsync, () => SelectedItem != null && NowPlayingId != SelectedItem.Id);
        AnalyzeCommand = new AsyncRelayCommand(AnalyzeAsync, () => SelectedItem != null);
        OpenInFolderCommand = new RelayCommand(OpenInFolder, () => SelectedItem != null);
        RefreshCommand = new AsyncRelayCommand(RefreshAsync);

        // Position update timer
        _positionTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        _positionTimer.Tick += OnPositionTimerTick;

        // Load initial data
        _ = RefreshAsync();
    }

    public async Task RefreshAsync()
    {
        _loadCts?.Cancel();
        _loadCts = new CancellationTokenSource();
        var ct = _loadCts.Token;

        try
        {
            IsBusy = true;
            var items = await _audioService.ListAsync(ct);
            
            Items.Clear();
            foreach (var item in items)
            {
                var row = new AudioRowVm(item);
                
                // Load waveform if analyzed
                if (item.HasAnalysis)
                {
                    _ = LoadWaveformAsync(row, ct);
                }
                
                Items.Add(row);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh audio list");
            ShowToast("Failed to load audio files", true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ImportAsync()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Import Audio Files",
            Filter = "Audio Files|*.wav;*.mp3;*.flac;*.m4a;*.ogg;*.wma;*.aac|All Files|*.*",
            Multiselect = true
        };

        // Remember last folder
        var lastFolder = _settings?.GetValue<string>("LastAudioImportFolder", string.Empty);
        if (!string.IsNullOrEmpty(lastFolder) && Directory.Exists(lastFolder))
            dialog.InitialDirectory = lastFolder;

        if (dialog.ShowDialog() != true) return;

        // Save folder
        var folder = Path.GetDirectoryName(dialog.FileNames.FirstOrDefault());
        if (!string.IsNullOrEmpty(folder))
            _ = _settings?.SetValueAsync("LastAudioImportFolder", folder);

        await ImportFilesAsync(dialog.FileNames);
    }
    
    public async Task ImportAsync(string filePath)
    {
        await ImportFilesAsync(new[] { filePath });
    }
    
    private async Task ImportFilesAsync(string[] files)
    {
        try
        {
            IsBusy = true;
            int imported = 0;
            
            foreach (var file in files)
            {
                var item = await _audioService.ImportAsync(file);
                if (item != null)
                {
                    imported++;
                    
                    // Add or update in list
                    var existing = Items.FirstOrDefault(i => i.Id == item.Id);
                    if (existing != null)
                    {
                        existing.UpdateFrom(item);
                    }
                    else
                    {
                        Items.Insert(0, new AudioRowVm(item));
                    }
                }
            }
            
            ShowToast($"Imported {imported} file(s)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Import failed");
            ShowToast("Import failed", true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task RecordAsync()
    {
        ShowToast("Recording feature coming soon");
        _logger.LogInformation("Record command invoked (stubbed)");
        await Task.CompletedTask;
    }

    private async Task PlayPauseAsync()
    {
        if (SelectedItem == null) return;

        try
        {
            // If playing this item, toggle pause
            if (_currentSession != null && NowPlayingId == SelectedItem.Id)
            {
                if (_currentSession.State == PlaybackState.Playing)
                {
                    _currentSession.Pause();
                    TransportState = "Paused";
                    _positionTimer.Stop();
                }
                else
                {
                    _currentSession.Resume();
                    TransportState = "Playing";
                    _positionTimer.Start();
                }
            }
            else
            {
                // Stop current and start new
                Stop();
                
                _currentSession = _audioService.Play(SelectedItem.Id);
                _currentSession.Volume = Volume;
                _currentSession.StateChanged += OnSessionStateChanged;
                _currentSession.PositionChanged += OnSessionPositionChanged;
                
                NowPlayingId = SelectedItem.Id;
                CurrentDuration = _currentSession.Duration;
                CurrentPosition = TimeSpan.Zero;
                TransportState = "Playing";
                _positionTimer.Start();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Playback failed");
            ShowToast("Playback failed", true);
            Stop();
        }
        
        await Task.CompletedTask;
    }

    private void Stop()
    {
        _positionTimer.Stop();
        _currentSession?.Dispose();
        _currentSession = null;
        NowPlayingId = null;
        CurrentPosition = TimeSpan.Zero;
        CurrentDuration = TimeSpan.Zero;
        TransportState = "Stopped";
        UpdateCommands();
    }

    private async Task DeleteAsync()
    {
        if (SelectedItem == null) return;

        var result = MessageBox.Show(
            $"Delete '{SelectedItem.FileName}'?",
            "Confirm Delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        
        if (result != MessageBoxResult.Yes) return;

        try
        {
            IsBusy = true;
            await _audioService.DeleteAsync(SelectedItem.Id);
            Items.Remove(SelectedItem);
            SelectedItem = null;
            ShowToast("File deleted");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delete failed");
            ShowToast("Delete failed", true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [SupportedOSPlatform("windows")]
    private async Task AnalyzeAsync()
    {
        if (SelectedItem == null) return;

        try
        {
            IsBusy = true;
            var analysis = await _audioService.AnalyzeAsync(SelectedItem.Id, force: true);
            
            // Update item
            SelectedItem.HasAnalysis = true;
            
            // Load waveforms
            if (analysis.WaveformSmall != null)
            {
                SelectedItem.WaveformSmall = LoadBitmapFromBytes(analysis.WaveformSmall);
            }
            if (analysis.WaveformLarge != null)
            {
                SelectedItem.WaveformLarge = LoadBitmapFromBytes(analysis.WaveformLarge);
            }
            
            ShowToast("Analysis complete");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Analysis failed");
            ShowToast("Analysis failed", true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void OpenInFolder()
    {
        if (SelectedItem?.FilePath == null) return;
        
        try
        {
            Process.Start("explorer.exe", $"/select,\"{SelectedItem.FilePath}\"");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open folder");
        }
    }

    [SupportedOSPlatform("windows")]
    private async Task LoadWaveformAsync(AudioRowVm row, CancellationToken ct)
    {
        try
        {
            var analysis = await _audioService.AnalyzeAsync(row.Id, force: false, ct);
            
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                if (analysis.WaveformSmall != null)
                    row.WaveformSmall = LoadBitmapFromBytes(analysis.WaveformSmall);
                if (analysis.WaveformLarge != null)
                    row.WaveformLarge = LoadBitmapFromBytes(analysis.WaveformLarge);
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load waveform for {Id}", row.Id);
        }
    }

    private BitmapImage? LoadBitmapFromBytes(byte[] bytes)
    {
        if (bytes == null || bytes.Length == 0) return null;
        
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.StreamSource = new MemoryStream(bytes);
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }

    private void OnSessionStateChanged(object? sender, PlaybackStateChangedEventArgs e)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            TransportState = e.NewState.ToString();
            if (e.NewState == PlaybackState.Stopped)
            {
                Stop();
            }
        });
    }

    private void OnSessionPositionChanged(object? sender, TimeSpan position)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            CurrentPosition = position;
        });
    }

    private void OnPositionTimerTick(object? sender, EventArgs e)
    {
        if (_currentSession != null)
        {
            CurrentPosition = _currentSession.Position;
        }
    }

    private void UpdatePlayingStates()
    {
        foreach (var item in Items)
        {
            item.IsPlaying = item.Id == NowPlayingId;
        }
    }

    private void UpdateCommands()
    {
        CommandManager.InvalidateRequerySuggested();
    }

    private void ShowToast(string message, bool isError = false)
    {
        _logger.Log(isError ? LogLevel.Error : LogLevel.Information, message);
        // Toast implementation would go here
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        
        _loadCts?.Cancel();
        _positionTimer?.Stop();
        Stop();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    // Command implementations
    private sealed class AsyncRelayCommand : ICommand
    {
        private readonly Func<Task> _execute;
        private readonly Func<bool>? _canExecute;
        private bool _isExecuting;

        public AsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => !_isExecuting && (_canExecute?.Invoke() ?? true);

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

// Row view model
public sealed class AudioRowVm : INotifyPropertyChanged
{
    public string Id { get; }
    public string FileName { get; }
    public string FilePath { get; }
    public long FileSize { get; }
    public TimeSpan Duration { get; private set; }
    public int SampleRate { get; private set; }
    public int Channels { get; private set; }
    public int Bitrate { get; private set; }
    public DateTime ModifiedUtc { get; }
    
    private bool _hasAnalysis;
    public bool HasAnalysis
    {
        get => _hasAnalysis;
        set { _hasAnalysis = value; OnPropertyChanged(); }
    }

    private bool _isPlaying;
    public bool IsPlaying
    {
        get => _isPlaying;
        set { _isPlaying = value; OnPropertyChanged(); }
    }

    private BitmapImage? _waveformSmall;
    public BitmapImage? WaveformSmall
    {
        get => _waveformSmall;
        set { _waveformSmall = value; OnPropertyChanged(); }
    }

    private BitmapImage? _waveformLarge;
    public BitmapImage? WaveformLarge
    {
        get => _waveformLarge;
        set { _waveformLarge = value; OnPropertyChanged(); }
    }

    // Display properties
    public string DurationDisplay => Duration.TotalHours >= 1 
        ? $"{(int)Duration.TotalHours}:{Duration.Minutes:00}:{Duration.Seconds:00}"
        : $"{(int)Duration.TotalMinutes}:{Duration.Seconds:00}";
    
    public string SizeDisplay
    {
        get
        {
            double size = FileSize;
            string[] units = { "B", "KB", "MB", "GB" };
            int unit = 0;
            while (size >= 1024 && unit < units.Length - 1)
            {
                size /= 1024;
                unit++;
            }
            return $"{size:0.#} {units[unit]}";
        }
    }

    public string ModifiedDisplay => ModifiedUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm");

    public AudioRowVm(AudioItem item)
    {
        Id = item.Id;
        FileName = item.FileName;
        FilePath = item.FilePath;
        FileSize = item.FileSize;
        Duration = item.Duration;
        SampleRate = item.SampleRate;
        Channels = item.Channels;
        Bitrate = item.Bitrate;
        ModifiedUtc = item.ModifiedUtc;
        HasAnalysis = item.HasAnalysis;
    }

    public void UpdateFrom(AudioItem item)
    {
        Duration = item.Duration;
        SampleRate = item.SampleRate;
        Channels = item.Channels;
        Bitrate = item.Bitrate;
        HasAnalysis = item.HasAnalysis;
        OnPropertyChanged(nameof(DurationDisplay));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}