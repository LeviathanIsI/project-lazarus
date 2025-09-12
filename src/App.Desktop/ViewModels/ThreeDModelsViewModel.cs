using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Lazarus.Shared;
using Lazarus.Desktop.Services;

namespace Lazarus.Desktop.ViewModels;

public sealed class ThreeDModelsViewModel : ViewModelBase, IDisposable
{
    private readonly ILogger<ThreeDModelsViewModel> _logger;
    private readonly IOrchestratorClient _orchestratorClient;
    private FileSystemWatcher? _watchImport;

    // Supported extensions
    public static readonly string[] ModelExtensions = new[] { ".obj", ".fbx", ".gltf", ".glb", ".stl" };

    // Library root (Avatar assets)
    private static string LibraryRoot => LazarusPaths.AvatarAssets.Models3D;

    // ===== Stats =====
    private int _totalModels;
    public int TotalModels { get => _totalModels; private set => SetProperty(ref _totalModels, value); }

    private int _generatedToday;
    public int GeneratedToday { get => _generatedToday; private set => SetProperty(ref _generatedToday, value); }

    private string _storageUsedText = "0 MB";
    public string StorageUsedText { get => _storageUsedText; private set => SetProperty(ref _storageUsedText, value); }

    public bool IsRenderReady => _orchestratorClient.IsHealthy;
    public string RenderStatusText => IsRenderReady ? "Ready" : "Offline";

    // ===== Library =====
    public ObservableCollection<ModelItem> Models { get; } = new();
    public ICollectionView ModelsView { get; }

    private ModelItem? _selected;
    public ModelItem? Selected
    {
        get => _selected;
        set
        {
            if (SetProperty(ref _selected, value))
            {
                OnPropertyChanged(nameof(HasSelection));
                // Hook for preview loader (Helix / custom)
                _previewLoader?.Invoke(value?.FullPath);
            }
        }
    }
    public bool HasSelection => Selected != null;

    private string _search = "";
    public string Search
    {
        get => _search;
        set { if (SetProperty(ref _search, value)) ModelsView.Refresh(); }
    }

    private Func<string?, bool>? _previewLoader; // injected from the View (keeps MVVM, no code-behind logic leak)

    // ===== Commands =====
    public ICommand ImportModelsCommand { get; }
    public ICommand GenerateModelCommand { get; }
    public ICommand DeleteSelectedCommand { get; }
    public ICommand RevealInExplorerCommand { get; }
    public ICommand SortByNameCommand { get; }
    public ICommand SortByDateCommand { get; }
    public ICommand SortBySizeCommand { get; }

    public ThreeDModelsViewModel(ILogger<ThreeDModelsViewModel> logger, IOrchestratorClient orchestratorClient)
    {
        _logger = logger;
        _orchestratorClient = orchestratorClient;

        ImportModelsCommand = new RelayCommand(ImportModels);
        GenerateModelCommand = new RelayCommand(GenerateModelPlaceholder);
        DeleteSelectedCommand = new RelayCommand(DeleteSelected, () => HasSelection);
        RevealInExplorerCommand = new RelayCommand(RevealInExplorer, () => HasSelection);
        SortByNameCommand = new RelayCommand(() => ApplySort(nameof(ModelItem.Name)));
        SortByDateCommand = new RelayCommand(() => ApplySort(nameof(ModelItem.LastWrite)));
        SortBySizeCommand = new RelayCommand(() => ApplySort(nameof(ModelItem.SizeBytes)));

        ModelsView = CollectionViewSource.GetDefaultView(Models);
        ModelsView.Filter = Filter;
        ApplySort(nameof(ModelItem.LastWrite), descending: true);

        _orchestratorClient.HealthStatusChanged += (_, __) =>
        {
            OnPropertyChanged(nameof(IsRenderReady));
            OnPropertyChanged(nameof(RenderStatusText));
        };

        EnsureFolders();
        StartWatchers();
        ReloadLibraryAndStats();
    }

    // Let the View supply a preview loader (Helix, etc.) without breaking MVVM
    public void SetPreviewLoader(Func<string?, bool> loader) => _previewLoader = loader;

    private void EnsureFolders()
    {
        Directory.CreateDirectory(LibraryRoot);
    }

    private void StartWatchers()
    {
        try
        {
            _watchImport = NewWatcher(LibraryRoot);
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Failed to start watchers"); }
    }

    private FileSystemWatcher NewWatcher(string path)
    {
        var w = new FileSystemWatcher(path)
        {
            IncludeSubdirectories = true,
            EnableRaisingEvents = true
        };
        w.Created += (_, __) => DebouncedReload();
        w.Changed += (_, __) => DebouncedReload();
        w.Renamed += (_, __) => DebouncedReload();
        w.Deleted += (_, __) => DebouncedReload();
        return w;
    }

    private DateTime _lastReload = DateTime.MinValue;
    private void DebouncedReload()
    {
        var now = DateTime.Now;
        if ((now - _lastReload).TotalMilliseconds < 250) return;
        _lastReload = now;
        Application.Current.Dispatcher.Invoke(ReloadLibraryAndStats);
    }

    private void ReloadLibraryAndStats()
    {
        try
        {
            var allFiles =
                Directory.Exists(LibraryRoot)
                    ? Directory.EnumerateFiles(LibraryRoot, "*", SearchOption.AllDirectories)
                    : Enumerable.Empty<string>()
            .Where(f => ModelExtensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

            Models.Clear();
            foreach (var f in allFiles)
            {
                Models.Add(new ModelItem
                {
                    Name = Path.GetFileName(f),
                    FullPath = f,
                    Extension = Path.GetExtension(f),
                    SizeBytes = SafeGetLength(f),
                    LastWrite = SafeGetLastWriteTime(f),
                    Origin = ModelOrigin.Imported
                });
            }
            ModelsView.Refresh();

            TotalModels = Models.Count;
            var today = DateTime.Today;
            GeneratedToday = Models.Count(m => m.LastWrite.Date == today);
            var bytes = Models.Sum(m => m.SizeBytes);
            StorageUsedText = FormatBytes(bytes);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to reload 3D library/stats");
        }

        (DeleteSelectedCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (RevealInExplorerCommand as RelayCommand)?.RaiseCanExecuteChanged();
    }

    private bool Filter(object? o)
    {
        if (o is not ModelItem m) return false;
        if (string.IsNullOrWhiteSpace(Search)) return true;
        var s = Search.Trim();
        return m.Name.Contains(s, StringComparison.OrdinalIgnoreCase)
            || m.Extension.Contains(s, StringComparison.OrdinalIgnoreCase);
    }

    private void ApplySort(string property, bool descending = false)
    {
        using (ModelsView.DeferRefresh())
        {
            ModelsView.SortDescriptions.Clear();
            ModelsView.SortDescriptions.Add(new SortDescription(property, descending ? ListSortDirection.Descending : ListSortDirection.Ascending));
        }
    }

    private void ImportModels()
    {
        try
        {
            var dlg = new OpenFileDialog
            {
                Title = "Import 3D Models",
                Filter = "3D Models (*.obj;*.fbx;*.gltf;*.glb;*.stl)|*.obj;*.fbx;*.gltf;*.glb;*.stl|All files (*.*)|*.*",
                Multiselect = true
            };
            if (dlg.ShowDialog() != true) return;

            EnsureFolders();

            foreach (var src in dlg.FileNames)
            {
                try
                {
                    var name = Path.GetFileName(src);
                    var dest = EnsureUniquePath(Path.Combine(LibraryRoot, name));
                    File.Copy(src, dest, overwrite: false);
                }
                catch (Exception exFile)
                {
                    _logger.LogWarning(exFile, "Failed to import model {File}", src);
                }
            }

            ReloadLibraryAndStats();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Import models failed");
            MessageBox.Show($"Failed to import models: {ex.Message}", "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void GenerateModelPlaceholder()
    {
        MessageBox.Show("Model generation is not yet implemented.", "Coming Soon", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void DeleteSelected()
    {
        if (Selected == null) return;
        try
        {
            var path = Selected.FullPath;
            File.Delete(path);
            Selected = null;
            ReloadLibraryAndStats();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Delete failed for selected file");
            MessageBox.Show($"Could not delete:\n{ex.Message}", "Delete Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void RevealInExplorer()
    {
        if (Selected == null) return;
        try
        {
            var args = $"/e,/select,\"{Selected.FullPath}\"";
            System.Diagnostics.Process.Start("explorer.exe", args);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "RevealInExplorer failed");
        }
    }

    private static DateTime SafeGetLastWriteTime(string f) { try { return File.GetLastWriteTime(f); } catch { return DateTime.MinValue; } }
    private static long SafeGetLength(string f) { try { return new FileInfo(f).Length; } catch { return 0; } }

    private static string EnsureUniquePath(string path)
    {
        if (!File.Exists(path)) return path;
        var dir = Path.GetDirectoryName(path)!;
        var name = Path.GetFileNameWithoutExtension(path);
        var ext = Path.GetExtension(path);
        for (int i = 1; ; i++)
        {
            var candidate = Path.Combine(dir, $"{name} ({i}){ext}");
            if (!File.Exists(candidate)) return candidate;
        }
    }

    

    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1) { order++; len /= 1024; }
        return $"{len:0.#} {sizes[order]}";
    }

    protected override void OnDisposing()
    {
        try { _watchImport?.Dispose(); } catch { }
        
        base.OnDisposing();
    }
}

public enum ModelOrigin { Imported, Generated }

public sealed class ModelItem : ViewModelBase
{
    public string Name { get; init; } = "";
    public string FullPath { get; init; } = "";
    public string Extension { get; init; } = "";
    public long SizeBytes { get; init; }
    public DateTime LastWrite { get; init; }
    public ModelOrigin Origin { get; init; }
    public string SizeText => SizeBytes <= 0 ? "�" : FormatBytes(SizeBytes);

    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1) { order++; len /= 1024; }
        return $"{len:0.#} {sizes[order]}";
    }
}










