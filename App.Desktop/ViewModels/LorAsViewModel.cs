using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows;
using Lazarus.Desktop.Helpers;
using Lazarus.Shared.Models;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Specialized;

namespace Lazarus.Desktop.ViewModels;

public class LorAsViewModel : INotifyPropertyChanged
{
    private bool _isLoading;
    private string _statusText = "Ready to discover LoRAs...";
    private LoRADto? _selectedLoRA;
    private string _searchFilter = "";
    private LoRACategory _selectedCategory = LoRACategory.All;
    private SortOrder _sortOrder = SortOrder.Name;

    private readonly Dispatcher _dispatcher;
    
    // Static event for cross-tab LoRA state communication
    public static event EventHandler? LoRAStateChanged;
    
    // Static reference to current instance for cross-tab access
    private static LorAsViewModel? _currentInstance;
    
    /// <summary>
    /// Get the current applied active LoRAs count for cross-tab synchronization
    /// </summary>
    public static int GetCurrentAppliedActiveCount() => _currentInstance?.AppliedLoRAsCount ?? 0;
    
    /// <summary>
    /// Get the current applied LoRAs collection for cross-tab status display
    /// </summary>
    public static ObservableCollection<AppliedLoRADto>? GetCurrentAppliedLoRAs() => _currentInstance?.AppliedLoRAs;

    public LorAsViewModel()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("[EMERGENCY] LorAsViewModel constructor START");
            Console.WriteLine("[EMERGENCY] LorAsViewModel constructor START");
            
            // Set static instance for cross-tab access
            _currentInstance = this;
            
            // Get dispatcher for UI thread safety
            System.Diagnostics.Debug.WriteLine("[EMERGENCY] Getting dispatcher...");
            _dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
            System.Diagnostics.Debug.WriteLine($"[EMERGENCY] Dispatcher obtained: {_dispatcher != null}");
            
            System.Diagnostics.Debug.WriteLine("[EMERGENCY] Creating ObservableCollections...");
            AvailableLoRAs = new ObservableCollection<LoRADto>();
            AppliedLoRAs = new ObservableCollection<AppliedLoRADto>();
            System.Diagnostics.Debug.WriteLine("[EMERGENCY] ObservableCollections created");
            
            System.Diagnostics.Debug.WriteLine("[EMERGENCY] Creating Categories collection...");
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

        // Set SortOrders enum for the combobox binding
        SortOrders = new ObservableCollection<SortOrder>
        {
            SortOrder.Name,
            SortOrder.DateAdded,
            SortOrder.FileSize,
            SortOrder.Category
        };

        // Commands - the digital rituals
        ScanLoRAsCommand = new RelayCommand(async _ => await SafeExecuteAsync(ScanLoRAsAsync), _ => !IsLoading);
        ApplyLoRACommand = new RelayCommand(async lora => {
            try 
            {
                System.Diagnostics.Debug.WriteLine("[EMERGENCY] ApplyLoRACommand ENTRY");
                Console.WriteLine($"[EMERGENCY] ApplyLoRACommand triggered with parameter: {lora?.GetType()?.Name ?? "null"}");
                
                if (lora is LoRADto dto)
                {
                    System.Diagnostics.Debug.WriteLine($"[EMERGENCY] Got LoRADto: {dto.Name}");
                    Console.WriteLine($"[EMERGENCY] Applying LoRA: {dto.Name ?? "null name"}, Path: {dto.FilePath ?? "null path"}");
                    
                    // Direct call instead of SafeExecuteAsync to avoid any wrapper issues
                    System.Diagnostics.Debug.WriteLine("[EMERGENCY] About to call ApplyLoRAAsync directly");
                    await ApplyLoRAAsync(dto);
                    System.Diagnostics.Debug.WriteLine("[EMERGENCY] ApplyLoRAAsync completed");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[EMERGENCY] Invalid parameter type: {lora?.GetType()?.Name ?? "null"}");
                    Console.WriteLine($"[EMERGENCY] Invalid parameter type for ApplyLoRACommand: {lora?.GetType()?.Name ?? "null"}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[EMERGENCY] Command crashed: {ex.Message}");
                Console.WriteLine($"[EMERGENCY] ApplyLoRACommand exception: {ex.GetType().Name}: {ex.Message}");
                Console.WriteLine($"[EMERGENCY] Stack trace: {ex.StackTrace}");
            }
        }, lora => {
            try 
            {
                var result = !IsLoading && lora is LoRADto;
                System.Diagnostics.Debug.WriteLine($"[EMERGENCY] CanExecute check: IsLoading={IsLoading}, lora is LoRADto={lora is LoRADto}, result={result}");
                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[EMERGENCY] CanExecute crashed: {ex.Message}");
                return false;
            }
        });
        RemoveLoRACommand = new RelayCommand(async applied => await SafeExecuteAsync(() => RemoveLoRAAsync((AppliedLoRADto)applied!)),
            applied => applied is AppliedLoRADto);
        ClearAllLoRAsCommand = new RelayCommand(async _ => await SafeExecuteAsync(ClearAllLoRAsAsync), _ => AppliedLoRAs.Any());
        RefreshCommand = new RelayCommand(async _ => await SafeExecuteAsync(ScanLoRAsAsync), _ => !IsLoading);
        ImportLoRACommand = new RelayCommand(async _ => await SafeExecuteAsync(ImportLoRAAsync), _ => !IsLoading);
        ToggleLoRACommand = new RelayCommand(async applied => await SafeExecuteAsync(() => ToggleLoRAAsync((AppliedLoRADto)applied!)),
            applied => !IsLoading && applied is AppliedLoRADto);

            // Auto-scan moved to explicit initialization
            System.Diagnostics.Debug.WriteLine("[EMERGENCY] Constructor completed successfully - auto-scan deferred");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[EMERGENCY] Constructor crash: {ex.Message}");
            Console.WriteLine($"[EMERGENCY] Constructor exception: {ex.GetType().Name}: {ex.Message}");
            Console.WriteLine($"[EMERGENCY] Constructor stack trace: {ex.StackTrace}");
            throw; // Re-throw to see the actual crash
        }
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
    public ObservableCollection<SortOrder> SortOrders { get; }

    public int TotalLoRAsCount => AvailableLoRAs?.Count ?? 0;
    public int AppliedLoRAsCount => AppliedLoRAs?.Count(l => l.IsEnabled) ?? 0;
    public int TotalAppliedLoRAsCount 
    { 
        get 
        {
            var count = AppliedLoRAs?.Count ?? 0;
            System.Diagnostics.Debug.WriteLine($"[DEBUG] TotalAppliedLoRAsCount = {count}, Collection has items: {AppliedLoRAs?.Any()}");
            return count;
        }
    }
    public float TotalWeight => AppliedLoRAs?.Where(l => l.IsEnabled).Sum(l => l.Weight) ?? 0.0f;

    #endregion

    #region Commands

    public ICommand ScanLoRAsCommand { get; }
    public ICommand ApplyLoRACommand { get; }
    public ICommand RemoveLoRACommand { get; }
    public ICommand ClearAllLoRAsCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand ImportLoRACommand { get; }
    public ICommand ToggleLoRACommand { get; }

    #endregion

    #region Methods
    
    /// <summary>
    /// Initialize the ViewModel after UI is fully loaded to prevent race conditions
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("[EMERGENCY] LorAsViewModel.InitializeAsync START");
            Console.WriteLine("[EMERGENCY] LorAsViewModel.InitializeAsync START");
            
            // Small delay to ensure UI thread is stable
            await Task.Delay(100);
            
            // Start the scan
            await SafeExecuteAsync(ScanLoRAsAsync);
            
            System.Diagnostics.Debug.WriteLine("[EMERGENCY] LorAsViewModel.InitializeAsync COMPLETED");
            Console.WriteLine("[EMERGENCY] LorAsViewModel.InitializeAsync COMPLETED");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[EMERGENCY] InitializeAsync crashed: {ex.Message}");
            Console.WriteLine($"[EMERGENCY] LorAsViewModel.InitializeAsync exception: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private async Task ScanLoRAsAsync()
    {
        try
        {
            IsLoading = true;
            StatusText = "Scanning for LoRA artifacts...";

            // Use the specified user directory for LoRAs
            var lorasPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Lazarus", "models", "loras");
            var discoveredLoRAs = new List<LoRADto>();

            if (Directory.Exists(lorasPath))
            {
                StatusText = "Analyzing LoRA collection in user directory...";
                
                // Look for .safetensors files with optional adapter_config.json pairs
                var adapterFiles = Directory.GetFiles(lorasPath, "*.safetensors", SearchOption.AllDirectories);
                
                StatusText = $"Found {adapterFiles.Length} potential LoRA adapters...";

                foreach (var adapterFile in adapterFiles)
                {
                    var lora = await AnalyzeLoRAFileAsync(adapterFile);
                    if (lora != null)
                    {
                        discoveredLoRAs.Add(lora);
                    }
                }
            }
            else
            {
                Directory.CreateDirectory(lorasPath);
                StatusText = $"Created LoRA directory: {lorasPath}";
            }

            // THREAD-SAFE BATCH UPDATE - All collection changes on UI thread
            await _dispatcher.InvokeAsync(() =>
            {
                try
                {
                    Console.WriteLine($"[THREAD SAFETY] Starting batch LoRA collection update");
                    
                    // Batch all changes to prevent multiple refresh cycles
                    // Temporarily disable collection changed notifications for better performance
                    var sortedLoRAs = discoveredLoRAs.OrderBy(l => l.Name).ToList();
                    
                    AvailableLoRAs.Clear();
                    foreach (var lora in sortedLoRAs)
                    {
                        AvailableLoRAs.Add(lora);
                    }
                    
                    OnPropertyChanged(nameof(TotalLoRAsCount));
                    Console.WriteLine($"[THREAD SAFETY] Batch update completed - {discoveredLoRAs.Count} items");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[THREAD SAFETY] Collection update failed: {ex.Message}");
                }
            }, System.Windows.Threading.DispatcherPriority.Normal);

            StatusText = $"Discovered {discoveredLoRAs.Count} LoRA artifacts in {lorasPath}";
        }
        catch (OutOfMemoryException ex)
        {
            StatusText = $"Insufficient memory to scan LoRA collection: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"[LoRA] Out of memory during scan: {ex}");
        }
        catch (UnauthorizedAccessException ex)
        {
            StatusText = $"Access denied to LoRA directory: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"[LoRA] Access denied during scan: {ex}");
        }
        catch (DirectoryNotFoundException ex)
        {
            StatusText = $"LoRA directory not found: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"[LoRA] Directory not found during scan: {ex}");
        }
        catch (PathTooLongException ex)
        {
            StatusText = $"LoRA path too long: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"[LoRA] Path too long during scan: {ex}");
        }
        catch (IOException ex)
        {
            StatusText = $"I/O error during LoRA scan: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"[LoRA] I/O error during scan: {ex}");
        }
        catch (Exception ex)
        {
            StatusText = $"LoRA scanning failed: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"[LoRA] Unexpected error during scan: {ex}");
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
            // Defensive null/empty checks
            if (string.IsNullOrWhiteSpace(filePath))
            {
                System.Diagnostics.Debug.WriteLine($"[LoRA] Invalid file path: null or empty");
                return null;
            }

            if (!File.Exists(filePath))
            {
                System.Diagnostics.Debug.WriteLine($"[LoRA] File does not exist: {filePath}");
                return null;
            }

            var fileInfo = new FileInfo(filePath);
            var fileName = Path.GetFileNameWithoutExtension(fileInfo.Name);
            var directory = Path.GetDirectoryName(filePath) ?? "";

            // Look for adapter_config.json first (primary metadata source)
            var adapterConfigPath = Path.Combine(directory, "adapter_config.json");
            var metadataPath = Path.ChangeExtension(filePath, ".json");
            
            LoRAMetadata? metadata = null;
            AdapterConfig? adapterConfig = null;

            // Try to load adapter_config.json (HuggingFace PEFT format)
            if (File.Exists(adapterConfigPath))
            {
                try
                {
                    var adapterJson = await File.ReadAllTextAsync(adapterConfigPath);
                    adapterConfig = JsonSerializer.Deserialize<AdapterConfig>(adapterJson, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        AllowTrailingCommas = true,
                        ReadCommentHandling = JsonCommentHandling.Skip
                    });
                    System.Diagnostics.Debug.WriteLine($"[LoRA] ✅ Parsed adapter_config.json for {fileName}:");
                    System.Diagnostics.Debug.WriteLine($"  - Type: {adapterConfig?.PeftType}");
                    System.Diagnostics.Debug.WriteLine($"  - Rank: {adapterConfig?.R}");
                    System.Diagnostics.Debug.WriteLine($"  - Alpha: {adapterConfig?.LoraAlpha}");
                    System.Diagnostics.Debug.WriteLine($"  - Target Modules: {string.Join(", ", adapterConfig?.TargetModules ?? new List<string>())}");
                    System.Diagnostics.Debug.WriteLine($"  - Base Model: {adapterConfig?.BaseModelNameOrPath}");
                }
                catch (JsonException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[LoRA] JSON parsing failed for adapter_config.json: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"[LoRA] Creating minimal LoRA config due to malformed adapter_config.json");
                    
                    // Try to create a minimal valid config
                    adapterConfig = CreateMinimalAdapterConfig(fileName);
                }
                catch (UnauthorizedAccessException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[LoRA] Access denied to adapter_config.json: {ex.Message}");
                }
                catch (FileNotFoundException)
                {
                    // File disappeared - ignore silently
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[LoRA] Failed to read adapter_config.json: {ex.Message}");
                }
            }

            // Try to load companion metadata file
            if (File.Exists(metadataPath))
            {
                try
                {
                    var json = await File.ReadAllTextAsync(metadataPath);
                    metadata = JsonSerializer.Deserialize<LoRAMetadata>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        AllowTrailingCommas = true,
                        ReadCommentHandling = JsonCommentHandling.Skip
                    });
                    System.Diagnostics.Debug.WriteLine($"[LoRA] ✅ Parsed metadata.json for {fileName}");
                }
                catch (JsonException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[LoRA] JSON parsing failed for metadata.json: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"[LoRA] Creating minimal metadata due to malformed metadata.json");
                    
                    // Try to create minimal valid metadata
                    metadata = CreateMinimalMetadata(fileName);
                }
                catch (UnauthorizedAccessException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[LoRA] Access denied to metadata.json: {ex.Message}");
                }
                catch (FileNotFoundException)
                {
                    // File disappeared - ignore silently
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[LoRA] Failed to read metadata.json: {ex.Message}");
                }
            }

            // Determine LoRA type and characteristics
            var loraType = DetermineLoRAType(adapterConfig, fileName);
            var complexity = CalculateComplexity(adapterConfig, fileInfo.Length);
            var recommendedWeight = DetermineRecommendedWeight(adapterConfig, metadata, loraType);

            // Defensive creation with null-safe operations
            var lora = new LoRADto
            {
                Id = Guid.NewGuid().ToString(),
                Name = SafeGetString(metadata?.Name ?? adapterConfig?.Description ?? CleanFileName(fileName ?? "Unknown")),
                FileName = fileInfo.Name ?? "Unknown.safetensors",
                FilePath = filePath,
                FileSize = fileInfo.Length,
                Description = SafeGetString(metadata?.Description ?? adapterConfig?.Description ?? GenerateDescription(fileName ?? "Unknown", loraType, complexity)),
                Category = metadata?.Category ?? InferCategoryFromName(fileName ?? ""),
                Tags = metadata?.Tags ?? InferTagsFromName(fileName ?? "", adapterConfig) ?? new List<string>(),
                TriggerWords = metadata?.TriggerWords ?? ExtractTriggerWords(fileName ?? "", metadata) ?? new List<string>(),
                PreviewImagePath = FindPreviewImage(filePath),
                BaseModel = SafeGetString(metadata?.BaseModel ?? adapterConfig?.BaseModelNameOrPath ?? InferBaseModel(fileName ?? "")),
                Version = SafeGetString(metadata?.Version ?? "1.0"),
                Author = SafeGetString(metadata?.Author ?? "Unknown"),
                CreatedDate = fileInfo.CreationTime,
                ModifiedDate = fileInfo.LastWriteTime,
                IsLoaded = false,
                RecommendedWeight = Math.Clamp(recommendedWeight, 0.0f, 2.0f), // Clamp weight to safe range
                LoRAType = SafeGetString(loraType),
                Rank = Math.Clamp(adapterConfig?.R ?? 16, 1, 1024), // Clamp rank to reasonable range
                Alpha = Math.Clamp(adapterConfig?.LoraAlpha ?? 16, 1, 1024),
                TargetModules = adapterConfig?.TargetModules ?? new List<string>()
            };

            return lora;
        }
        catch (UnauthorizedAccessException ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LoRA] Access denied for {filePath}: {ex.Message}");
            SafeUpdateUI(() => StatusText = $"Access denied to {Path.GetFileName(filePath)} - check file permissions");
            return null;
        }
        catch (DirectoryNotFoundException ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LoRA] Directory not found for {filePath}: {ex.Message}");
            SafeUpdateUI(() => StatusText = $"Directory not found for {Path.GetFileName(filePath)}");
            return null;
        }
        catch (FileNotFoundException ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LoRA] File not found: {filePath}: {ex.Message}");
            return null; // File disappeared during processing - silent failure
        }
        catch (IOException ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LoRA] IO error analyzing {filePath}: {ex.Message}");
            SafeUpdateUI(() => StatusText = $"File access error for {Path.GetFileName(filePath)} - file may be in use");
            return null;
        }
        catch (JsonException ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LoRA] JSON parsing error for {filePath}: {ex.Message}");
            SafeUpdateUI(() => StatusText = $"Invalid JSON configuration in {Path.GetFileName(filePath)}");
            
            // Create a basic LoRA entry even with malformed JSON
            try
            {
                return CreateFallbackLoRADto(filePath);
            }
            catch
            {
                return null;
            }
        }
        catch (OutOfMemoryException ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LoRA] Out of memory analyzing large file {filePath}: {ex.Message}");
            SafeUpdateUI(() => StatusText = $"File {Path.GetFileName(filePath)} is too large to process");
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LoRA] Unexpected error analyzing {filePath}: {ex}");
            SafeUpdateUI(() => StatusText = $"Failed to analyze {Path.GetFileName(filePath)}: {ex.Message}");
            
            // Try to create a minimal entry for the file
            try
            {
                return CreateFallbackLoRADto(filePath);
            }
            catch
            {
                return null;
            }
        }
    }

    /// <summary>
    /// Public method for direct Apply button calls - bypasses command system entirely
    /// </summary>
    public async Task ApplyLoRADirectAsync(LoRADto lora)
    {
        if (IsLoading || lora == null) return;
        
        IsLoading = true;
        StatusText = $"Applying {lora.Name}...";
        
        try
        {
            await ApplyLoRAAsync(lora);
        }
        catch (Exception ex)
        {
            StatusText = $"Failed to apply {lora.Name}: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private Task ApplyLoRAAsync(LoRADto lora)
    {
        if (lora?.Name == null || AppliedLoRAs?.Any(a => a.LoRAId == lora.Id) == true)
        {
            StatusText = "LoRA already applied or invalid";
            return Task.CompletedTask;
        }

        var appliedLora = new AppliedLoRADto
        {
            Id = Guid.NewGuid().ToString(),
            LoRAId = lora.Id,
            Name = lora.Name,
            FilePath = lora.FilePath,
            Order = AppliedLoRAs.Count + 1
        };
        
        // Set initial state BEFORE enabling notifications
        appliedLora.Weight = lora.RecommendedWeight > 0 ? lora.RecommendedWeight : 0.8f;
        appliedLora.IsEnabled = true; // Direct property assignment = checkbox checked + "ACTIVE" text
        
        // Enable notifications AFTER setting initial state
        appliedLora.EnablePropertyNotifications();
        
        // Subscribe to property changes
        appliedLora.PropertyChanged += OnAppliedLoRAPropertyChanged;

        AppliedLoRAs.Add(appliedLora);
        lora.IsLoaded = true;
        
        // CRITICAL: Notify UI that collection count changed
        OnPropertyChanged(nameof(TotalAppliedLoRAsCount));
        OnPropertyChanged(nameof(AppliedLoRAsCount));
        OnPropertyChanged(nameof(TotalWeight));
        
        // Notify other tabs of LoRA state change
        NotifyLoRAStateChanged();

        StatusText = $"Applied {lora.Name}";
        return Task.CompletedTask;
    }

    private async Task RemoveLoRAAsync(AppliedLoRADto appliedLora)
    {
        try
        {
            // Unsubscribe from property changes
            appliedLora.PropertyChanged -= OnAppliedLoRAPropertyChanged;
            
            await _dispatcher.InvokeAsync(() =>
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

                OnPropertyChanged(nameof(AppliedLoRAsCount));
                OnPropertyChanged(nameof(TotalAppliedLoRAsCount));
                OnPropertyChanged(nameof(TotalWeight));
                
                // Notify other tabs of LoRA state change
                NotifyLoRAStateChanged();
            });

            StatusText = $"Removed {appliedLora.Name}";

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

            await _dispatcher.InvokeAsync(() =>
            {
                // Reset all loaded states and unsubscribe from property changes
                foreach (var appliedLora in AppliedLoRAs)
                {
                    var originalLora = AvailableLoRAs.FirstOrDefault(l => l.Id == appliedLora.LoRAId);
                    if (originalLora != null)
                    {
                        originalLora.IsLoaded = false;
                    }
                    
                    // Unsubscribe from property changes
                    appliedLora.PropertyChanged -= OnAppliedLoRAPropertyChanged;
                }

                AppliedLoRAs.Clear();
                OnPropertyChanged(nameof(AppliedLoRAsCount));
                OnPropertyChanged(nameof(TotalAppliedLoRAsCount));
                OnPropertyChanged(nameof(TotalWeight));
                
                // Notify other tabs of LoRA state change
                NotifyLoRAStateChanged();
            });

            StatusText = "All LoRAs cleared";

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

    /// <summary>
    /// Toggle LoRA enabled/disabled state with proper loading/unloading from model
    /// </summary>
    private async Task ToggleLoRAAsync(AppliedLoRADto appliedLora)
    {
        try
        {
            var newState = !appliedLora.IsEnabled;
            
            // Show loading state immediately
            appliedLora.IsLoading = true;
            StatusText = $"{(newState ? "Loading" : "Unloading")} LoRA '{appliedLora.Name}' from model...";

            // Create LoRA info for orchestrator
            var loraInfo = new AppliedLoRAInfo
            {
                Id = appliedLora.Id,
                Name = appliedLora.Name,
                FilePath = appliedLora.FilePath,
                Weight = appliedLora.Weight,
                IsEnabled = newState,
                Order = appliedLora.Order,
                AppliedAt = DateTime.UtcNow,
                Description = $"LoRA {(newState ? "loaded into" : "unloaded from")} model with weight {appliedLora.Weight:F2}"
            };

            // Sync with orchestrator FIRST before updating UI
            bool success;
            if (newState)
            {
                // Load LoRA into model
                success = await ApiClient.ApplyLoRAAsync(loraInfo);
                StatusText = success 
                    ? $"✅ LoRA '{appliedLora.Name}' loaded into model (active)" 
                    : $"❌ Failed to load LoRA '{appliedLora.Name}' into model";
            }
            else
            {
                // Unload LoRA from model by removing it entirely
                success = await ApiClient.RemoveLoRAAsync(appliedLora.Id);
                StatusText = success 
                    ? $"⏹️ LoRA '{appliedLora.Name}' unloaded from model (inactive)" 
                    : $"❌ Failed to unload LoRA '{appliedLora.Name}' from model";
            }

            // Only update local state if orchestrator operation succeeded
            if (success)
            {
                appliedLora.IsEnabled = newState;
                OnPropertyChanged(nameof(AppliedLoRAsCount));
                OnPropertyChanged(nameof(TotalWeight));
                
                // Update original LoRA loaded status
                var originalLora = AvailableLoRAs.FirstOrDefault(l => l.Id == appliedLora.LoRAId);
                if (originalLora != null)
                {
                    originalLora.IsLoaded = newState;
                }
                
                // Notify other tabs of LoRA state change
                NotifyLoRAStateChanged();
                
                Console.WriteLine($"[LoRA] ✅ Successfully {(newState ? "loaded" : "unloaded")} {appliedLora.Name}");
            }
            else
            {
                // Keep original state if operation failed
                Console.WriteLine($"[LoRA] ❌ Failed to {(newState ? "load" : "unload")} {appliedLora.Name}");
            }
        }
        catch (Exception ex)
        {
            StatusText = $"Failed to {(appliedLora.IsEnabled ? "unload" : "load")} LoRA: {ex.Message}";
            Console.WriteLine($"[LoRA] Toggle exception for {appliedLora.Name}: {ex.Message}");
        }
        finally
        {
            // Always hide loading state when operation completes
            appliedLora.IsLoading = false;
        }
    }


    private void FilterLoRAs()
    {
        try
        {
            // TODO: Implement filtering logic if needed
            // For now, just update status
            var filterText = string.IsNullOrEmpty(SearchFilter) ? "all" : $"'{SearchFilter}'";
            var categoryText = SelectedCategory == LoRACategory.All ? "all categories" : SelectedCategory.ToString();

            SafeUpdateUI(() => StatusText = $"Showing {filterText} LoRAs in {categoryText}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LoRA] Error in FilterLoRAs: {ex.Message}");
        }
    }

    private void SortLoRAs()
    {
        try
        {
            // TODO: Implement sorting if using filtered collections
            SafeUpdateUI(() => StatusText = $"Sorted by {SortOrder}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LoRA] Error in SortLoRAs: {ex.Message}");
        }
    }

    #endregion

    #region Safe Execution and Error Handling

    private async Task SafeExecuteAsync(Func<Task> asyncAction)
    {
        if (asyncAction == null)
        {
            Console.WriteLine("[LorAsViewModel] SafeExecuteAsync: asyncAction is null, aborting");
            return;
        }
        
        try
        {
            Console.WriteLine($"[LorAsViewModel] Starting SafeExecuteAsync operation");
            IsLoading = true;
            Console.WriteLine($"[LorAsViewModel] Set IsLoading = true");
            await asyncAction();
            Console.WriteLine($"[LorAsViewModel] SafeExecuteAsync operation completed successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LorAsViewModel] SafeExecuteAsync caught exception: {ex.GetType().Name}: {ex.Message}");
            Console.WriteLine($"[LorAsViewModel] Stack trace: {ex.StackTrace}");
            
            try
            {
                if (_dispatcher != null)
                {
                    await _dispatcher.InvokeAsync(() =>
                    {
                        StatusText = $"❌ Operation failed: {ex.Message}";
                        Console.WriteLine($"[LorAsViewModel] Updated status text with error message");
                    });
                }
                else
                {
                    StatusText = $"❌ Operation failed: {ex.Message}";
                    Console.WriteLine($"[LorAsViewModel] No dispatcher available, set status directly");
                }
            }
            catch (Exception dispatchEx)
            {
                Console.WriteLine($"[LorAsViewModel] Failed to update UI after error: {dispatchEx.Message}");
            }
        }
        finally
        {
            try
            {
                IsLoading = false;
                Console.WriteLine($"[LorAsViewModel] SafeExecuteAsync finally block - IsLoading set to false");
            }
            catch (Exception finallyEx)
            {
                Console.WriteLine($"[LorAsViewModel] Error in finally block: {finallyEx.Message}");
            }
        }
    }

    private void SafeUpdateUI(Action uiAction)
    {
        try
        {
            if (_dispatcher.CheckAccess())
            {
                uiAction();
            }
            else
            {
                _dispatcher.Invoke(uiAction);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LoRAs] UI Update Error: {ex}");
        }
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
        
        if (string.IsNullOrWhiteSpace(fileName))
            return tags;
            
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
        try
        {
            var directory = Path.GetDirectoryName(loraPath);
            var fileName = Path.GetFileNameWithoutExtension(loraPath);

            if (string.IsNullOrEmpty(directory) || string.IsNullOrEmpty(fileName))
                return null;

            var extensions = new[] { ".png", ".jpg", ".jpeg", ".webp" };

            foreach (var ext in extensions)
            {
                var previewPath = Path.Combine(directory, fileName + ext);
                if (File.Exists(previewPath))
                {
                    return previewPath;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LoRA] Error finding preview image for {loraPath}: {ex.Message}");
        }

        return null;
    }

    private static string SafeGetString(string? input)
    {
        return string.IsNullOrWhiteSpace(input) ? "Unknown" : input.Trim();
    }
    
    /// <summary>
    /// Create a fallback LoRA DTO when parsing fails
    /// </summary>
    private LoRADto? CreateFallbackLoRADto(string filePath)
    {
        try
        {
            var fileInfo = new FileInfo(filePath);
            var fileName = Path.GetFileNameWithoutExtension(fileInfo.Name);
            
            System.Diagnostics.Debug.WriteLine($"[LoRA] Creating fallback LoRA DTO for {fileName}");
            
            return new LoRADto
            {
                Id = Guid.NewGuid().ToString(),
                Name = CleanFileName(fileName ?? "Unknown LoRA"),
                FileName = fileInfo.Name,
                FilePath = filePath,
                FileSize = fileInfo.Length,
                Description = $"LoRA file found but configuration could not be parsed. File: {fileName}",
                Category = LoRACategory.Other,
                Tags = new List<string> { "unparseable", "fallback" },
                TriggerWords = new List<string>(),
                PreviewImagePath = FindPreviewImage(filePath),
                BaseModel = "Unknown",
                Version = "Unknown",
                Author = "Unknown",
                CreatedDate = fileInfo.CreationTime,
                ModifiedDate = fileInfo.LastWriteTime,
                IsLoaded = false,
                RecommendedWeight = 0.8f,
                LoRAType = "Unknown",
                Rank = 16,
                Alpha = 16,
                TargetModules = new List<string>(),
                AdapterName = CleanFileName(fileName ?? "Unknown"),
                TaskType = "Unknown",
                LibraryName = "Unknown",
                InferenceMode = true,
                UseRsLora = false,
                UseDora = false,
                InitMethod = "Unknown",
                CompatibleModels = new List<string>(),
                TrainingSteps = null,
                LearningRate = null,
                Optimizer = null,
                TrainingDataset = null
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LoRA] Failed to create fallback DTO for {filePath}: {ex.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Create minimal adapter config when JSON parsing fails
    /// </summary>
    private static AdapterConfig CreateMinimalAdapterConfig(string fileName)
    {
        return new AdapterConfig
        {
            PeftType = "LoRA",
            R = 16,
            LoraAlpha = 16,
            TargetModules = new List<string> { "unknown" },
            TaskType = "CAUSAL_LM",
            Description = $"Reconstructed config for {fileName} (original was malformed)",
            BaseModelNameOrPath = "Unknown",
            AdapterName = fileName
        };
    }
    
    /// <summary>
    /// Create minimal metadata when JSON parsing fails
    /// </summary>
    private static LoRAMetadata CreateMinimalMetadata(string fileName)
    {
        return new LoRAMetadata
        {
            Name = fileName,
            Description = $"LoRA adapter (metadata was malformed)",
            Category = LoRACategory.Other,
            Tags = new List<string> { "recovered" },
            TriggerWords = new List<string>(),
            BaseModel = "Unknown",
            Version = "Unknown",
            Author = "Unknown",
            RecommendedWeight = 0.8f
        };
    }

    private string DetermineLoRAType(AdapterConfig? config, string fileName)
    {
        if (config != null)
        {
            return config.PeftType ?? "LoRA";
        }

        var name = fileName.ToLowerInvariant();
        if (name.Contains("style")) return "Style";
        if (name.Contains("character") || name.Contains("char")) return "Character";
        if (name.Contains("concept")) return "Concept";
        if (name.Contains("pose")) return "Pose";
        if (name.Contains("clothing") || name.Contains("outfit")) return "Clothing";
        if (name.Contains("background") || name.Contains("scene")) return "Scene";
        
        return "General";
    }

    private string CalculateComplexity(AdapterConfig? config, long fileSize)
    {
        if (config != null)
        {
            var rank = config.R ?? 16;
            if (rank >= 128) return "Ultra High";
            if (rank >= 64) return "High";
            if (rank >= 32) return "Medium";
            if (rank >= 16) return "Standard";
            return "Low";
        }

        // Estimate complexity from file size
        var sizeMB = fileSize / (1024.0 * 1024.0);
        if (sizeMB >= 500) return "Ultra High";
        if (sizeMB >= 200) return "High";
        if (sizeMB >= 50) return "Medium";
        if (sizeMB >= 10) return "Standard";
        return "Low";
    }

    private float DetermineRecommendedWeight(AdapterConfig? config, LoRAMetadata? metadata, string loraType)
    {
        if (metadata?.RecommendedWeight != null) return metadata.RecommendedWeight;

        // Type-based recommendations
        return loraType.ToLowerInvariant() switch
        {
            "style" => 0.7f,
            "character" => 0.8f,
            "concept" => 0.6f,
            "pose" => 0.5f,
            "clothing" => 0.6f,
            "scene" => 0.7f,
            _ => 0.8f
        };
    }

    private string CleanFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return "Unknown";
            
        try
        {
            return fileName
                .Replace("_", " ")
                .Replace("-", " ")
                .Replace(".", " ")
                .Trim()
                .ToTitleCase();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LoRA] Error cleaning filename '{fileName}': {ex.Message}");
            return fileName.Trim();
        }
    }

    private string GenerateDescription(string fileName, string loraType, string complexity)
    {
        return $"{loraType} adapter with {complexity.ToLowerInvariant()} complexity. Extracted from {fileName}.";
    }

    private List<string> InferTagsFromName(string fileName, AdapterConfig? config)
    {
        var tags = new List<string>();
        
        if (string.IsNullOrWhiteSpace(fileName))
            return tags;
            
        // Add existing tags
        try
        {
            tags.AddRange(InferTagsFromName(fileName));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LoRA] Error inferring tags from name: {ex.Message}");
        }

        // Add technical tags from config
        if (config != null)
        {
            try
            {
                if (config.R.HasValue && config.R.Value > 0) tags.Add($"rank-{config.R.Value}");
                if (config.LoraAlpha.HasValue && config.LoraAlpha.Value > 0) tags.Add($"alpha-{config.LoraAlpha.Value}");
                if (!string.IsNullOrWhiteSpace(config.PeftType)) tags.Add(config.PeftType.ToLowerInvariant());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LoRA] Error processing adapter config tags: {ex.Message}");
            }
        }

        return tags.Where(t => !string.IsNullOrWhiteSpace(t)).Distinct().ToList();
    }

    private List<string>? ExtractTriggerWords(string fileName, LoRAMetadata? metadata)
    {
        if (metadata?.TriggerWords != null && metadata.TriggerWords.Any())
        {
            return metadata.TriggerWords;
        }

        // Try to extract trigger words from filename patterns
        var triggers = new List<string>();
        var name = fileName.ToLowerInvariant();

        // Common trigger word patterns
        if (name.Contains("style"))
        {
            var style = ExtractStyleFromName(name);
            if (!string.IsNullOrEmpty(style)) triggers.Add(style);
        }

        return triggers.Any() ? triggers : null;
    }

    private string ExtractStyleFromName(string name)
    {
        // Extract style names from common patterns
        var patterns = new[] { "style", "art", "aesthetic" };
        foreach (var pattern in patterns)
        {
            var index = name.IndexOf(pattern);
            if (index > 0)
            {
                return name.Substring(0, index).Trim('_', '-', ' ');
            }
        }
        return "";
    }

    private string InferBaseModel(string fileName)
    {
        var name = fileName.ToLowerInvariant();
        
        if (name.Contains("sdxl") || name.Contains("xl")) return "SDXL";
        if (name.Contains("sd15") || name.Contains("1.5")) return "SD 1.5";
        if (name.Contains("sd20") || name.Contains("2.0")) return "SD 2.0";
        if (name.Contains("sd21") || name.Contains("2.1")) return "SD 2.1";
        if (name.Contains("flux")) return "Flux";
        if (name.Contains("pony")) return "Pony Diffusion";
        
        return "Unknown";
    }

    #endregion

    #region Cross-Tab Communication
    
    /// <summary>
    /// Handle property changes from applied LoRAs (e.g., weight changes)
    /// </summary>
    private void OnAppliedLoRAPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not AppliedLoRADto appliedLora) return;
        
        try
        {
            if (e.PropertyName == nameof(AppliedLoRADto.Weight))
            {
                // Update total weight calculation
                OnPropertyChanged(nameof(TotalWeight));
                NotifyLoRAStateChanged();
            }
            else if (e.PropertyName == nameof(AppliedLoRADto.IsEnabled))
            {
                // Update active count
                OnPropertyChanged(nameof(AppliedLoRAsCount));
                OnPropertyChanged(nameof(TotalWeight));
                NotifyLoRAStateChanged();
            }
        }
        catch (Exception ex)
        {
            // Silent fail to prevent crashes
            System.Diagnostics.Debug.WriteLine($"Property change handler error: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Notify other tabs that LoRA state has changed
    /// </summary>
    private void NotifyLoRAStateChanged()
    {
        try
        {
            Console.WriteLine($"[LorAsViewModel] Broadcasting LoRA state change to other tabs");
            LoRAStateChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LorAsViewModel] Failed to notify LoRA state change: {ex.Message}");
        }
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
    
    // Enhanced LoRA properties with comprehensive metadata
    public string LoRAType { get; set; } = "General";
    public int Rank { get; set; } = 16;
    public int Alpha { get; set; } = 16;
    public List<string> TargetModules { get; set; } = new();
    
    // Advanced adapter properties
    public string AdapterName { get; set; } = "";
    public string TaskType { get; set; } = "";
    public string LibraryName { get; set; } = "";
    public bool InferenceMode { get; set; } = true;
    public bool UseRsLora { get; set; } = false;
    public bool UseDora { get; set; } = false;
    public string InitMethod { get; set; } = "";
    public List<string> CompatibleModels { get; set; } = new();
    
    // Training metadata
    public int? TrainingSteps { get; set; }
    public float? LearningRate { get; set; }
    public string? Optimizer { get; set; }
    public string? TrainingDataset { get; set; }
    
    // Computed properties for UI display
    public string FormattedFileSize => FormatFileSize(FileSize);
    public string ShortDescription => Description.Length > 100 ? Description.Substring(0, 100) + "..." : Description;
    public string TechnicalInfo => $"Precision: {GetPrecisionLabel(Rank)} • Strength: {GetStrengthLabel(Alpha)} • {TargetModules.Count} components";
    public string ComplexityBadge => GetComplexityLabel(Rank);
    public string ModelCompatibility => CompatibleModels.Any() ? string.Join(", ", CompatibleModels.Take(2)) + (CompatibleModels.Count > 2 ? $" +{CompatibleModels.Count - 2}" : "") : BaseModel;
    public string TrainingInfo => TrainingSteps.HasValue ? $"{TrainingSteps:N0} steps" + (LearningRate.HasValue ? $" @ {LearningRate:E1}" : "") : "Unknown";
    public string AdapterTypeDisplay => GetHumanReadableType();
    public string LibraryDisplay => !string.IsNullOrEmpty(LibraryName) ? LibraryName : "PEFT";
    
    // Human-readable tooltips and labels for UX surgery
    public string RankTooltip => $"Complexity Level ({Rank}): {GetRankExplanation(Rank)}";
    public string AlphaTooltip => $"Training Strength ({Alpha}): {GetAlphaExplanation(Alpha)}";
    public string WeightTooltip => "How much this adapter influences the model (0.0 = no effect, 1.0 = full effect, 2.0 = amplified effect)";
    public string PrecisionLabel => GetPrecisionLabel(Rank);
    public string StrengthLabel => GetStrengthLabel(Alpha);

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
    
    // UX Surgery: Human-readable labels instead of technical jargon
    private string GetPrecisionLabel(int rank) => rank switch
    {
        >= 128 => "Ultra High",
        >= 64 => "High",
        >= 32 => "Medium", 
        >= 16 => "Standard",
        _ => "Basic"
    };
    
    private string GetStrengthLabel(int alpha) => alpha switch
    {
        >= 64 => "Very Strong",
        >= 32 => "Strong",
        >= 16 => "Balanced", 
        >= 8 => "Gentle",
        _ => "Minimal"
    };
    
    private string GetComplexityLabel(int rank) => rank switch
    {
        >= 128 => "Professional",
        >= 64 => "Advanced",
        >= 32 => "Intermediate",
        >= 16 => "Standard",
        _ => "Simple"
    };
    
    private string GetHumanReadableType()
    {
        // Parse actual intent from adapter config and filename
        var type = LoRAType?.ToLower() ?? "unknown";
        var name = Name?.ToLower() ?? "";
        var description = Description?.ToLower() ?? "";
        
        // Intelligent classification based on multiple sources
        if (name.Contains("character") || description.Contains("character") || type == "character")
            return "Character Adapter";
        if (name.Contains("style") || description.Contains("style") || name.Contains("art") || type == "style")
            return "Art Style Modifier";
        if (name.Contains("concept") || description.Contains("concept") || type == "concept")
            return "Concept Enhancer";
        if (name.Contains("pose") || name.Contains("position") || type == "pose")
            return "Pose Controller";
        if (name.Contains("clothing") || name.Contains("outfit") || name.Contains("dress") || type == "clothing")
            return "Clothing/Outfit";
        if (name.Contains("background") || name.Contains("scene") || name.Contains("landscape") || type == "background")
            return "Scene/Background";
        if (name.Contains("lighting") || name.Contains("light"))
            return "Lighting Enhancement";
        if (name.Contains("texture") || name.Contains("material"))
            return "Texture/Material";
        if (type == "other" || type == "unknown")
            return "General Purpose Adapter";
            
        return $"Specialized: {char.ToUpper(type[0])}{type.Substring(1)}";
    }
    
    private string GetRankExplanation(int rank) => rank switch
    {
        >= 128 => "Maximum detail preservation - best quality but largest file size",
        >= 64 => "High detail - excellent balance of quality and size",
        >= 32 => "Good detail - suitable for most use cases",
        >= 16 => "Standard detail - efficient and reliable",
        _ => "Basic detail - lightweight but may miss fine details"
    };
    
    private string GetAlphaExplanation(int alpha) => alpha switch
    {
        >= 64 => "Very aggressive training - may overpower the base model",
        >= 32 => "Strong training - noticeable influence on output",
        >= 16 => "Balanced training - good mix with base model",
        >= 8 => "Gentle training - subtle influence",
        _ => "Minimal training - very light touch"
    };
    
    private static string CalculateComplexityFromRank(int rank)
    {
        return rank switch
        {
            >= 128 => "Ultra",
            >= 64 => "High", 
            >= 32 => "Medium",
            >= 16 => "Standard",
            _ => "Low"
        };
    }
}

public class AppliedLoRADto : INotifyPropertyChanged
{
    private float _weight = 0.8f;
    private bool _isEnabled = false;
    private bool _isLoading = false;
    private bool _isInitializing = true; // Prevent events during construction
    
    public string Id { get; set; } = "";
    public string LoRAId { get; set; } = "";
    public string Name { get; set; } = "";
    public string FilePath { get; set; } = "";
    
    public float Weight 
    { 
        get => _weight;
        set
        {
            if (Math.Abs(_weight - value) > 0.001f) // Use epsilon for float comparison
            {
                _weight = Math.Clamp(value, 0.0f, 2.0f); // Clamp to valid range
                if (!_isInitializing)
                {
                    OnPropertyChanged();
                }
            }
        }
    }
    
    public bool IsEnabled 
    { 
        get => _isEnabled;
        set
        {
            if (_isEnabled != value)
            {
                _isEnabled = value;
                if (!_isInitializing)
                {
                    OnPropertyChanged();
                    // Trigger the actual model sync when checkbox changes
                    _ = Task.Run(async () => await SyncWithModelAsync());
                }
            }
        }
    }
    
    /// <summary>
    /// Sync the LoRA state with the model when checkbox changes
    /// </summary>
    private async Task SyncWithModelAsync()
    {
        try
        {
            // TODO: Add actual API sync logic here
            // For now, just simulate the sync behavior
            Console.WriteLine($"[LoRA] Syncing {Name} with model: IsEnabled={IsEnabled}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LoRA] Failed to sync {Name}: {ex.Message}");
        }
    }
    
    public bool IsLoading 
    { 
        get => _isLoading;
        set
        {
            if (_isLoading != value)
            {
                _isLoading = value;
                if (!_isInitializing)
                {
                    OnPropertyChanged();
                }
            }
        }
    }
    
    public int Order { get; set; }
    
    /// <summary>
    /// Call this after object construction to enable property change notifications
    /// </summary>
    public void EnablePropertyNotifications()
    {
        _isInitializing = false;
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;
    
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        if (!_isInitializing)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
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

// HuggingFace PEFT adapter configuration
public class AdapterConfig
{
    [JsonPropertyName("peft_type")]
    public string? PeftType { get; set; }
    
    [JsonPropertyName("r")]
    public int? R { get; set; } // Rank
    
    [JsonPropertyName("lora_alpha")]
    public int? LoraAlpha { get; set; }
    
    [JsonPropertyName("target_modules")]
    public List<string>? TargetModules { get; set; }
    
    [JsonPropertyName("bias")]
    public string? Bias { get; set; }
    
    [JsonPropertyName("task_type")]
    public string? TaskType { get; set; }
    
    [JsonPropertyName("base_model_name_or_path")]
    public string? BaseModelNameOrPath { get; set; }
    
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    
    // Extended metadata for richer display
    [JsonPropertyName("adapter_name")]
    public string? AdapterName { get; set; }
}

// String extension for title case
public static class StringExtensions
{
    public static string ToTitleCase(this string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        
        var words = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < words.Length; i++)
        {
            if (words[i].Length > 0)
            {
                words[i] = char.ToUpper(words[i][0]) + (words[i].Length > 1 ? words[i].Substring(1).ToLower() : "");
            }
        }
        return string.Join(" ", words);
    }
}

#endregion