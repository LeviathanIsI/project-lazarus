using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Lazarus.Shared;
using Lazarus.Desktop.Services;

namespace Lazarus.Desktop.ViewModels;

public sealed class ThreeDModelsViewModel : ViewModelBase
{
    private readonly ILogger<ThreeDModelsViewModel> _logger;
    private readonly IOrchestratorClient _orchestratorClient;

    private int _totalModels;
    public int TotalModels { get => _totalModels; private set => SetProperty(ref _totalModels, value); }

    private int _generatedToday;
    public int GeneratedToday { get => _generatedToday; private set => SetProperty(ref _generatedToday, value); }

    private string _storageUsedText = "0 MB";
    public string StorageUsedText { get => _storageUsedText; private set => SetProperty(ref _storageUsedText, value); }

    public bool IsRenderReady => _orchestratorClient.IsHealthy;
    public string RenderStatusText => IsRenderReady ? "Ready" : "Offline";

    public ICommand ImportModelsCommand { get; }
    public ICommand GenerateModelCommand { get; }

    private static readonly string[] ModelExtensions = new[] { ".obj", ".fbx", ".gltf", ".glb", ".stl" };

    // Import destination (under shared Import-Export)
    private static string ImportRoot => System.IO.Path.Combine(LazarusPaths.SharedResources.ImportExport, "Import", "3D-Models");
    // Generated output placeholder (user content)
    private static string GeneratedRoot => System.IO.Path.Combine(LazarusPaths.UserContent.GeneratedOutput, "3D-Models");

    public ThreeDModelsViewModel(ILogger<ThreeDModelsViewModel> logger, IOrchestratorClient orchestratorClient)
    {
        _logger = logger;
        _orchestratorClient = orchestratorClient;

        ImportModelsCommand = new RelayCommand(ImportModels);
        GenerateModelCommand = new RelayCommand(GenerateModelPlaceholder);

        // Track orchestrator health for status pill
        _orchestratorClient.HealthStatusChanged += (_, __) =>
        {
            OnPropertyChanged(nameof(IsRenderReady));
            OnPropertyChanged(nameof(RenderStatusText));
        };

        EnsureFolders();
        RefreshStats();
    }

    private void EnsureFolders()
    {
        try { Directory.CreateDirectory(ImportRoot); } catch { }
        try { Directory.CreateDirectory(GeneratedRoot); } catch { }
    }

    private void RefreshStats()
    {
        try
        {
            var files = Directory.Exists(ImportRoot)
                ? Directory.EnumerateFiles(ImportRoot, "*", SearchOption.AllDirectories)
                    .Where(f => ModelExtensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
                    .ToList()
                : new System.Collections.Generic.List<string>();

            TotalModels = files.Count;
            var today = DateTime.Today;
            GeneratedToday = files.Count(f => SafeGetLastWriteTime(f).Date == today);
            var bytes = files.Sum(f => SafeGetLength(f));
            StorageUsedText = FormatBytes(bytes);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to refresh 3D models stats");
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
            if (dlg.ShowDialog() != true)
                return;

            EnsureFolders();

            foreach (var src in dlg.FileNames)
            {
                try
                {
                    var name = Path.GetFileName(src);
                    var dest = Path.Combine(ImportRoot, name);
                    // Avoid overwriting by adding suffix if needed
                    dest = EnsureUniquePath(dest);
                    File.Copy(src, dest, overwrite: false);
                }
                catch (Exception exFile)
                {
                    _logger.LogWarning(exFile, "Failed to import model {File}", src);
                }
            }

            RefreshStats();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Import models failed");
            MessageBox.Show($"Failed to import models: {ex.Message}", "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void GenerateModelPlaceholder()
    {
        try
        {
            // Placeholder: create a stub file to simulate generation output
            EnsureFolders();
            var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var outPath = Path.Combine(GeneratedRoot, $"generated_{stamp}.obj");
            File.WriteAllText(outPath, "# Lazarus 3D placeholder OBJ\n# TODO: Integrate backend runner");

            // Also drop it in the import folder to make it visible in stats for now
            var imported = Path.Combine(ImportRoot, Path.GetFileName(outPath));
            File.Copy(outPath, EnsureUniquePath(imported), overwrite: false);

            _logger.LogInformation("Generated placeholder 3D model at {Path}", outPath);
            RefreshStats();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Generate model placeholder failed");
            MessageBox.Show($"Failed to generate model: {ex.Message}", "Generation Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static DateTime SafeGetLastWriteTime(string file)
    {
        try { return File.GetLastWriteTime(file); } catch { return DateTime.MinValue; }
    }

    private static long SafeGetLength(string file)
    {
        try { return new FileInfo(file).Length; } catch { return 0; }
    }

    private static string EnsureUniquePath(string path)
    {
        if (!File.Exists(path)) return path;
        var dir = Path.GetDirectoryName(path)!;
        var name = Path.GetFileNameWithoutExtension(path);
        var ext = Path.GetExtension(path);
        int i = 1;
        string candidate;
        do { candidate = Path.Combine(dir, $"{name} ({i++}){ext}"); } while (File.Exists(candidate));
        return candidate;
    }

    private static string FormatBytes(long bytes)
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
}
