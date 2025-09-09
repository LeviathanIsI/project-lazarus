using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using Lazarus.Backend.Services;
using Lazarus.Data.Entities;
using Lazarus.Data.Repositories;
using Microsoft.Extensions.Logging;

namespace Lazarus.Desktop.ViewModels;

public sealed class ImageLabViewModel : ViewModelBase
{
    private readonly IImageService _imageService;
    private readonly IImageJobRepository _jobs;
    private readonly ILogger<ImageLabViewModel>? _logger;

    public ImageLabViewModel(IImageService imageService, IImageJobRepository jobs, ILogger<ImageLabViewModel> logger)
    {
        _imageService = imageService;
        _jobs = jobs;
        _logger = logger;

        ControlNets = new ObservableCollection<string>(ScanDir(Lazarus.Shared.LazarusPaths.GenAssets.ControlNet));
        StylePresets = new ObservableCollection<string>(ScanDir(Lazarus.Shared.LazarusPaths.GenAssets.StylePresets));
        Upscalers = new ObservableCollection<string>(ScanDir(Lazarus.Shared.LazarusPaths.GenAssets.Upscale));
        Vaes = new ObservableCollection<string>(ScanDir(Lazarus.Shared.LazarusPaths.GenAssets.Vae));

        GenerateCommand = new RelayCommand(async () => await GenerateAsync(), () => !IsGenerating);
        RandomizeSeedCommand = new RelayCommand(() => { if (!IsSeedLocked) Seed = _rng.Next(); }, () => !IsSeedLocked);
        LockSeedCommand = new RelayCommand(() => IsSeedLocked = !IsSeedLocked);

        _ = RefreshCountersAsync();
        _ = LoadLastPreviewAsync();
    }

    private static IEnumerable<string> ScanDir(string path)
    {
        try
        {
            if (!Directory.Exists(path)) return Array.Empty<string>();
            return Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories)
                .Where(p => !p.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase))
                .Select(f => Path.GetFileName(f))
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Select(n => n!)
                .OrderBy(n => n)
                .ToList();
        }
        catch { return Array.Empty<string>(); }
    }

    private readonly Random _rng = new();

    // Top stats
    public int TotalImages { get => _totalImages; private set => SetProperty(ref _totalImages, value); }
    private int _totalImages;
    public int GeneratedToday { get => _generatedToday; private set => SetProperty(ref _generatedToday, value); }
    private int _generatedToday;
    public double StorageUsedMb { get => _storageUsedMb; private set => SetProperty(ref _storageUsedMb, value); }
    private double _storageUsedMb;
    public string ProcessingStatus { get => _processingStatus; private set => SetProperty(ref _processingStatus, value); }
    private string _processingStatus = "Ready";

    // Inputs
    public string? Prompt { get => _prompt; set => SetProperty(ref _prompt, value); }
    private string? _prompt;
    public string? NegativePrompt { get => _negativePrompt; set => SetProperty(ref _negativePrompt, value); }
    private string? _negativePrompt;

    public string Mode { get => _mode; set => SetProperty(ref _mode, value); }
    private string _mode = "Txt2Img";

    public ObservableCollection<string> ControlNets { get; }
    public ObservableCollection<string> StylePresets { get; }
    public ObservableCollection<string> Upscalers { get; }
    public ObservableCollection<string> Vaes { get; }

    public string? SelectedControlNet { get => _selectedControlNet; set => SetProperty(ref _selectedControlNet, value); }
    private string? _selectedControlNet;
    public string? SelectedStylePreset { get => _selectedStylePreset; set => SetProperty(ref _selectedStylePreset, value); }
    private string? _selectedStylePreset;
    public string? SelectedUpscaler { get => _selectedUpscaler; set => SetProperty(ref _selectedUpscaler, value); }
    private string? _selectedUpscaler;
    public string? SelectedVAE { get => _selectedVae; set => SetProperty(ref _selectedVae, value); }
    private string? _selectedVae;

    public int? Seed { get => _seed; set => SetProperty(ref _seed, value); }
    private int? _seed;
    public bool IsSeedLocked { get => _isSeedLocked; set { if (SetProperty(ref _isSeedLocked, value)) RandomizeSeedCommand.RaiseCanExecuteChanged(); } }
    private bool _isSeedLocked;
    public int Steps { get => _steps; set => SetProperty(ref _steps, Math.Max(1, value)); }
    private int _steps = 30;
    public double CfgScale { get => _cfgScale; set => SetProperty(ref _cfgScale, Math.Max(0.0, value)); }
    private double _cfgScale = 7.0;
    public int Width { get => _width; set => SetProperty(ref _width, Math.Max(64, value)); }
    private int _width = 512;
    public int Height { get => _height; set => SetProperty(ref _height, Math.Max(64, value)); }
    private int _height = 512;

    public string? InitImagePath { get => _initImagePath; set => SetProperty(ref _initImagePath, value); }
    private string? _initImagePath;
    public string? MaskImagePath { get => _maskImagePath; set => SetProperty(ref _maskImagePath, value); }
    private string? _maskImagePath;
    public double? Strength { get => _strength; set => SetProperty(ref _strength, value); }
    private double? _strength;

    public ObservableCollection<ImageJob> JobHistory { get; } = new();

    public RelayCommand GenerateCommand { get; }
    public RelayCommand RandomizeSeedCommand { get; }
    public RelayCommand LockSeedCommand { get; }

    public string? PreviewImagePath { get => _previewPath; private set => SetProperty(ref _previewPath, value); }
    private string? _previewPath;
    public bool IsGenerating { get => _isGenerating; private set { if (SetProperty(ref _isGenerating, value)) GenerateCommand.RaiseCanExecuteChanged(); } }
    private bool _isGenerating;
    public string? JobLog { get => _jobLog; private set => SetProperty(ref _jobLog, value); }
    private string? _jobLog;

    private async Task GenerateAsync()
    {
        IsGenerating = true;
        ProcessingStatus = "Generating…";
        try
        {
            var job = new ImageJob
            {
                Prompt = Prompt,
                NegativePrompt = NegativePrompt,
                Mode = Mode,
                ControlNetPath = ResolvePath(Lazarus.Shared.LazarusPaths.GenAssets.ControlNet, SelectedControlNet),
                StylePresetPath = ResolvePath(Lazarus.Shared.LazarusPaths.GenAssets.StylePresets, SelectedStylePreset),
                UpscalerPath = ResolvePath(Lazarus.Shared.LazarusPaths.GenAssets.Upscale, SelectedUpscaler),
                VaePath = ResolvePath(Lazarus.Shared.LazarusPaths.GenAssets.Vae, SelectedVAE),
                Seed = Seed,
                Steps = Steps,
                CfgScale = CfgScale,
                Width = Width,
                Height = Height,
                SourceImagePath = InitImagePath,
                MaskImagePath = MaskImagePath,
                Strength = Strength
            };

            var output = await _imageService.GenerateAsync(job).ConfigureAwait(false);
            job.OutputPath = output;

            _jobs.Add(job);
            await _jobs.SaveChangesAsync().ConfigureAwait(false);

            await App.Current.Dispatcher.InvokeAsync(() =>
            {
                JobHistory.Insert(0, job);
                PreviewImagePath = output;
                JobLog = $"Generated: {Path.GetFileName(output)}";
            });

            await RefreshCountersAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "GenerateAsync failed");
            JobLog = ex.Message;
        }
        finally
        {
            ProcessingStatus = "Ready";
            IsGenerating = false;
        }
    }

    private static string? ResolvePath(string root, string? leaf)
    {
        if (string.IsNullOrWhiteSpace(leaf)) return null;
        var p = Path.Combine(root, leaf);
        return p;
    }

    private async Task RefreshCountersAsync()
    {
        try
        {
            var today = DateTime.UtcNow.Date;
            TotalImages = await _jobs.CountAsync(_ => true).ConfigureAwait(false);
            GeneratedToday = await _jobs.CountAsync(j => j.CreatedAt >= today && j.CreatedAt < today.AddDays(1)).ConfigureAwait(false);

            // Storage: sum output file sizes
            double bytes = 0;
            var all = await _jobs.GetAllAsync().ConfigureAwait(false);
            foreach (var j in all)
            {
                try { if (!string.IsNullOrWhiteSpace(j.OutputPath) && File.Exists(j.OutputPath)) bytes += new FileInfo(j.OutputPath).Length; } catch { }
            }
            StorageUsedMb = Math.Round(bytes / (1024.0 * 1024.0), 2);

            // History
            await App.Current.Dispatcher.InvokeAsync(() =>
            {
                JobHistory.Clear();
                foreach (var j in all.OrderByDescending(x => x.CreatedAt).Take(50)) JobHistory.Add(j);
            });
        }
        catch (Exception ex) { _logger?.LogWarning(ex, "RefreshCounters failed"); }
    }

    private Task LoadLastPreviewAsync()
    {
        try
        {
            var dir = Lazarus.Shared.LazarusPaths.UserContent.GeneratedOutput;
            if (Directory.Exists(dir))
            {
                var last = Directory.EnumerateFiles(dir, "*.png").OrderByDescending(f => f).FirstOrDefault();
                if (last != null) PreviewImagePath = last;
            }
        }
        catch { }
        return Task.CompletedTask;
    }
}
