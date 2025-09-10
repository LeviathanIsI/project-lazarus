using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Lazarus.Desktop.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace Lazarus.Desktop.ViewModels;

public sealed class AudioViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly IAudioService _audio;
    private readonly ILogger<AudioViewModel> _log;
    private CancellationTokenSource? _cts;

    public ObservableCollection<AudioRow> Items { get; } = new();

    private int _totalFiles;
    public int TotalFiles { get => _totalFiles; set { if (_totalFiles != value) { _totalFiles = value; OnPropertyChanged(); } } }

    private int _generatedToday;
    public int GeneratedToday { get => _generatedToday; set { if (_generatedToday != value) { _generatedToday = value; OnPropertyChanged(); } } }

    private string _totalDurationDisplay = "0:00:00";
    public string TotalDurationDisplay { get => _totalDurationDisplay; set { if (_totalDurationDisplay != value) { _totalDurationDisplay = value; OnPropertyChanged(); } } }

    private string _synthesisStatus = "Checking";
    public string SynthesisStatus { get => _synthesisStatus; set { if (_synthesisStatus != value) { _synthesisStatus = value; OnPropertyChanged(); } } }

    public ICommand ImportAudioCommand { get; }
    public ICommand GenerateAudioCommand { get; }
    public ICommand RefreshStatsCommand { get; }
    public ICommand OpenInExplorerCommand { get; }
    public ICommand DeleteItemCommand { get; }

    public AudioViewModel(IAudioService audio, ILogger<AudioViewModel> log)
    {
        _audio = audio ?? throw new ArgumentNullException(nameof(audio));
        _log = log ?? throw new ArgumentNullException(nameof(log));

        ImportAudioCommand = new AsyncRelayCommand(ImportAsync);
        GenerateAudioCommand = new AsyncRelayCommand(GenerateAsync);
        RefreshStatsCommand = new AsyncRelayCommand(LoadAsync);
        OpenInExplorerCommand = new RelayCommand<string?>(OpenInExplorer);
        DeleteItemCommand = new RelayCommand<string?>(DeleteItem);

        _ = LoadAsync();
    }

    // PUBLIC API
    public async Task LoadAsync()
    {
        CancelPending();
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        try
        {
            SynthesisStatus = await _audio.IsSynthesisReadyAsync(ct) ? "Ready" : "Not Ready";

            // Refresh list + stats
            Items.Clear();
            var stats = await _audio.GetStatsAsync(ct);
            TotalFiles = stats.TotalFiles;
            GeneratedToday = stats.GeneratedToday;
            TotalDurationDisplay = FormatSpan(stats.TotalDuration);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Failed to load audio stats");
            SynthesisStatus = "Error";
        }
    }

    private async Task ImportAsync()
    {
        var ofd = new OpenFileDialog
        {
            Title = "Import Audio",
            Filter = "Audio Files|*.wav;*.mp3;*.flac;*.ogg;*.m4a|All Files|*.*",
            Multiselect = true
        };
        if (ofd.ShowDialog() != true) return;

        CancelPending();
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        try
        {
            var added = await _audio.ImportAsync(ofd.FileNames, ct);
            foreach (var a in added)
            {
                Items.Add(AudioRow.From(a));
            }
            await LoadAsync();
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Import failed");
            MessageBox.Show("Import failed. See logs.", "Audio", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task GenerateAsync()
    {
        CancelPending();
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        try
        {
            // Minimal MVP request  later wire voice/model/runner picks from UI
            var req = new AudioGenRequest(Text: "Hello from Lazarus", Voice: "en_US-amy", SampleRate: 22050);
            var item = await _audio.GenerateAsync(req, ct);
            if (item != null)
            {
                Items.Insert(0, AudioRow.From(item));
                await LoadAsync();
            }
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Generate failed");
            MessageBox.Show("Audio generation failed. See logs.", "Audio", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static string FormatSpan(TimeSpan ts)
        => $"{(int)ts.TotalHours}:{ts.Minutes:00}:{ts.Seconds:00}";

    private void OpenInExplorer(string? path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            if (File.Exists(path))
                System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{path}\"");
            else if (Directory.Exists(path))
                System.Diagnostics.Process.Start("explorer.exe", $"\"{path}\"");
        }
        catch { /* ignore */ }
    }

    private void DeleteItem(string? path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return;
            File.Delete(path);
            for (int i = Items.Count - 1; i >= 0; i--)
            {
                if (string.Equals(Items[i].FilePath, path, StringComparison.OrdinalIgnoreCase))
                    Items.RemoveAt(i);
            }
            _ = LoadAsync();
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Delete failed");
        }
    }

    private void CancelPending()
    {
        try { _cts?.Cancel(); } catch { /* ignore */ }
        _cts?.Dispose();
        _cts = null;
    }

    public void Dispose() => CancelPending();

    // Simple row VM
    public sealed class AudioRow
    {
        public string Id { get; init; } = string.Empty;
        public string FileName { get; init; } = string.Empty;
        public string FilePath { get; init; } = string.Empty;
        public string Duration { get; init; } = "0:00";
        public string CreatedLocal { get; init; } = string.Empty;
        public bool IsGenerated { get; init; }

        public static AudioRow From(AudioItem a) => new()
        {
            Id = a.Id,
            FileName = a.FileName,
            FilePath = a.FilePath,
            Duration = $"{(int)a.Duration.TotalMinutes}:{a.Duration.Seconds:00}",
            CreatedLocal = a.CreatedUtc.ToLocalTime().ToString("g", CultureInfo.CurrentCulture),
            IsGenerated = a.IsGenerated
        };
    }

    // Lightweight RelayCommand impls (local to avoid dependencies)
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private sealed class AsyncRelayCommand : ICommand
    {
        private readonly Func<Task> _exec;
        private bool _busy;
        public AsyncRelayCommand(Func<Task> exec) => _exec = exec;
        public bool CanExecute(object? parameter) => !_busy;
        public async void Execute(object? parameter)
        {
            if (_busy) return;
            _busy = true; CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            try { await _exec(); } finally { _busy = false; CanExecuteChanged?.Invoke(this, EventArgs.Empty); }
        }
        public event EventHandler? CanExecuteChanged;
    }
    private sealed class RelayCommand<T> : ICommand
    {
        private readonly Action<T?> _exec;
        public RelayCommand(Action<T?> exec) => _exec = exec;
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => _exec((T?)parameter);
        public event EventHandler? CanExecuteChanged { add { } remove { } }
    }
}

