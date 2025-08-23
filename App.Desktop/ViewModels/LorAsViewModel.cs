using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Lazarus.Desktop.Helpers;
using Lazarus.Shared.Models;
using System.IO;
using System.Text.Json;

namespace Lazarus.Desktop.ViewModels;

public class LorAsViewModel : INotifyPropertyChanged
{
    private bool _isLoading;
    private string _statusText = "Ready to discover LoRAs...";
    private LoRADto? _selectedLoRA;
    private string _searchFilter = "";
    private LoRACategory _selectedCategory = LoRACategory.All;
    private SortOrder _sortOrder = SortOrder.Name;

    public LorAsViewModel()
    {
        AvailableLoRAs = new ObservableCollection<LoRADto>();
        AppliedLoRAs = new ObservableCollection<AppliedLoRADto>();
        Categories = new ObservableCollection<LoRACategory>
        {
            LoRACategory.All,
            LoRACategory.Style,
            LoRACategory.Character,
            LoRACategory.Concept,
            LoRACategory.Clothing,
            LoRACategory.Background,
            LoRACategory.Pose,
            LoRACategory.Other
        };

        // Commands - the digital rituals
        ScanLoRAsCommand = new RelayCommand(async _ => await ScanLoRAsAsync(), _ => !IsLoading);
        ApplyLoRACommand = new RelayCommand(async lora => await ApplyLoRAAsync((LoRADto)lora!),
            lora => !IsLoading && lora is LoRADto);
        RemoveLoRACommand = new RelayCommand(async applied => await RemoveLoRAAsync((AppliedLoRADto)applied!),
            applied => applied is AppliedLoRADto);
        ClearAllLoRAsCommand = new RelayCommand(async _ => await ClearAllLoRAsAsync(), _ => AppliedLoRAs.Any());
        RefreshCommand = new RelayCommand(async _ => await ScanLoRAsAsync(), _ => !IsLoading);
        ImportLoRACommand = new RelayCommand(async _ => await ImportLoRAAsync(), _ => !IsLoading);

        // Auto-scan on startup
        _ = ScanLoRAsAsync();
    }

    #region Properties

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public LoRADto? SelectedLoRA
    {
        get => _selectedLoRA;
        set => SetProperty(ref _selectedLoRA, value);
    }

    public string SearchFilter
    {
        get => _searchFilter;
        set
        {
            if (SetProperty(ref _searchFilter, value))
            {
                FilterLoRAs();
            }
        }
    }

    public LoRACategory SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (SetProperty(ref _selectedCategory, value))
            {
                FilterLoRAs();
            }
        }
    }

    public SortOrder SortOrder
    {
        get => _sortOrder;
        set
        {
            if (SetProperty(ref _sortOrder, value))
            {
                SortLoRAs();
            }
        }
    }

    public ObservableCollection<LoRADto> AvailableLoRAs { get; }
    public ObservableCollection<AppliedLoRADto> AppliedLoRAs { get; }
    public ObservableCollection<LoRACategory> Categories { get; }

    public int TotalLoRAsCount => AvailableLoRAs?.Count ?? 0;
    public int AppliedLoRAsCount => AppliedLoRAs?.Count ?? 0;
    public float TotalWeight => AppliedLoRAs?.Sum(l => l.Weight) ?? 0.0f;

    #endregion

    #region Commands

    public ICommand ScanLoRAsCommand { get; }
    public ICommand ApplyLoRACommand { get; }
    public ICommand RemoveLoRACommand { get; }
    public ICommand ClearAllLoRAsCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand ImportLoRACommand { get; }

    #endregion

    #region Methods

    private async Task ScanLoRAsAsync()
    {
        try
        {
            IsLoading = true;
            StatusText = "Scanning for LoRA artifacts...";

            var lorasPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "loras");
            var discoveredLoRAs = new List<LoRADto>();

            if (Directory.Exists(lorasPath))
            {
                // Scan for .safetensors and .ckpt files
                var loraFiles = Directory.GetFiles(lorasPath, "*.*", SearchOption.AllDirectories)
                    .Where(f => f.EndsWith(".safetensors", StringComparison.OrdinalIgnoreCase) ||
                               f.EndsWith(".ckpt", StringComparison.OrdinalIgnoreCase) ||
                               f.EndsWith(".pt", StringComparison.OrdinalIgnoreCase))
                    .ToArray();

                StatusText = $"Found {loraFiles.Length} potential LoRA files...";

                foreach (var file in loraFiles)
                {
                    var lora = await AnalyzeLoRAFileAsync(file);
                    if (lora != null)
                    {
                        discoveredLoRAs.Add(lora);
                    }
                }
            }
            else
            {
                Directory.CreateDirectory(lorasPath);
                StatusText = "Created LoRAs directory - add your .safetensors files here";
            }

            // Update UI collection
            AvailableLoRAs.Clear();
            foreach (var lora in discoveredLoRAs.OrderBy(l => l.Name))
            {
                AvailableLoRAs.Add(lora);
            }

            StatusText = $"Discovered {discoveredLoRAs.Count} LoRA artifacts";
            OnPropertyChanged(nameof(TotalLoRAsCount));
        }
        catch (Exception ex)
        {
            StatusText = $"LoRA scanning ritual failed: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task<LoRADto?> AnalyzeLoRAFileAsync(string filePath)
    {
        try
        {
            var fileInfo = new FileInfo(filePath);
            var fileName = Path.GetFileNameWithoutExtension(fileInfo.Name);

            // Try to load metadata from companion .json file
            var metadataPath = Path.ChangeExtension(filePath, ".json");
            LoRAMetadata? metadata = null;

            if (File.Exists(metadataPath))
            {
                var json = await File.ReadAllTextAsync(metadataPath);
                metadata = JsonSerializer.Deserialize<LoRAMetadata>(json);
            }

            var lora = new LoRADto
            {
                Id = Guid.NewGuid().ToString(),
                Name = metadata?.Name ?? fileName,
                FileName = fileInfo.Name,
                FilePath = filePath,
                FileSize = fileInfo.Length,
                Description = metadata?.Description ?? "No description available",
                Category = metadata?.Category ?? InferCategoryFromName(fileName),
                Tags = metadata?.Tags ?? InferTagsFromName(fileName),
                TriggerWords = metadata?.TriggerWords ?? new List<string>(),
                PreviewImagePath = FindPreviewImage(filePath),
                BaseModel = metadata?.BaseModel ?? "Unknown",
                Version = metadata?.Version ?? "1.0",
                Author = metadata?.Author ?? "Unknown",
                CreatedDate = fileInfo.CreationTime,
                ModifiedDate = fileInfo.LastWriteTime,
                IsLoaded = false,
                RecommendedWeight = metadata?.RecommendedWeight ?? 0.8f
            };

            return lora;
        }
        catch (Exception ex)
        {
            StatusText = $"Failed to analyze {Path.GetFileName(filePath)}: {ex.Message}";
            return null;
        }
    }

    private async Task ApplyLoRAAsync(LoRADto lora)
    {
        try
        {
            StatusText = $"Applying LoRA: {lora.Name}...";

            // Check if already applied
            if (AppliedLoRAs.Any(a => a.LoRAId == lora.Id))
            {
                StatusText = $"{lora.Name} is already applied";
                return;
            }

            var appliedLora = new AppliedLoRADto
            {
                Id = Guid.NewGuid().ToString(),
                LoRAId = lora.Id,
                Name = lora.Name,
                FilePath = lora.FilePath,
                Weight = lora.RecommendedWeight,
                IsEnabled = true,
                Order = AppliedLoRAs.Count + 1
            };

            AppliedLoRAs.Add(appliedLora);
            lora.IsLoaded = true;

            StatusText = $"Applied {lora.Name} with weight {lora.RecommendedWeight:F2}";
            OnPropertyChanged(nameof(AppliedLoRAsCount));
            OnPropertyChanged(nameof(TotalWeight));

            // TODO: Actually apply to the model via API
            // await ApiClient.ApplyLoRAAsync(appliedLora);
        }
        catch (Exception ex)
        {
            StatusText = $"Failed to apply LoRA: {ex.Message}";
        }
    }

    private async Task RemoveLoRAAsync(AppliedLoRADto appliedLora)
    {
        try
        {
            AppliedLoRAs.Remove(appliedLora);

            // Update the original LoRA status
            var originalLora = AvailableLoRAs.FirstOrDefault(l => l.Id == appliedLora.LoRAId);
            if (originalLora != null)
            {
                originalLora.IsLoaded = false;
            }

            // Reorder remaining LoRAs
            for (int i = 0; i < AppliedLoRAs.Count; i++)
            {
                AppliedLoRAs[i].Order = i + 1;
            }

            StatusText = $"Removed {appliedLora.Name}";
            OnPropertyChanged(nameof(AppliedLoRAsCount));
            OnPropertyChanged(nameof(TotalWeight));

            // TODO: Remove from model via API
            // await ApiClient.RemoveLoRAAsync(appliedLora.Id);
        }
        catch (Exception ex)
        {
            StatusText = $"Failed to remove LoRA: {ex.Message}";
        }
    }

    private async Task ClearAllLoRAsAsync()
    {
        try
        {
            StatusText = "Clearing all applied LoRAs...";

            // Reset all loaded states
            foreach (var appliedLora in AppliedLoRAs)
            {
                var originalLora = AvailableLoRAs.FirstOrDefault(l => l.Id == appliedLora.LoRAId);
                if (originalLora != null)
                {
                    originalLora.IsLoaded = false;
                }
            }

            AppliedLoRAs.Clear();

            StatusText = "All LoRAs cleared";
            OnPropertyChanged(nameof(AppliedLoRAsCount));
            OnPropertyChanged(nameof(TotalWeight));

            // TODO: Clear all from model via API
            // await ApiClient.ClearAllLoRAsAsync();
        }
        catch (Exception ex)
        {
            StatusText = $"Failed to clear LoRAs: {ex.Message}";
        }
    }

    private async Task ImportLoRAAsync()
    {
        try
        {
            // TODO: Implement file dialog to import LoRA
            // For now, just rescan
            await ScanLoRAsAsync();
        }
        catch (Exception ex)
        {
            StatusText = $"Import failed: {ex.Message}";
        }
    }

    private void FilterLoRAs()
    {
        // TODO: Implement filtering logic if needed
        // For now, just update status
        var filterText = string.IsNullOrEmpty(SearchFilter) ? "all" : $"'{SearchFilter}'";
        var categoryText = SelectedCategory == LoRACategory.All ? "all categories" : SelectedCategory.ToString();

        StatusText = $"Showing {filterText} LoRAs in {categoryText}";
    }

    private void SortLoRAs()
    {
        // TODO: Implement sorting if using filtered collections
        StatusText = $"Sorted by {SortOrder}";
    }

    #endregion

    #region Helper Methods

    private LoRACategory InferCategoryFromName(string fileName)
    {
        var name = fileName.ToLowerInvariant();

        if (name.Contains("style") || name.Contains("art")) return LoRACategory.Style;
        if (name.Contains("char") || name.Contains("person") || name.Contains("face")) return LoRACategory.Character;
        if (name.Contains("cloth") || name.Contains("dress") || name.Contains("outfit")) return LoRACategory.Clothing;
        if (name.Contains("pose") || name.Contains("action")) return LoRACategory.Pose;
        if (name.Contains("background") || name.Contains("scene")) return LoRACategory.Background;
        if (name.Contains("concept") || name.Contains("idea")) return LoRACategory.Concept;

        return LoRACategory.Other;
    }

    private List<string> InferTagsFromName(string fileName)
    {
        var tags = new List<string>();
        var name = fileName.ToLowerInvariant();

        // Extract common tags from filename
        if (name.Contains("anime")) tags.Add("anime");
        if (name.Contains("realistic")) tags.Add("realistic");
        if (name.Contains("2d")) tags.Add("2d");
        if (name.Contains("3d")) tags.Add("3d");
        if (name.Contains("nsfw")) tags.Add("nsfw");
        if (name.Contains("sfw")) tags.Add("sfw");

        return tags;
    }

    private string? FindPreviewImage(string loraPath)
    {
        var directory = Path.GetDirectoryName(loraPath);
        var fileName = Path.GetFileNameWithoutExtension(loraPath);

        var extensions = new[] { ".png", ".jpg", ".jpeg", ".webp" };

        foreach (var ext in extensions)
        {
            var previewPath = Path.Combine(directory!, fileName + ext);
            if (File.Exists(previewPath))
            {
                return previewPath;
            }
        }

        return null;
    }

    #endregion

    #region INotifyPropertyChanged

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    #endregion
}

#region Supporting Enums and DTOs

public enum LoRACategory
{
    All,
    Style,
    Character,
    Concept,
    Clothing,
    Background,
    Pose,
    Other
}

public enum SortOrder
{
    Name,
    DateAdded,
    FileSize,
    Category
}

public class LoRADto
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string FileName { get; set; } = "";
    public string FilePath { get; set; } = "";
    public long FileSize { get; set; }
    public string Description { get; set; } = "";
    public LoRACategory Category { get; set; }
    public List<string> Tags { get; set; } = new();
    public List<string> TriggerWords { get; set; } = new();
    public string? PreviewImagePath { get; set; }
    public string BaseModel { get; set; } = "";
    public string Version { get; set; } = "";
    public string Author { get; set; } = "";
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
    public bool IsLoaded { get; set; }
    public float RecommendedWeight { get; set; } = 0.8f;

    public string FormattedFileSize => FormatFileSize(FileSize);
    public string ShortDescription => Description.Length > 100 ? Description.Substring(0, 100) + "..." : Description;

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}

public class AppliedLoRADto
{
    public string Id { get; set; } = "";
    public string LoRAId { get; set; } = "";
    public string Name { get; set; } = "";
    public string FilePath { get; set; } = "";
    public float Weight { get; set; } = 0.8f;
    public bool IsEnabled { get; set; } = true;
    public int Order { get; set; }
}

public class LoRAMetadata
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public LoRACategory Category { get; set; }
    public List<string>? Tags { get; set; }
    public List<string>? TriggerWords { get; set; }
    public string? BaseModel { get; set; }
    public string? Version { get; set; }
    public string? Author { get; set; }
    public float RecommendedWeight { get; set; } = 0.8f;
}

#endregion