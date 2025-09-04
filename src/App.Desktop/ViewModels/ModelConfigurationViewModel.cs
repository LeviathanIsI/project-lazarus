using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using Lazarus.App.Desktop.Collections;
using Lazarus.App.Shared.Services;
using Lazarus.App.Data.Services;
using Lazarus.App.Shared.Models;
using Lazarus.App.Desktop.Services;
using Microsoft.Win32;
using System.IO;
using System.ComponentModel;

namespace Lazarus.App.Desktop.ViewModels;

/// <summary>
/// View model for the Model Configuration section - manages asset selection with filesystem scanning
/// </summary>
public partial class ModelConfigurationViewModel : BaseViewModel
{
    private readonly ILogger<ModelConfigurationViewModel> _logger;
    private readonly IAssetKeeperService _assetKeeperService;
    private readonly IAssetRegistryPurificationService _purificationService;
    private readonly IDirectoryService _directoryService;
    private FileSystemWatcher? _fileWatcher;
    
    // Asset Collections for dropdown binding
    private LlmAsset? _selectedBaseModel;
    private LlmAsset? _selectedLoRAAdapter;
    private LlmAsset? _selectedEmbedding;
    private LlmAsset? _selectedTokenizer;
    private LlmAsset? _selectedAsset;
    
    // Multi-select collections for new UI requirements
    private readonly ObservableCollection<LlmAsset> _selectedEmbeddings = new();
    private readonly ObservableCollection<LlmAsset> _selectedLoRAAdapters = new();
    
    // Status and compatibility tracking
    private string _assetScanStatus = "Ready";
    private bool _hasCompatibilityWarning = false;
    private string _compatibilityMessage = string.Empty;
    private bool _canLoadConfiguration = false;
    private bool _isScanning = false;
    
    // Navigation state preservation tracking
    private bool _isInitialized = false;
    private bool _hasLoadedAssets = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="ModelConfigurationViewModel"/> class
    /// </summary>
    /// <param name="logger">The logger instance</param>
    /// <param name="assetKeeperService">The asset keeper service</param>
    /// <param name="purificationService">The asset registry purification service</param>
    /// <param name="directoryService">The directory service for AppData access</param>
    public ModelConfigurationViewModel(
        ILogger<ModelConfigurationViewModel> logger, 
        IAssetKeeperService assetKeeperService,
        IAssetRegistryPurificationService purificationService,
        IDirectoryService directoryService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _assetKeeperService = assetKeeperService ?? throw new ArgumentNullException(nameof(assetKeeperService));
        _purificationService = purificationService ?? throw new ArgumentNullException(nameof(purificationService));
        _directoryService = directoryService ?? throw new ArgumentNullException(nameof(directoryService));
        
        Title = "Model Configuration";
        StatusMessage = "Ready to configure LLM assets";
        
        // Initialize asset collections for dropdowns with UI binding enforcement (SINGLETON-SAFE)
        if (!_isInitialized)
        {
            InitializeCollectionsOnUIThread();
            _isInitialized = true;
        }
        
        // Initialize commands
        AddAssetCommand = new RelayCommand(ExecuteAddAsset);
        RemoveAssetCommand = new RelayCommand<LlmAsset>(ExecuteRemoveAsset);
        RefreshAssetsCommand = new RelayCommand(ExecuteRefreshAssets);
        ValidateAssetCommand = new RelayCommand<LlmAsset>(ExecuteValidateAsset);
        ValidateSelectionCommand = new RelayCommand(ExecuteValidateSelection);
        LoadConfigurationCommand = new RelayCommand(ExecuteLoadConfiguration);
        PurgePhantomAssetsCommand = new RelayCommand(ExecutePurgePhantomAssets);
        CleanupOrphanedAssetsCommand = new RelayCommand(ExecuteCleanupOrphanedAssets);
        ToggleEmbeddingSelectionCommand = new RelayCommand<SelectableAssetWrapper>(ExecuteToggleEmbeddingSelection);
        ToggleLoRAAdapterSelectionCommand = new RelayCommand<SelectableAssetWrapper>(ExecuteToggleLoRAAdapterSelection);
        ClearAllSelectionsCommand = new RelayCommand(ExecuteClearAllSelections);
        
        // NAVIGATION-SAFE: Only initialize asset discovery if not already loaded
        if (!_hasLoadedAssets)
        {
            _ = Task.Run(ForceInitializeAssetDiscoveryAsync);
        }
        
        _logger.LogInformation("ASSET.KEEPER: ModelConfigurationViewModel initialized (Navigation-Safe Mode: Initialized={IsInitialized}, AssetsLoaded={HasLoadedAssets})", _isInitialized, _hasLoadedAssets);
    }

    /// <summary>
    /// Gets the title of the view
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// Gets or sets the selected base model
    /// </summary>
    public LlmAsset? SelectedBaseModel
    {
        get => _selectedBaseModel;
        set
        {
            if (SetProperty(ref _selectedBaseModel, value))
            {
                // Critical Fix: Synchronize base model selection with SelectedAssets table
                if (_selectedBaseModel != null && !SelectedAssets.Contains(_selectedBaseModel))
                {
                    SelectedAssets.Add(_selectedBaseModel);
                }
                ValidateAssetCompatibility();
            }
        }
    }

    /// <summary>
    /// Gets or sets the selected LoRA adapter
    /// </summary>
    public LlmAsset? SelectedLoRAAdapter
    {
        get => _selectedLoRAAdapter;
        set
        {
            if (SetProperty(ref _selectedLoRAAdapter, value))
            {
                ValidateAssetCompatibility();
            }
        }
    }

    /// <summary>
    /// Gets or sets the selected embedding model
    /// </summary>
    public LlmAsset? SelectedEmbedding
    {
        get => _selectedEmbedding;
        set
        {
            if (SetProperty(ref _selectedEmbedding, value))
            {
                ValidateAssetCompatibility();
            }
        }
    }

    /// <summary>
    /// Gets or sets the selected tokenizer
    /// </summary>
    public LlmAsset? SelectedTokenizer
    {
        get => _selectedTokenizer;
        set
        {
            if (SetProperty(ref _selectedTokenizer, value))
            {
                ValidateAssetCompatibility();
            }
        }
    }

    /// <summary>
    /// Gets or sets the selected asset in the registry grid
    /// </summary>
    public LlmAsset? SelectedAsset
    {
        get => _selectedAsset;
        set => SetProperty(ref _selectedAsset, value);
    }

    /// <summary>
    /// Gets or sets the asset scan status message
    /// </summary>
    public string AssetScanStatus
    {
        get => _assetScanStatus;
        set 
        { 
            if (SetProperty(ref _assetScanStatus, value))
            {
                OnPropertyChanged(nameof(HasScanStatus));
            }
        }
    }

    /// <summary>
    /// Gets or sets whether there is a compatibility warning
    /// </summary>
    public bool HasCompatibilityWarning
    {
        get => _hasCompatibilityWarning;
        set => SetProperty(ref _hasCompatibilityWarning, value);
    }

    /// <summary>
    /// Gets or sets the compatibility message
    /// </summary>
    public string CompatibilityMessage
    {
        get => _compatibilityMessage;
        set => SetProperty(ref _compatibilityMessage, value);
    }

    /// <summary>
    /// Gets or sets whether the configuration can be loaded
    /// </summary>
    public bool CanLoadConfiguration
    {
        get => _canLoadConfiguration;
        set => SetProperty(ref _canLoadConfiguration, value);
    }

    /// <summary>
    /// Gets or sets whether assets are currently being scanned
    /// </summary>
    public bool IsScanning
    {
        get => _isScanning;
        set 
        { 
            if (SetProperty(ref _isScanning, value))
            {
                OnPropertyChanged(nameof(IsNotScanning));
            }
        }
    }

    /// <summary>
    /// Gets whether assets are not currently being scanned (inverse of IsScanning)
    /// </summary>
    public bool IsNotScanning => !IsScanning;

    /// <summary>
    /// Gets whether there is a scan status to display
    /// </summary>
    public bool HasScanStatus => !string.IsNullOrWhiteSpace(AssetScanStatus);

    /// <summary>
    /// Gets whether there is a discovery status to display
    /// </summary>
    public bool HasDiscoveryStatus => !string.IsNullOrWhiteSpace(DiscoveryStatusMessage);

    /// <summary>
    /// Gets whether any assets are available for selection
    /// </summary>
    public bool HasAnyAssets => AllAssets?.Count > 0;

    /// <summary>
    /// Gets a summary of selected embeddings for UI display
    /// </summary>
    public string EmbeddingsSelectionSummary
    {
        get
        {
            var count = SelectedEmbeddings.Count;
            return count switch
            {
                0 => "No embeddings selected",
                1 => SelectedEmbeddings.First().Name,
                _ => $"{count} embeddings selected"
            };
        }
    }

    /// <summary>
    /// Gets a summary of selected LoRA adapters for UI display
    /// </summary>
    public string LoRAAdaptersSelectionSummary
    {
        get
        {
            var count = SelectedLoRAAdapters.Count;
            return count switch
            {
                0 => "No LoRA adapters selected",
                1 => SelectedLoRAAdapters.First().Name,
                _ => $"{count} LoRA adapters selected"
            };
        }
    }

    /// <summary>
    /// Gets or sets whether the embeddings dropdown is open
    /// </summary>
    private bool _isEmbeddingsDropdownOpen;
    public bool IsEmbeddingsDropdownOpen
    {
        get => _isEmbeddingsDropdownOpen;
        set => SetProperty(ref _isEmbeddingsDropdownOpen, value);
    }

    /// <summary>
    /// Gets or sets whether the LoRA adapters dropdown is open
    /// </summary>
    private bool _isLoRAAdaptersDropdownOpen;
    public bool IsLoRAAdaptersDropdownOpen
    {
        get => _isLoRAAdaptersDropdownOpen;
        set => SetProperty(ref _isLoRAAdaptersDropdownOpen, value);
    }

    /// <summary>
    /// Gets or sets the discovery status message for UI display
    /// </summary>
    public string DiscoveryStatusMessage { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the collection of base models with aggressive UI binding enforcement
    /// </summary>
    public ThreadSafeObservableCollection<LlmAsset> BaseModels { get; private set; } = null!;

    /// <summary>
    /// Gets the collection of LoRA adapters with aggressive UI binding enforcement
    /// </summary>
    public ThreadSafeObservableCollection<LlmAsset> LoRAAdapters { get; private set; } = null!;

    /// <summary>
    /// Gets the collection of embedding models with aggressive UI binding enforcement
    /// </summary>
    public ThreadSafeObservableCollection<LlmAsset> Embeddings { get; private set; } = null!;

    /// <summary>
    /// Gets the collection of tokenizers with aggressive UI binding enforcement
    /// </summary>
    public ThreadSafeObservableCollection<LlmAsset> Tokenizers { get; private set; } = null!;

    /// <summary>
    /// Gets the collection of all assets for the registry grid with aggressive UI binding enforcement
    /// </summary>
    public ThreadSafeObservableCollection<LlmAsset> AllAssets { get; private set; } = null!;

    /// <summary>
    /// Gets the collection of user-selected assets for the registry grid (user-driven selection only)
    /// </summary>
    public ThreadSafeObservableCollection<LlmAsset> SelectedAssets { get; private set; } = null!;

    /// <summary>
    /// Gets the collection of selectable embeddings with checkbox state
    /// </summary>
    public ThreadSafeObservableCollection<SelectableAssetWrapper> SelectableEmbeddings { get; private set; } = null!;

    /// <summary>
    /// Gets the collection of selectable LoRA adapters with checkbox state
    /// </summary>
    public ThreadSafeObservableCollection<SelectableAssetWrapper> SelectableLoRAAdapters { get; private set; } = null!;

    /// <summary>
    /// Gets the collection of selected embedding models for multi-select UI
    /// </summary>
    public ObservableCollection<LlmAsset> SelectedEmbeddings => _selectedEmbeddings;

    /// <summary>
    /// Gets the collection of selected LoRA adapters for multi-select UI
    /// </summary>
    public ObservableCollection<LlmAsset> SelectedLoRAAdapters => _selectedLoRAAdapters;

    /// <summary>
    /// Gets the add asset command
    /// </summary>
    public IRelayCommand AddAssetCommand { get; }

    /// <summary>
    /// Gets the remove asset command
    /// </summary>
    public IRelayCommand<LlmAsset> RemoveAssetCommand { get; }


    /// <summary>
    /// Gets the refresh assets command
    /// </summary>
    public IRelayCommand RefreshAssetsCommand { get; }

    /// <summary>
    /// Gets the validate asset command
    /// </summary>
    public IRelayCommand<LlmAsset> ValidateAssetCommand { get; }

    /// <summary>
    /// Gets the validate selection command
    /// </summary>
    public IRelayCommand ValidateSelectionCommand { get; }

    /// <summary>
    /// Gets the load configuration command
    /// </summary>
    public IRelayCommand LoadConfigurationCommand { get; }

    /// <summary>
    /// Gets the purge phantom assets command
    /// </summary>
    public IRelayCommand PurgePhantomAssetsCommand { get; }

    /// <summary>
    /// Gets the cleanup orphaned assets command
    /// </summary>
    public IRelayCommand CleanupOrphanedAssetsCommand { get; }

    /// <summary>
    /// Gets the toggle embedding selection command
    /// </summary>
    public IRelayCommand<SelectableAssetWrapper> ToggleEmbeddingSelectionCommand { get; }

    /// <summary>
    /// Gets the toggle LoRA adapter selection command
    /// </summary>
    public IRelayCommand<SelectableAssetWrapper> ToggleLoRAAdapterSelectionCommand { get; }

    /// <summary>
    /// Gets the clear all selections command
    /// </summary>
    public IRelayCommand ClearAllSelectionsCommand { get; }




    /// <summary>
    /// Initializes ObservableCollections on UI thread with binding event handlers
    /// </summary>
    private void InitializeCollectionsOnUIThread()
    {
        if (App.Current?.Dispatcher.CheckAccess() == true)
        {
            InitializeCollectionsCore();
        }
        else
        {
            App.Current?.Dispatcher.Invoke(InitializeCollectionsCore);
        }
    }

    /// <summary>
    /// Core collection initialization with binding enforcement
    /// </summary>
    private void InitializeCollectionsCore()
    {
        _logger.LogInformation("[UI BINDING ENFORCEMENT] Initializing ObservableCollections on UI thread");
        
        BaseModels = new ThreadSafeObservableCollection<LlmAsset>();
        LoRAAdapters = new ThreadSafeObservableCollection<LlmAsset>();
        Embeddings = new ThreadSafeObservableCollection<LlmAsset>();
        Tokenizers = new ThreadSafeObservableCollection<LlmAsset>();
        AllAssets = new ThreadSafeObservableCollection<LlmAsset>();
        SelectedAssets = new ThreadSafeObservableCollection<LlmAsset>();
        SelectableEmbeddings = new ThreadSafeObservableCollection<SelectableAssetWrapper>();
        SelectableLoRAAdapters = new ThreadSafeObservableCollection<SelectableAssetWrapper>();
        
        // Hook collection change events for diagnostic logging and UI updates
        BaseModels.CollectionChanged += (s, e) => 
        {
            _logger.LogDebug("[UI BINDING DIAGNOSTIC] BaseModels collection changed: {Action}, Count: {Count}", e.Action, BaseModels.Count);
            OnPropertyChanged(nameof(BaseModels));
            OnPropertyChanged(nameof(HasAnyAssets));
        };
        
        LoRAAdapters.CollectionChanged += (s, e) => 
        {
            _logger.LogDebug("[UI BINDING DIAGNOSTIC] LoRAAdapters collection changed: {Action}, Count: {Count}", e.Action, LoRAAdapters.Count);
            OnPropertyChanged(nameof(LoRAAdapters));
        };
        
        Embeddings.CollectionChanged += (s, e) => 
        {
            _logger.LogDebug("[UI BINDING DIAGNOSTIC] Embeddings collection changed: {Action}, Count: {Count}", e.Action, Embeddings.Count);
            OnPropertyChanged(nameof(Embeddings));
        };
        
        Tokenizers.CollectionChanged += (s, e) => 
        {
            _logger.LogDebug("[UI BINDING DIAGNOSTIC] Tokenizers collection changed: {Action}, Count: {Count}", e.Action, Tokenizers.Count);
            OnPropertyChanged(nameof(Tokenizers));
        };
        
        AllAssets.CollectionChanged += (s, e) => 
        {
            _logger.LogDebug("[UI BINDING DIAGNOSTIC] AllAssets collection changed: {Action}, Count: {Count}", e.Action, AllAssets.Count);
            OnPropertyChanged(nameof(AllAssets));
            OnPropertyChanged(nameof(HasAnyAssets)); // Update empty state visibility
        };

        SelectedAssets.CollectionChanged += (s, e) => 
        {
            _logger.LogDebug("[UI BINDING DIAGNOSTIC] SelectedAssets collection changed: {Action}, Count: {Count}", e.Action, SelectedAssets.Count);
            OnPropertyChanged(nameof(SelectedAssets));
        };

        SelectableEmbeddings.CollectionChanged += (s, e) => 
        {
            _logger.LogDebug("[UI BINDING DIAGNOSTIC] SelectableEmbeddings collection changed: {Action}, Count: {Count}", e.Action, SelectableEmbeddings.Count);
            OnPropertyChanged(nameof(SelectableEmbeddings));
        };

        SelectableLoRAAdapters.CollectionChanged += (s, e) => 
        {
            _logger.LogDebug("[UI BINDING DIAGNOSTIC] SelectableLoRAAdapters collection changed: {Action}, Count: {Count}", e.Action, SelectableLoRAAdapters.Count);
            OnPropertyChanged(nameof(SelectableLoRAAdapters));
        };
        
        // Hook multi-select collection changes for summary updates
        SelectedEmbeddings.CollectionChanged += (s, e) =>
        {
            _logger.LogDebug("[UX.COPILOT] SelectedEmbeddings changed: {Action}, Count: {Count}", e.Action, SelectedEmbeddings.Count);
            OnPropertyChanged(nameof(EmbeddingsSelectionSummary));
        };
        
        SelectedLoRAAdapters.CollectionChanged += (s, e) =>
        {
            _logger.LogDebug("[UX.COPILOT] SelectedLoRAAdapters changed: {Action}, Count: {Count}", e.Action, SelectedLoRAAdapters.Count);
            OnPropertyChanged(nameof(LoRAAdaptersSelectionSummary));
        };
        
        // Force PropertyChanged notifications for all collection properties
        OnPropertyChanged(nameof(BaseModels));
        OnPropertyChanged(nameof(LoRAAdapters));
        OnPropertyChanged(nameof(Embeddings));
        OnPropertyChanged(nameof(Tokenizers));
        OnPropertyChanged(nameof(AllAssets));
        OnPropertyChanged(nameof(HasAnyAssets));
        OnPropertyChanged(nameof(SelectedAssets));
        OnPropertyChanged(nameof(SelectableEmbeddings));
        OnPropertyChanged(nameof(SelectableLoRAAdapters));
        
        _logger.LogInformation("[UI BINDING ENFORCEMENT] All ObservableCollections initialized with binding event handlers");
    }

    /// <summary>
    /// Forces aggressive asset discovery and dropdown population
    /// </summary>
    private async Task ForceInitializeAssetDiscoveryAsync()
    {
        try
        {
            _logger.LogInformation("ASSET.KEEPER: Starting aggressive asset discovery initialization");
            SetBusyState(true, "Forcing asset discovery and initialization...");
            AssetScanStatus = "Initializing Asset.Keeper...";

            // Prepare directory paths for Asset.Keeper
            var directoryPaths = new Dictionary<string, string>
            {
                { "BaseModels", _directoryService.GetDirectoryPath(DirectoryType.BaseModels) },
                { "LoRAAdapters", _directoryService.GetDirectoryPath(DirectoryType.LoRAAdapters) },
                { "Embeddings", _directoryService.GetDirectoryPath(DirectoryType.Embeddings) },
                { "Tokenizers", _directoryService.GetDirectoryPath(DirectoryType.Tokenizers) }
            };

            // Force aggressive AppData directory initialization and scanning
            var foundAssets = await _assetKeeperService.ForceInitializeAppDataDirectoriesAsync(directoryPaths);
            _logger.LogInformation("ASSET.KEEPER: Discovered {AssetCount} assets during initialization", foundAssets);

            // Load all discovered assets into dropdown collections
            await LoadAllAssetsAsync();

            // Initialize enhanced FileSystemWatcher for real-time monitoring
            InitializeEnhancedFileSystemWatcher();

            // Mark as loaded to prevent re-initialization on navigation
            _hasLoadedAssets = true;

            StatusMessage = foundAssets > 0 
                ? $"Asset.Keeper initialized - {foundAssets} assets discovered"
                : "Asset.Keeper initialized - sample assets created for demonstration";

            AssetScanStatus = foundAssets > 0 
                ? $"Ready - {foundAssets} assets loaded"
                : "Ready - sample assets available";
                
            DiscoveryStatusMessage = foundAssets > 0 
                ? $"Asset.Keeper successfully discovered {foundAssets} assets in AppData directories"
                : "Asset.Keeper initialized with sample assets for demonstration";
            OnPropertyChanged(nameof(DiscoveryStatusMessage));
            OnPropertyChanged(nameof(HasDiscoveryStatus));

            _logger.LogInformation("ASSET.KEEPER: Navigation-safe initialization completed successfully (Assets={AssetCount}, HasLoadedAssets={HasLoadedAssets})", foundAssets, _hasLoadedAssets);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ASSET.KEEPER: Fatal error during aggressive initialization");
            StatusMessage = $"Asset.Keeper initialization failed: {ex.Message}";
            AssetScanStatus = "Initialization failed";
        }
        finally
        {
            SetBusyState(false);
        }
    }

    /// <summary>
    /// Initializes enhanced filesystem watcher for targeted AppData monitoring
    /// </summary>
    private void InitializeEnhancedFileSystemWatcher()
    {
        try
        {
            // Monitor the Models parent directory to catch changes in all subdirectories
            var modelsRootPath = Path.Combine(_directoryService.UserProfilePath, "Models");
            
            if (!Directory.Exists(modelsRootPath))
            {
                _logger.LogDebug("Models root directory does not exist yet: {Path}", modelsRootPath);
                return;
            }

            _fileWatcher = new FileSystemWatcher(modelsRootPath)
            {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite,
                Filter = "*.*" // Monitor all files, filtering will be done in the event handler
            };

            _fileWatcher.Created += OnAssetFileSystemChanged;
            _fileWatcher.Deleted += OnAssetFileSystemChanged;
            _fileWatcher.Renamed += OnAssetFileSystemRenamed;
            _fileWatcher.Error += OnFileSystemWatcherError;
            
            _fileWatcher.EnableRaisingEvents = true;
            _logger.LogInformation("ASSET.KEEPER: Enhanced FileSystemWatcher initialized for {Path}", modelsRootPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ASSET.KEEPER: Failed to initialize enhanced FileSystemWatcher");
        }
    }

    /// <summary>
    /// Handles asset-related filesystem changes in the AppData directories
    /// </summary>
    private async void OnAssetFileSystemChanged(object sender, FileSystemEventArgs e)
    {
        try
        {
            // Only process relevant asset file types
            var extension = Path.GetExtension(e.FullPath).ToLowerInvariant();
            if (!IsRelevantAssetFile(extension)) return;

            _logger.LogDebug("ASSET.KEEPER: Asset file change detected: {ChangeType} - {Path}", e.ChangeType, e.FullPath);
            
            // Debounce rapid changes
            await Task.Delay(1000);
            
            // Handle change on UI thread
            App.Current?.Dispatcher.BeginInvoke(new Action(async () =>
            {
                await HandleAssetFileChangeAsync(e.ChangeType, e.FullPath);
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ASSET.KEEPER: Error handling asset filesystem change: {Path}", e.FullPath);
        }
    }

    /// <summary>
    /// Handles asset file renames in the AppData directories
    /// </summary>
    private async void OnAssetFileSystemRenamed(object sender, RenamedEventArgs e)
    {
        try
        {
            var extension = Path.GetExtension(e.FullPath).ToLowerInvariant();
            if (!IsRelevantAssetFile(extension)) return;

            _logger.LogDebug("ASSET.KEEPER: Asset file renamed: {OldPath} -> {NewPath}", e.OldFullPath, e.FullPath);
            
            await Task.Delay(1000);
            
            App.Current?.Dispatcher.BeginInvoke(new Action(async () =>
            {
                // Handle as delete of old path and creation of new path
                await HandleAssetFileChangeAsync(WatcherChangeTypes.Deleted, e.OldFullPath);
                await HandleAssetFileChangeAsync(WatcherChangeTypes.Created, e.FullPath);
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ASSET.KEEPER: Error handling asset file rename: {OldPath} -> {NewPath}", e.OldFullPath, e.FullPath);
        }
    }

    /// <summary>
    /// Handles FileSystemWatcher errors
    /// </summary>
    private void OnFileSystemWatcherError(object sender, ErrorEventArgs e)
    {
        _logger.LogError(e.GetException(), "ASSET.KEEPER: FileSystemWatcher error occurred");
        
        // Attempt to reinitialize the watcher
        try
        {
            _fileWatcher?.Dispose();
            _fileWatcher = null;
            
            // Delay before reinitializing to avoid rapid restart loops
            Task.Delay(5000).ContinueWith(_ => InitializeEnhancedFileSystemWatcher());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ASSET.KEEPER: Failed to reinitialize FileSystemWatcher after error");
        }
    }

    /// <summary>
    /// Checks if a file extension represents a relevant asset type
    /// </summary>
    private static bool IsRelevantAssetFile(string extension)
    {
        return extension switch
        {
            ".gguf" => true,
            ".safetensors" => true,
            ".bin" => true,
            ".json" => true,
            ".model" => true,
            ".pth" => true,
            ".pt" => true,
            _ => false
        };
    }

    /// <summary>
    /// Executes the add asset command - opens file dialog to add new assets
    /// </summary>
    private async void ExecuteAddAsset()
    {
        _logger.LogInformation("Opening file dialog to add new assets");
        
        var openFileDialog = new OpenFileDialog
        {
            Title = "Select Asset Files",
            Filter = "Asset Files|*.gguf;*.safetensors;*.bin;*.pth;*.pt;*.json;*.model|All Files|*.*",
            Multiselect = true
        };

        if (openFileDialog.ShowDialog() == true)
        {
            SetBusyState(true, "Adding selected assets...");
            
            try
            {
                var addedCount = 0;
                foreach (var filePath in openFileDialog.FileNames)
                {
                    var asset = await _assetKeeperService.RegisterModelAsync(filePath);
                    if (asset != null)
                    {
                        // Force UI thread addition
                        if (App.Current?.Dispatcher != null)
                        {
                            await App.Current.Dispatcher.InvokeAsync(() =>
                            {
                                AddAssetToAppropriateCollectionWithUIBinding(asset);
                                AllAssets.Add(asset);
                                OnPropertyChanged(nameof(BaseModels));
                                OnPropertyChanged(nameof(LoRAAdapters));
                                OnPropertyChanged(nameof(Embeddings));
                                OnPropertyChanged(nameof(Tokenizers));
                                OnPropertyChanged(nameof(AllAssets));
                            });
                            addedCount++;
                        }
                    }
                }
                
                StatusMessage = addedCount > 0 ? $"Successfully added {addedCount} asset(s)" : "No new assets were added";
                AssetScanStatus = $"Added {addedCount} assets";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding asset files");
                StatusMessage = $"Error adding assets: {ex.Message}";
                AssetScanStatus = "Error adding assets";
            }
            finally
            {
                SetBusyState(false);
            }
        }
    }

    /// <summary>
    /// Executes the remove asset command - SELECTION ONLY, preserves dropdown sources
    /// CRITICAL FIX: Remove button removes from table/selections but keeps dropdown options available
    /// </summary>
    /// <param name="asset">The asset to remove from selections</param>
    private void ExecuteRemoveAsset(LlmAsset? asset)
    {
        if (asset == null) return;
        
        _logger.LogInformation("[SELECTION OPERATION] Removing asset from selections (NOT deleting from system): {AssetName}", asset.Name);
        
        try
        {
            SetBusyState(true, $"Removing '{asset.Name}' from selections...");
            
            // CRITICAL FIX: This is a SELECTION removal, NOT a physical asset deletion
            // Remove from selected collections only, preserve dropdown source integrity
            RemoveAssetFromCollections(asset);
            
            StatusMessage = $"'{asset.Name}' removed from selections (still available in dropdowns)";
            AssetScanStatus = "Removed from selections";
            
            _logger.LogInformation("[SELECTION OPERATION] Asset '{AssetName}' removed from selections - dropdown sources preserved", asset.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SELECTION OPERATION] Error removing asset from selections: {AssetName}", asset.Name);
            StatusMessage = $"Error removing from selections: {ex.Message}";
            AssetScanStatus = "Selection removal failed";
        }
        finally
        {
            SetBusyState(false);
        }
    }


    /// <summary>
    /// Executes the refresh assets command
    /// </summary>
    private async void ExecuteRefreshAssets()
    {
        _logger.LogInformation("Refreshing all assets from AppData directories");
        SetBusyState(true, "Refreshing assets...");
        AssetScanStatus = "Refreshing assets...";
        
        try
        {
            await ScanAppDataDirectoriesAsync();
            StatusMessage = "Assets refreshed successfully";
            AssetScanStatus = "Refresh completed";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing assets");
            StatusMessage = $"Error refreshing assets: {ex.Message}";
            AssetScanStatus = "Refresh failed";
        }
        finally
        {
            SetBusyState(false);
        }
    }

    /// <summary>
    /// Executes the validate asset command
    /// </summary>
    /// <param name="asset">The asset to validate</param>
    private async void ExecuteValidateAsset(LlmAsset? asset)
    {
        if (asset == null) return;
        
        _logger.LogInformation("Validating asset: {AssetName}", asset.Name);
        
        try
        {
            SetBusyState(true, $"Validating '{asset.Name}'...");
            
            var isValid = await _assetKeeperService.ValidateAssetAsync(asset.Id);
            await LoadAllAssetsAsync(); // Refresh to show updated status
            
            StatusMessage = isValid ? $"Asset '{asset.Name}' validation passed" : $"Asset '{asset.Name}' validation failed";
            AssetScanStatus = isValid ? "Validation passed" : "Validation failed";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating asset: {AssetName}", asset.Name);
            StatusMessage = $"Error validating asset: {ex.Message}";
            AssetScanStatus = "Validation error";
        }
        finally
        {
            SetBusyState(false);
        }
    }

    /// <summary>
    /// Executes the validate selection command
    /// </summary>
    private void ExecuteValidateSelection()
    {
        _logger.LogInformation("Validating current asset selection");
        ValidateAssetCompatibility();
    }

    /// <summary>
    /// Executes the load configuration command
    /// </summary>
    private async void ExecuteLoadConfiguration()
    {
        if (!CanLoadConfiguration)
        {
            _logger.LogWarning("Cannot load configuration - validation failed");
            return;
        }

        _logger.LogInformation("Loading configuration with selected assets");
        SetBusyState(true, "Loading configuration...");
        
        try
        {
            // TODO: Implement actual configuration loading with Runner.Whisperer
            await Task.Delay(1000); // Placeholder for actual loading logic
            
            StatusMessage = "Configuration loaded successfully";
            AssetScanStatus = "Configuration loaded";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading configuration");
            StatusMessage = $"Error loading configuration: {ex.Message}";
            AssetScanStatus = "Load failed";
        }
        finally
        {
            SetBusyState(false);
        }
    }

    /// <summary>
    /// Executes the purge phantom assets command
    /// </summary>
    private async void ExecutePurgePhantomAssets()
    {
        _logger.LogWarning("DATABASE EXORCISM: User initiated complete phantom asset purge");
        SetBusyState(true, "PURGING ALL PHANTOM ASSETS...");
        AssetScanStatus = "Nuclear option - purging phantom assets...";
        
        try
        {
            // Clear all UI collections immediately to reflect purge
            await ClearAllAssetCollectionsAsync();
            
            // Execute nuclear database purge
            var purificationResult = await _purificationService.PurgeAllPhantomEntriesAsync();
            
            if (purificationResult.Success)
            {
                _logger.LogInformation("PHANTOM PURGE COMPLETE: Eliminated {PhantomCount} phantom entries in {Duration:F2}s",
                    purificationResult.PhantomsEliminated, purificationResult.Duration.TotalSeconds);
                
                StatusMessage = $"Phantom purge complete: {purificationResult.PhantomsEliminated} entries eliminated";
                AssetScanStatus = $"PURGED {purificationResult.PhantomsEliminated} phantom assets";
                
                // Perform fresh asset discovery after purge
                await PerformFreshAssetDiscoveryAsync();
            }
            else
            {
                _logger.LogError("PHANTOM PURGE FAILED: {ErrorMessage}", purificationResult.ErrorMessage);
                StatusMessage = $"Phantom purge failed: {purificationResult.ErrorMessage}";
                AssetScanStatus = "Phantom purge failed";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error during phantom asset purge");
            StatusMessage = $"Phantom purge error: {ex.Message}";
            AssetScanStatus = "Purge operation failed";
        }
        finally
        {
            SetBusyState(false);
        }
    }

    /// <summary>
    /// Executes the toggle embedding selection command
    /// </summary>
    /// <param name="wrapper">The selectable embedding wrapper</param>
    private void ExecuteToggleEmbeddingSelection(SelectableAssetWrapper? wrapper)
    {
        if (wrapper == null) return;

        // The IsSelected property is already toggled by the CheckBox binding
        // We just need to synchronize the collections based on the current state
        
        if (wrapper.IsSelected)
        {
            // Add to selected assets table if not already present
            if (!SelectedAssets.Contains(wrapper.Asset))
            {
                SelectedAssets.Add(wrapper.Asset);
            }

            // Add to legacy collection for compatibility
            if (!SelectedEmbeddings.Contains(wrapper.Asset))
            {
                SelectedEmbeddings.Add(wrapper.Asset);
            }
        }
        else
        {
            // Remove from selected assets table
            SelectedAssets.Remove(wrapper.Asset);
            
            // Remove from legacy collection for compatibility
            SelectedEmbeddings.Remove(wrapper.Asset);
        }

        // Update selection summary
        OnPropertyChanged(nameof(EmbeddingsSelectionSummary));
        ValidateAssetCompatibility();

        _logger.LogDebug("Embedding selection toggled: {AssetName} - Selected: {IsSelected}", wrapper.Name, wrapper.IsSelected);
    }

    /// <summary>
    /// Executes the toggle LoRA adapter selection command
    /// </summary>
    /// <param name="wrapper">The selectable LoRA adapter wrapper</param>
    private void ExecuteToggleLoRAAdapterSelection(SelectableAssetWrapper? wrapper)
    {
        if (wrapper == null) return;

        // The IsSelected property is already toggled by the CheckBox binding
        // We just need to synchronize the collections based on the current state
        
        if (wrapper.IsSelected)
        {
            // Add to selected assets table if not already present
            if (!SelectedAssets.Contains(wrapper.Asset))
            {
                SelectedAssets.Add(wrapper.Asset);
            }

            // Add to legacy collection for compatibility
            if (!SelectedLoRAAdapters.Contains(wrapper.Asset))
            {
                SelectedLoRAAdapters.Add(wrapper.Asset);
            }
        }
        else
        {
            // Remove from selected assets table
            SelectedAssets.Remove(wrapper.Asset);
            
            // Remove from legacy collection for compatibility
            SelectedLoRAAdapters.Remove(wrapper.Asset);
        }

        // Update selection summary
        OnPropertyChanged(nameof(LoRAAdaptersSelectionSummary));
        ValidateAssetCompatibility();

        _logger.LogDebug("LoRA adapter selection toggled: {AssetName} - Selected: {IsSelected}", wrapper.Name, wrapper.IsSelected);
    }

    /// <summary>
    /// Executes the cleanup orphaned assets command
    /// </summary>
    private async void ExecuteCleanupOrphanedAssets()
    {
        _logger.LogInformation("REGISTRY HYGIENE: User initiated orphaned asset cleanup");
        SetBusyState(true, "Cleaning up orphaned assets...");
        AssetScanStatus = "Cleaning up orphaned assets...";
        
        try
        {
            var cleanupResult = await _purificationService.CleanupOrphanedAssetsAsync();
            
            if (cleanupResult.Success)
            {
                _logger.LogInformation("ORPHAN CLEANUP COMPLETE: Removed {OrphansRemoved} orphaned entries, retained {ValidAssetsRetained} valid entries",
                    cleanupResult.OrphansRemoved, cleanupResult.ValidAssetsRetained);
                
                // Refresh UI to reflect cleanup results
                await LoadAllAssetsAsync();
                
                StatusMessage = $"Cleanup complete: {cleanupResult.OrphansRemoved} orphaned assets removed, {cleanupResult.ValidAssetsRetained} valid assets retained";
                AssetScanStatus = $"Cleanup: -{cleanupResult.OrphansRemoved} orphans, +{cleanupResult.ValidAssetsRetained} valid";
            }
            else
            {
                _logger.LogError("ORPHAN CLEANUP FAILED: {ErrorMessage}", cleanupResult.ErrorMessage);
                StatusMessage = $"Orphan cleanup failed: {cleanupResult.ErrorMessage}";
                AssetScanStatus = "Orphan cleanup failed";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during orphaned asset cleanup");
            StatusMessage = $"Orphan cleanup error: {ex.Message}";
            AssetScanStatus = "Cleanup operation failed";
        }
        finally
        {
            SetBusyState(false);
        }
    }

    /// <summary>
    /// Clears all asset collections to eliminate phantom references
    /// </summary>
    private async Task ClearAllAssetCollectionsAsync()
    {
        if (App.Current?.Dispatcher?.CheckAccess() == true)
        {
            ClearAllAssetCollectionsCore();
        }
        else if (App.Current?.Dispatcher != null)
        {
            await App.Current.Dispatcher.InvokeAsync(ClearAllAssetCollectionsCore);
        }
    }

    /// <summary>
    /// Core method to clear all asset collections and selections
    /// </summary>
    private void ClearAllAssetCollectionsCore()
    {
        _logger.LogInformation("PHANTOM CLEARANCE: Clearing all ObservableCollections and selections");
        
        // Clear all selections first to prevent binding issues
        SelectedBaseModel = null;
        SelectedLoRAAdapter = null;
        SelectedEmbedding = null;
        SelectedTokenizer = null;
        SelectedAsset = null;
        
        // Clear multi-select collections
        SelectedEmbeddings.Clear();
        SelectedLoRAAdapters.Clear();
        
        // Clear all collections
        BaseModels.Clear();
        LoRAAdapters.Clear();
        Embeddings.Clear();
        Tokenizers.Clear();
        AllAssets.Clear();
        SelectedAssets.Clear();
        SelectableEmbeddings.Clear();
        SelectableLoRAAdapters.Clear();
        
        // Force PropertyChanged notifications
        OnPropertyChanged(nameof(SelectedBaseModel));
        OnPropertyChanged(nameof(SelectedLoRAAdapter));
        OnPropertyChanged(nameof(SelectedEmbedding));
        OnPropertyChanged(nameof(SelectedTokenizer));
        OnPropertyChanged(nameof(SelectedAsset));
        OnPropertyChanged(nameof(BaseModels));
        OnPropertyChanged(nameof(LoRAAdapters));
        OnPropertyChanged(nameof(Embeddings));
        OnPropertyChanged(nameof(Tokenizers));
        OnPropertyChanged(nameof(AllAssets));
        
        _logger.LogInformation("PHANTOM CLEARANCE: All collections and selections cleared");
    }

    /// <summary>
    /// Performs fresh asset discovery from filesystem after phantom purge
    /// </summary>
    private async Task PerformFreshAssetDiscoveryAsync()
    {
        try
        {
            _logger.LogInformation("POST-PURGE: Performing fresh asset discovery from filesystem");
            AssetScanStatus = "Discovering fresh assets from filesystem...";
            
            // Get common asset directories for discovery
            var commonDirectories = GetCommonAssetDirectories().ToList();
            
            if (commonDirectories.Any())
            {
                var discoveryResult = await _purificationService.DiscoverAndReconcileAssetsAsync(commonDirectories);
                
                if (discoveryResult.Success)
                {
                    _logger.LogInformation("FRESH DISCOVERY COMPLETE: Registered {NewAssets} new assets from {FilesFound} files",
                        discoveryResult.NewAssetsRegistered, discoveryResult.FilesDiscovered);
                    
                    // Refresh UI with newly discovered assets
                    await LoadAllAssetsAsync();
                    
                    AssetScanStatus = $"Fresh discovery: {discoveryResult.NewAssetsRegistered} assets registered";
                }
                else
                {
                    _logger.LogWarning("FRESH DISCOVERY FAILED: {ErrorMessage}", discoveryResult.ErrorMessage);
                    AssetScanStatus = "Fresh discovery failed - manual asset registration may be required";
                }
            }
            else
            {
                _logger.LogInformation("No common asset directories found for fresh discovery");
                AssetScanStatus = "No asset directories found - add assets manually";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during fresh asset discovery");
            AssetScanStatus = "Fresh discovery error";
        }
    }

    /// <summary>
    /// Gets list of common directories where assets might be stored
    /// </summary>
    private IEnumerable<string> GetCommonAssetDirectories()
    {
        var directories = new List<string>();
        
        try
        {
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            var candidates = new[]
            {
                Path.Combine(userProfile, "AppData", "Local", "LM Studio", "models"),
                Path.Combine(appData, "LM Studio", "models"),
                Path.Combine(localAppData, "LM Studio", "models"),
                Path.Combine(userProfile, "Downloads"),
                Path.Combine(userProfile, "models"),
                Path.Combine(userProfile, "llm-models"),
                _directoryService.GetDirectoryPath(DirectoryType.BaseModels),
                _directoryService.GetDirectoryPath(DirectoryType.LoRAAdapters),
                _directoryService.GetDirectoryPath(DirectoryType.Embeddings),
                _directoryService.GetDirectoryPath(DirectoryType.Tokenizers),
                "D:\\models",
                "C:\\models"
            };

            directories.AddRange(candidates.Where(Directory.Exists));
            _logger.LogDebug("Found {DirectoryCount} existing asset directories for discovery", directories.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to detect common asset directories");
        }

        return directories.Distinct();
    }




    /// <summary>
    /// Loads all assets from the asset keeper service and categorizes them into dropdowns
    /// </summary>
    private async Task LoadAllAssetsAsync()
    {
        try
        {
            _logger.LogInformation("[ASSET.KEEPER DIAGNOSTIC] Starting LoadAllAssetsAsync");
            SetBusyState(true, "Loading assets...");
            AssetScanStatus = "Loading assets...";
            
            var allAssets = await _assetKeeperService.GetAllAssetsAsync();
            var assetsList = allAssets.ToList();
            
            _logger.LogInformation("[ASSET.KEEPER DIAGNOSTIC] Retrieved {AssetCount} assets from service", assetsList.Count);
            
            if (assetsList.Any())
            {
                foreach (var asset in assetsList.Take(5)) // Log first 5 assets for diagnosis
                {
                    _logger.LogDebug("[ASSET.KEEPER DIAGNOSTIC] Asset from service: {AssetName} ({AssetType}) at {FilePath}", 
                        asset.Name, asset.AssetType, asset.FilePath);
                }
                
                if (assetsList.Count > 5)
                {
                    _logger.LogDebug("[ASSET.KEEPER DIAGNOSTIC] ... and {RemainingCount} more assets", assetsList.Count - 5);
                }
            }
            
            // NAVIGATION-SAFE: Only update collections if we have new data or if collections are empty
            var shouldUpdate = !assetsList.Any() || 
                              AllAssets.Count == 0 || 
                              assetsList.Count != AllAssets.Count ||
                              !AllAssets.Select(a => a.Id).SequenceEqual(assetsList.Select(a => a.Id));
            
            if (shouldUpdate)
            {
                // AGGRESSIVE UI BINDING ENFORCEMENT: Force all collection operations on UI thread
                await ForceCollectionUpdateOnUIThreadAsync(assetsList);
                _logger.LogInformation("[NAVIGATION PRESERVATION] Collection updated with {AssetCount} assets", assetsList.Count);
            }
            else
            {
                _logger.LogDebug("[NAVIGATION PRESERVATION] Collections preserved - no update needed (Current: {CurrentCount}, Service: {ServiceCount})", AllAssets.Count, assetsList.Count);
            }
            
            StatusMessage = $"Loaded {assetsList.Count} asset(s)";
            AssetScanStatus = $"Loaded {assetsList.Count} assets";
            
            // Initial AppData scan if collections are empty
            if (!assetsList.Any() && !_hasLoadedAssets)
            {
                _logger.LogInformation("[ASSET.KEEPER DIAGNOSTIC] No assets found, triggering AppData scan");
                await ScanAppDataDirectoriesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ASSET.KEEPER DIAGNOSTIC] Error loading assets into dropdowns");
            StatusMessage = $"Error loading assets: {ex.Message}";
            AssetScanStatus = "Load failed";
        }
        finally
        {
            SetBusyState(false);
        }
    }

    /// <summary>
    /// Scans AppData directories for assets and registers them
    /// </summary>
    private async Task ScanAppDataDirectoriesAsync()
    {
        try
        {
            _logger.LogInformation("Scanning AppData directories for assets");
            AssetScanStatus = "Scanning AppData directories...";
            
            var totalFound = 0;
            
            // Scan each AppData subdirectory based on DirectoryType
            var directoriesToScan = new[]
            {
                DirectoryType.BaseModels,
                DirectoryType.LoRAAdapters, 
                DirectoryType.Embeddings,
                DirectoryType.Tokenizers
            };
            
            foreach (var directoryType in directoriesToScan)
            {
                try
                {
                    var directoryPath = _directoryService.GetDirectoryPath(directoryType);
                    if (Directory.Exists(directoryPath))
                    {
                        var found = await _assetKeeperService.ScanAndRegisterModelsAsync(directoryPath, true);
                        totalFound += found;
                        _logger.LogDebug("Found {Count} assets in {Directory}", found, directoryType);
                    }
                    else
                    {
                        _logger.LogDebug("Directory does not exist: {Directory}", directoryPath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error scanning directory: {DirectoryType}", directoryType);
                }
            }
            
            if (totalFound > 0)
            {
                await LoadAllAssetsAsync();
            }
            
            AssetScanStatus = totalFound > 0 ? $"Found {totalFound} new assets" : "No new assets found";
            _logger.LogInformation("AppData scan completed. Found {TotalFound} new assets", totalFound);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scanning AppData directories");
            AssetScanStatus = "AppData scan failed";
        }
    }

    /// <summary>
    /// Forces complete collection update on UI thread with aggressive binding enforcement
    /// </summary>
    /// <param name="assetsList">The list of assets to populate</param>
    private async Task ForceCollectionUpdateOnUIThreadAsync(List<LlmAsset> assetsList)
    {
        if (App.Current?.Dispatcher?.CheckAccess() == true)
        {
            await ForceCollectionUpdateCoreAsync(assetsList);
        }
        else if (App.Current?.Dispatcher != null)
        {
            await App.Current.Dispatcher.InvokeAsync(async () => 
            {
                await ForceCollectionUpdateCoreAsync(assetsList);
            });
        }
    }

    /// <summary>
    /// Core collection update logic with binding enforcement
    /// </summary>
    /// <param name="assetsList">The list of assets to populate</param>
    private async Task ForceCollectionUpdateCoreAsync(List<LlmAsset> assetsList)
    {
        _logger.LogInformation("[UI BINDING ENFORCEMENT] Starting aggressive collection update on UI thread");
        _logger.LogDebug("[UI BINDING ENFORCEMENT] Thread ID: {ThreadId}, IsUIThread: {IsUIThread}", 
            Thread.CurrentThread.ManagedThreadId, App.Current?.Dispatcher.CheckAccess());
        
        // Clear AVAILABLE collections only - preserve SELECTED collections during refresh
        _logger.LogDebug("[UI BINDING ENFORCEMENT] Clearing dropdown source collections (preserving user selections)");
        BaseModels.Clear();
        LoRAAdapters.Clear();
        Embeddings.Clear();
        Tokenizers.Clear();
        AllAssets.Clear();
        // CRITICAL FIX: Do NOT clear SelectedAssets - this is user-driven selection only
        // SelectedAssets persists across refreshes to maintain user configuration state
        SelectableEmbeddings.Clear();
        SelectableLoRAAdapters.Clear();
        
        // Force PropertyChanged notifications after clearing
        OnPropertyChanged(nameof(BaseModels));
        OnPropertyChanged(nameof(LoRAAdapters));
        OnPropertyChanged(nameof(Embeddings));
        OnPropertyChanged(nameof(Tokenizers));
        OnPropertyChanged(nameof(AllAssets));
        OnPropertyChanged(nameof(SelectableEmbeddings));
        OnPropertyChanged(nameof(SelectableLoRAAdapters));
        
        _logger.LogDebug("[UI BINDING ENFORCEMENT] Collections cleared, populating with {AssetCount} assets", assetsList.Count);
        
        // Populate collections with aggressive binding enforcement
        foreach (var asset in assetsList.OrderBy(a => a.Name))
        {
            AllAssets.Add(asset);
            AddAssetToAppropriateCollectionWithUIBinding(asset);
            
            // Yield to UI thread periodically for responsiveness
            if (assetsList.Count > 20)
            {
                await Task.Delay(1); // Allow UI thread to process
            }
        }
        
        // Populate selectable collections for checkbox UI
        PopulateSelectableCollections();

        // Force final PropertyChanged notifications
        OnPropertyChanged(nameof(BaseModels));
        OnPropertyChanged(nameof(LoRAAdapters));
        OnPropertyChanged(nameof(Embeddings));
        OnPropertyChanged(nameof(Tokenizers));
        OnPropertyChanged(nameof(AllAssets));
        OnPropertyChanged(nameof(SelectableEmbeddings));
        OnPropertyChanged(nameof(SelectableLoRAAdapters));
        
        _logger.LogInformation("[UI BINDING ENFORCEMENT] Collection update completed - BaseModels: {BaseCount}, LoRA: {LoRACount}, Embeddings: {EmbeddingCount}, Tokenizers: {TokenizerCount}, AllAssets: {AllCount}", 
            BaseModels.Count, LoRAAdapters.Count, Embeddings.Count, Tokenizers.Count, AllAssets.Count);
    }

    /// <summary>
    /// Populates selectable collections for checkbox UI from the standard collections
    /// CRITICAL FIX: Preserves existing selection state during dropdown refresh
    /// </summary>
    private void PopulateSelectableCollections()
    {
        _logger.LogDebug("[UI BINDING ENFORCEMENT] Populating selectable wrapper collections with selection state preservation");

        // Populate selectable embeddings with preserved selection state
        foreach (var embedding in Embeddings)
        {
            var wrapper = new SelectableAssetWrapper(embedding);
            
            // CRITICAL FIX: Preserve existing selection state across refreshes
            // Check multiple selection sources to ensure consistency
            wrapper.IsSelected = SelectedAssets.Contains(embedding) || 
                               SelectedEmbeddings.Contains(embedding);
            
            // Wire up selection change event for automatic synchronization
            wrapper.SelectionChanged += (sender, isSelected) =>
            {
                if (sender is SelectableAssetWrapper w)
                {
                    ExecuteToggleEmbeddingSelection(w);
                }
            };
            
            SelectableEmbeddings.Add(wrapper);
            
            _logger.LogTrace("[SELECTION PRESERVATION] Embedding {AssetName} selection state: {IsSelected}", 
                embedding.Name, wrapper.IsSelected);
        }

        // Populate selectable LoRA adapters with preserved selection state
        foreach (var loraAdapter in LoRAAdapters)
        {
            var wrapper = new SelectableAssetWrapper(loraAdapter);
            
            // CRITICAL FIX: Preserve existing selection state across refreshes
            // Check multiple selection sources to ensure consistency
            wrapper.IsSelected = SelectedAssets.Contains(loraAdapter) || 
                               SelectedLoRAAdapters.Contains(loraAdapter);
            
            // Wire up selection change event for automatic synchronization
            wrapper.SelectionChanged += (sender, isSelected) =>
            {
                if (sender is SelectableAssetWrapper w)
                {
                    ExecuteToggleLoRAAdapterSelection(w);
                }
            };
            
            SelectableLoRAAdapters.Add(wrapper);
            
            _logger.LogTrace("[SELECTION PRESERVATION] LoRA adapter {AssetName} selection state: {IsSelected}", 
                loraAdapter.Name, wrapper.IsSelected);
        }

        _logger.LogDebug("[UI BINDING ENFORCEMENT] Populated {EmbeddingCount} selectable embeddings, {LoRACount} selectable LoRA adapters with preserved selections", 
            SelectableEmbeddings.Count, SelectableLoRAAdapters.Count);
    }

    /// <summary>
    /// Adds an asset to the appropriate collection based on its type with UI binding enforcement
    /// </summary>
    /// <param name="asset">The asset to add</param>
    private void AddAssetToAppropriateCollectionWithUIBinding(LlmAsset asset)
    {
        _logger.LogDebug("[UI BINDING ENFORCEMENT] Categorizing asset: {AssetName} (Type: {AssetType}, Architecture: {Architecture})", 
            asset.Name, asset.AssetType, asset.Architecture);
        
        switch (asset.AssetType)
        {
            case LlmAssetType.BaseModel:
                BaseModels.Add(asset);
                _logger.LogDebug("[UI BINDING ENFORCEMENT] Added to BaseModels: {AssetName} (Collection Count: {Count})", asset.Name, BaseModels.Count);
                break;
            case LlmAssetType.LoRAAdapter:
                LoRAAdapters.Add(asset);
                _logger.LogDebug("[UI BINDING ENFORCEMENT] Added to LoRAAdapters: {AssetName} (Collection Count: {Count})", asset.Name, LoRAAdapters.Count);
                break;
            case LlmAssetType.Tokenizer:
                Tokenizers.Add(asset);
                _logger.LogDebug("[UI BINDING ENFORCEMENT] Added to Tokenizers: {AssetName} (Collection Count: {Count})", asset.Name, Tokenizers.Count);
                break;
            default:
                if (IsEmbeddingModel(asset))
                {
                    Embeddings.Add(asset);
                    _logger.LogDebug("[UI BINDING ENFORCEMENT] Added to Embeddings: {AssetName} (Collection Count: {Count})", asset.Name, Embeddings.Count);
                }
                else
                {
                    _logger.LogDebug("[UI BINDING ENFORCEMENT] Asset not categorized (unknown type): {AssetName} (Type: {AssetType})", 
                        asset.Name, asset.AssetType);
                }
                break;
        }
    }

    /// <summary>
    /// Adds an asset to the appropriate collection based on its type
    /// </summary>
    /// <param name="asset">The asset to add</param>
    private void AddAssetToAppropriateCollection(LlmAsset asset)
    {
        _logger.LogDebug("[DROPDOWN DIAGNOSTIC] Categorizing asset: {AssetName} (Type: {AssetType}, Architecture: {Architecture})", 
            asset.Name, asset.AssetType, asset.Architecture);
        
        switch (asset.AssetType)
        {
            case LlmAssetType.BaseModel:
                BaseModels.Add(asset);
                _logger.LogDebug("[DROPDOWN DIAGNOSTIC] Added to BaseModels: {AssetName}", asset.Name);
                break;
            case LlmAssetType.LoRAAdapter:
                LoRAAdapters.Add(asset);
                _logger.LogDebug("[DROPDOWN DIAGNOSTIC] Added to LoRAAdapters: {AssetName}", asset.Name);
                break;
            case LlmAssetType.Tokenizer:
                Tokenizers.Add(asset);
                _logger.LogDebug("[DROPDOWN DIAGNOSTIC] Added to Tokenizers: {AssetName}", asset.Name);
                break;
            // Embeddings are typically stored as base models with specific architecture
            default:
                if (IsEmbeddingModel(asset))
                {
                    Embeddings.Add(asset);
                    _logger.LogDebug("[DROPDOWN DIAGNOSTIC] Added to Embeddings: {AssetName}", asset.Name);
                }
                else
                {
                    _logger.LogDebug("[DROPDOWN DIAGNOSTIC] Asset not categorized (unknown type): {AssetName} (Type: {AssetType})", 
                        asset.Name, asset.AssetType);
                }
                break;
        }
    }

    /// <summary>
    /// Removes an asset from all collections with UI binding enforcement
    /// </summary>
    /// <param name="asset">The asset to remove</param>
    private void RemoveAssetFromCollections(LlmAsset asset)
    {
        if (App.Current?.Dispatcher.CheckAccess() == true)
        {
            RemoveAssetFromCollectionsCore(asset);
        }
        else
        {
            App.Current?.Dispatcher.Invoke(() => RemoveAssetFromCollectionsCore(asset));
        }
    }

    /// <summary>
    /// Core asset removal logic - ONLY removes from SELECTED collections, preserves dropdown sources
    /// CRITICAL FIX: Separate selection operations from dropdown source data integrity
    /// </summary>
    /// <param name="asset">The asset to remove from selections ONLY</param>
    private void RemoveAssetFromCollectionsCore(LlmAsset asset)
    {
        _logger.LogDebug("[SELECTION REMOVAL] Removing asset from SELECTION collections only: {AssetName}", asset.Name);
        
        // CRITICAL FIX: Only remove from SelectedAssets - DO NOT touch dropdown source collections
        // BaseModels, LoRAAdapters, Embeddings, Tokenizers, AllAssets remain UNTOUCHED
        SelectedAssets.Remove(asset);

        // Update selectable wrapper selection state but keep wrappers in dropdown
        var embeddingWrapper = SelectableEmbeddings.FirstOrDefault(w => w.Asset == asset);
        if (embeddingWrapper != null)
        {
            embeddingWrapper.IsSelected = false; // Uncheck checkbox but keep in dropdown
            _logger.LogDebug("[SELECTION REMOVAL] Unchecked embedding wrapper for {AssetName}", asset.Name);
        }

        var loraWrapper = SelectableLoRAAdapters.FirstOrDefault(w => w.Asset == asset);
        if (loraWrapper != null)
        {
            loraWrapper.IsSelected = false; // Uncheck checkbox but keep in dropdown
            _logger.LogDebug("[SELECTION REMOVAL] Unchecked LoRA wrapper for {AssetName}", asset.Name);
        }

        // Clear individual dropdown selections if the removed asset was selected
        if (SelectedBaseModel == asset) 
        {
            SelectedBaseModel = null;
            _logger.LogDebug("[SELECTION REMOVAL] Cleared base model selection for {AssetName}", asset.Name);
        }
        if (SelectedLoRAAdapter == asset) 
        {
            SelectedLoRAAdapter = null;
            _logger.LogDebug("[SELECTION REMOVAL] Cleared LoRA selection for {AssetName}", asset.Name);
        }
        if (SelectedEmbedding == asset) 
        {
            SelectedEmbedding = null;
            _logger.LogDebug("[SELECTION REMOVAL] Cleared embedding selection for {AssetName}", asset.Name);
        }
        if (SelectedTokenizer == asset) 
        {
            SelectedTokenizer = null;
            _logger.LogDebug("[SELECTION REMOVAL] Cleared tokenizer selection for {AssetName}", asset.Name);
        }
        if (SelectedAsset == asset) SelectedAsset = null;
        
        // Remove from multi-select collections
        SelectedEmbeddings.Remove(asset);
        SelectedLoRAAdapters.Remove(asset);
        
        // Update selection summaries for multi-select UI
        OnPropertyChanged(nameof(EmbeddingsSelectionSummary));
        OnPropertyChanged(nameof(LoRAAdaptersSelectionSummary));
        
        // DO NOT update dropdown source collections - they remain intact for future selection
        // Only notify that the SelectedAssets table has changed
        OnPropertyChanged(nameof(SelectedAssets));
        
        _logger.LogDebug("[SELECTION REMOVAL] Asset removed from selections only - dropdown sources preserved. SelectedAssets count: {Count}", SelectedAssets.Count);
    }

    /// <summary>
    /// Determines if an asset is an embedding model
    /// </summary>
    /// <param name="asset">The asset to check</param>
    /// <returns>True if the asset is an embedding model</returns>
    private static bool IsEmbeddingModel(LlmAsset asset)
    {
        // Primary classification: Use the AssetType.Embedding from folder-based classification
        if (asset.AssetType == LlmAssetType.Embedding)
        {
            return true;
        }
        
        // Fallback for legacy assets that may not have been reclassified yet
        var name = asset.Name.ToLowerInvariant();
        var architecture = asset.Architecture?.ToLowerInvariant() ?? "";
        
        return name.Contains("embedding") || 
               name.Contains("embed") ||
               architecture.Contains("embedding") ||
               architecture.Contains("e5") ||
               architecture.Contains("bge") ||
               architecture.Contains("gte");
    }

    /// <summary>
    /// Validates compatibility between selected assets
    /// </summary>
    private void ValidateAssetCompatibility()
    {
        HasCompatibilityWarning = false;
        CompatibilityMessage = string.Empty;
        CanLoadConfiguration = false;

        try
        {
            var warnings = new List<string>();

            // Basic validation - at least base model should be selected
            if (SelectedBaseModel == null)
            {
                warnings.Add("No base model selected");
                HasCompatibilityWarning = true;
                CompatibilityMessage = string.Join("; ", warnings);
                return;
            }

            // LoRA compatibility validation (now multi-select)
            if (SelectedLoRAAdapters.Any() && SelectedBaseModel != null)
            {
                foreach (var loraAdapter in SelectedLoRAAdapters)
                {
                    if (!string.IsNullOrEmpty(SelectedBaseModel.Architecture) && 
                        !string.IsNullOrEmpty(loraAdapter.Architecture))
                    {
                        if (SelectedBaseModel.Architecture != loraAdapter.Architecture)
                        {
                            warnings.Add($"LoRA adapter '{loraAdapter.Name}' architecture ({loraAdapter.Architecture}) may not match base model ({SelectedBaseModel.Architecture})");
                        }
                    }
                }
            }

            // VRAM estimation
            var totalVramEstimate = GetTotalVramEstimate();
            if (totalVramEstimate > 24) // Assuming 24GB as a reasonable upper limit
            {
                warnings.Add($"Total VRAM requirement ({totalVramEstimate:F1}GB) may exceed available memory");
            }

            // Tokenizer compatibility
            if (SelectedTokenizer != null && SelectedBaseModel != null)
            {
                if (!string.IsNullOrEmpty(SelectedTokenizer.Architecture) && 
                    !string.IsNullOrEmpty(SelectedBaseModel.Architecture))
                {
                    if (SelectedTokenizer.Architecture != SelectedBaseModel.Architecture)
                    {
                        warnings.Add($"Tokenizer architecture may not match base model");
                    }
                }
            }

            HasCompatibilityWarning = warnings.Any();
            CompatibilityMessage = HasCompatibilityWarning ? string.Join("; ", warnings) : "Asset selection is compatible";
            CanLoadConfiguration = SelectedBaseModel != null; // Allow loading with just base model

            _logger.LogDebug("Asset compatibility validation completed. Warnings: {WarningCount}", warnings.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during compatibility validation");
            HasCompatibilityWarning = true;
            CompatibilityMessage = "Error validating asset compatibility";
        }
    }

    /// <summary>
    /// Gets the total estimated VRAM requirement for selected assets
    /// </summary>
    /// <returns>Total VRAM estimate in GB</returns>
    private decimal GetTotalVramEstimate()
    {
        decimal total = 0;
        
        if (SelectedBaseModel?.VramEstimateGb.HasValue == true)
            total += SelectedBaseModel.VramEstimateGb.Value;
            
        // Multi-select embeddings
        foreach (var embedding in SelectedEmbeddings)
        {
            if (embedding.VramEstimateGb.HasValue)
                total += embedding.VramEstimateGb.Value;
        }
            
        // Multi-select LoRA adapters - typically add minimal VRAM overhead each
        total += SelectedLoRAAdapters.Count * 0.5m; // Estimate 0.5GB for each LoRA overhead
            
        return total;
    }

    /// <summary>
    /// Handles specific asset file changes detected by FileSystemWatcher
    /// </summary>
    /// <param name="changeType">The type of change that occurred</param>
    /// <param name="filePath">The file path that changed</param>
    private async Task HandleAssetFileChangeAsync(WatcherChangeTypes changeType, string filePath)
    {
        try
        {
            switch (changeType)
            {
                case WatcherChangeTypes.Created:
                    _logger.LogInformation("ASSET.KEEPER: New asset file detected: {FilePath}", filePath);
                    var newAsset = await _assetKeeperService.RegisterModelAsync(filePath);
                    if (newAsset != null)
                    {
                        // Force UI thread addition
                        AddAssetToAppropriateCollectionWithUIBinding(newAsset);
                        AllAssets.Add(newAsset);
                        OnPropertyChanged(nameof(BaseModels));
                        OnPropertyChanged(nameof(LoRAAdapters));
                        OnPropertyChanged(nameof(Embeddings));
                        OnPropertyChanged(nameof(Tokenizers));
                        OnPropertyChanged(nameof(AllAssets));
                        AssetScanStatus = $"New asset registered: {newAsset.Name}";
                        StatusMessage = $"Auto-registered new asset: {newAsset.Name}";
                    }
                    break;

                case WatcherChangeTypes.Deleted:
                    _logger.LogInformation("ASSET.KEEPER: Asset file deleted: {FilePath}", filePath);
                    var existingAsset = AllAssets.FirstOrDefault(a => string.Equals(a.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
                    if (existingAsset != null)
                    {
                        await _assetKeeperService.UpdateAssetStatusAsync(existingAsset.Id, LlmAssetStatus.Missing);
                        RemoveAssetFromCollections(existingAsset);
                        AssetScanStatus = $"Asset marked as missing: {existingAsset.Name}";
                        StatusMessage = $"Asset file removed: {existingAsset.Name}";
                    }
                    break;

                case WatcherChangeTypes.Changed:
                    _logger.LogDebug("ASSET.KEEPER: Asset file modified: {FilePath}", filePath);
                    var modifiedAsset = AllAssets.FirstOrDefault(a => string.Equals(a.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
                    if (modifiedAsset != null)
                    {
                        // Re-validate the modified asset
                        await _assetKeeperService.ValidateAssetAsync(modifiedAsset.Id);
                        await LoadAllAssetsAsync(); // Refresh to get updated status
                        AssetScanStatus = $"Asset validated: {modifiedAsset.Name}";
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ASSET.KEEPER: Error handling asset file change: {ChangeType} - {FilePath}", changeType, filePath);
        }
    }

    /// <summary>
    /// Refreshes assets for a specific directory path (legacy method for compatibility)
    /// </summary>
    /// <param name="filePath">The file path that changed</param>
    private async Task RefreshSpecificDirectoryAsync(string filePath)
    {
        try
        {
            var directory = Path.GetDirectoryName(filePath);
            if (string.IsNullOrEmpty(directory)) return;

            _logger.LogDebug("ASSET.KEEPER: Refreshing assets for directory: {Directory}", directory);
            
            var newAssetsCount = await _assetKeeperService.ScanAndRegisterModelsAsync(directory, false);
            if (newAssetsCount > 0)
            {
                await LoadAllAssetsAsync();
                AssetScanStatus = $"Auto-detected {newAssetsCount} new assets";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ASSET.KEEPER: Error refreshing specific directory: {FilePath}", filePath);
        }
    }

    /// <summary>
    /// Disposes of resources used by the ModelConfigurationViewModel
    /// SINGLETON-SAFE: Only dispose if initialized to prevent issues with singleton lifecycle
    /// Collections are preserved for navigation state persistence
    /// </summary>
    protected override void DisposeResources()
    {
        if (_isInitialized)
        {
            // Dispose FileSystemWatcher properly to prevent memory leaks
            if (_fileWatcher != null)
            {
                _fileWatcher.EnableRaisingEvents = false;
                _fileWatcher.Created -= OnAssetFileSystemChanged;
                _fileWatcher.Deleted -= OnAssetFileSystemChanged;
                _fileWatcher.Renamed -= OnAssetFileSystemRenamed;
                _fileWatcher.Error -= OnFileSystemWatcherError;
                _fileWatcher.Dispose();
                _fileWatcher = null;
            }

            // SINGLETON-SAFE: DO NOT clear collections to preserve navigation state
            // ObservableCollections will be retained for state persistence across navigation
            _logger.LogDebug("ModelConfigurationViewModel FileSystemWatcher disposed (Collections preserved for navigation)");
        }

        base.DisposeResources();
    }

    /// <summary>
    /// Executes the toggle embedding selection command
    /// </summary>
    /// <param name="asset">The asset to toggle selection for</param>
    private void ExecuteToggleEmbeddingSelection(LlmAsset? asset)
    {
        if (asset == null) return;
        
        if (SelectedEmbeddings.Contains(asset))
        {
            SelectedEmbeddings.Remove(asset);
            _logger.LogDebug("UX.COPILOT: Removed embedding from selection: {AssetName}", asset.Name);
        }
        else
        {
            SelectedEmbeddings.Add(asset);
            _logger.LogDebug("UX.COPILOT: Added embedding to selection: {AssetName}", asset.Name);
        }
        
        // Notify UI of selection summary changes
        OnPropertyChanged(nameof(EmbeddingsSelectionSummary));
        ValidateAssetCompatibility();
        
        StatusMessage = $"Embedding selection updated: {EmbeddingsSelectionSummary}";
    }

    /// <summary>
    /// Executes the toggle LoRA adapter selection command
    /// </summary>
    /// <param name="asset">The asset to toggle selection for</param>
    private void ExecuteToggleLoRAAdapterSelection(LlmAsset? asset)
    {
        if (asset == null) return;
        
        if (SelectedLoRAAdapters.Contains(asset))
        {
            SelectedLoRAAdapters.Remove(asset);
            _logger.LogDebug("UX.COPILOT: Removed LoRA adapter from selection: {AssetName}", asset.Name);
        }
        else
        {
            SelectedLoRAAdapters.Add(asset);
            _logger.LogDebug("UX.COPILOT: Added LoRA adapter to selection: {AssetName}", asset.Name);
        }
        
        // Notify UI of selection summary changes
        OnPropertyChanged(nameof(LoRAAdaptersSelectionSummary));
        ValidateAssetCompatibility();
        
        StatusMessage = $"LoRA adapter selection updated: {LoRAAdaptersSelectionSummary}";
    }

    /// <summary>
    /// Executes the clear all selections command
    /// </summary>
    private void ExecuteClearAllSelections()
    {
        _logger.LogInformation("UX.COPILOT: User initiated clear all selections");
        
        // Clear individual selections
        SelectedBaseModel = null;
        SelectedTokenizer = null;
        SelectedAsset = null;
        
        // Clear multi-select collections
        var embeddingCount = SelectedEmbeddings.Count;
        var loraCount = SelectedLoRAAdapters.Count;
        
        SelectedEmbeddings.Clear();
        SelectedLoRAAdapters.Clear();
        
        // Critical Fix: Clear the SelectedAssets collection and uncheck all checkboxes
        SelectedAssets.Clear();
        
        // Uncheck all embedding checkboxes
        foreach (var wrapper in SelectableEmbeddings)
        {
            wrapper.IsSelected = false;
        }
        
        // Uncheck all LoRA adapter checkboxes
        foreach (var wrapper in SelectableLoRAAdapters)
        {
            wrapper.IsSelected = false;
        }
        
        // Update UI
        OnPropertyChanged(nameof(EmbeddingsSelectionSummary));
        OnPropertyChanged(nameof(LoRAAdaptersSelectionSummary));
        ValidateAssetCompatibility();
        
        StatusMessage = $"All selections cleared (removed {embeddingCount} embeddings, {loraCount} LoRA adapters)";
        AssetScanStatus = "Selections cleared - ready for new configuration";
        
        _logger.LogInformation("UX.COPILOT: All selections cleared - {EmbeddingCount} embeddings, {LoRACount} LoRA adapters", 
            embeddingCount, loraCount);
    }
}