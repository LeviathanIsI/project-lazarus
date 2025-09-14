using System.Windows.Input;
using Lazarus.Shared.Settings;
using Microsoft.Win32;
using Lazarus.Desktop.Services;
using System.Collections.ObjectModel;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;

namespace Lazarus.Desktop.ViewModels;

/// <summary>
/// ViewModel for General settings section
/// </summary>
public class GeneralSettingsViewModel : SettingsSectionBase
{
    private string _defaultModel = "";
    private bool _autoSaveConversations;
    private bool _autoUpdateCheck;
    private string _language = "English";
    private string _theme = "Dark";
    private bool _startWithWindows;
    private bool _startMinimized;
    private bool _restoreLastSession;
    private bool _autoLoadModel;
    private int _autoSaveInterval;
    private int _historyLimit;
    private bool _autoDownloadUpdates;
    private bool _sendAnonymousUsage;
    private bool _sendCrashReports;
    private string _userName = "You";
    private string _assistantName = "Assistant";
    private string _systemPrompt = string.Empty;

    public GeneralSettingsViewModel(SettingsViewModel settings) : base(settings, "General")
    {
        SectionDescription = "Basic application settings and preferences";

        // Initialize with default values immediately
        ResetToDefault();
        CompleteInitialization();
    }

    public string DefaultModel
    {
        get => _defaultModel;
        set { if (SetProperty(ref _defaultModel, value)) MarkAsChanged(); }
    }

    public bool AutoSaveConversations
    {
        get => _autoSaveConversations;
        set { if (SetProperty(ref _autoSaveConversations, value)) MarkAsChanged(); }
    }

    public bool AutoUpdateCheck
    {
        get => _autoUpdateCheck;
        set { if (SetProperty(ref _autoUpdateCheck, value)) MarkAsChanged(); }
    }

    public string Language
    {
        get => _language;
        set { if (SetProperty(ref _language, value)) MarkAsChanged(); }
    }

    public string Theme
    {
        get => _theme;
        set { if (SetProperty(ref _theme, value)) MarkAsChanged(); }
    }

    public bool StartWithWindows
    {
        get => _startWithWindows;
        set { if (SetProperty(ref _startWithWindows, value)) MarkAsChanged(); }
    }

    public bool StartMinimized
    {
        get => _startMinimized;
        set { if (SetProperty(ref _startMinimized, value)) MarkAsChanged(); }
    }

    public bool RestoreLastSession
    {
        get => _restoreLastSession;
        set { if (SetProperty(ref _restoreLastSession, value)) MarkAsChanged(); }
    }

    public bool AutoLoadModel
    {
        get => _autoLoadModel;
        set { if (SetProperty(ref _autoLoadModel, value)) MarkAsChanged(); }
    }

    public int AutoSaveInterval
    {
        get => _autoSaveInterval;
        set { if (SetProperty(ref _autoSaveInterval, value)) MarkAsChanged(); }
    }

    public int HistoryLimit
    {
        get => _historyLimit;
        set { if (SetProperty(ref _historyLimit, value)) MarkAsChanged(); }
    }

    public bool AutoDownloadUpdates
    {
        get => _autoDownloadUpdates;
        set { if (SetProperty(ref _autoDownloadUpdates, value)) MarkAsChanged(); }
    }

    public bool SendAnonymousUsage
    {
        get => _sendAnonymousUsage;
        set { if (SetProperty(ref _sendAnonymousUsage, value)) MarkAsChanged(); }
    }

    public bool SendCrashReports
    {
        get => _sendCrashReports;
        set { if (SetProperty(ref _sendCrashReports, value)) MarkAsChanged(); }
    }

    // Chat persona fields
    public string UserName
    {
        get => _userName;
        set { if (SetProperty(ref _userName, value?.Trim() ?? "You")) MarkAsChanged(); }
    }

    public string AssistantName
    {
        get => _assistantName;
        set { if (SetProperty(ref _assistantName, value?.Trim() ?? "Assistant")) MarkAsChanged(); }
    }

    public string SystemPrompt
    {
        get => _systemPrompt;
        set { if (SetProperty(ref _systemPrompt, value ?? string.Empty)) MarkAsChanged(); }
    }

    public override void RefreshFromSettings()
    {
        var settings = ParentSettings.Settings;
        DefaultModel = settings.DefaultModel;
        AutoSaveConversations = settings.AutoSaveConversations;
        AutoUpdateCheck = settings.AutoUpdateCheck;
        Language = settings.Language;
        Theme = settings.UiTheme;
        StartWithWindows = settings.StartWithWindows;
        StartMinimized = settings.StartMinimized;
        RestoreLastSession = settings.RestoreLastSession;
        AutoLoadModel = settings.AutoLoadModel;
        AutoSaveInterval = settings.AutoSaveIntervalMinutes;
        HistoryLimit = settings.HistoryLimit;
        AutoDownloadUpdates = settings.AutoDownloadUpdates;
        SendAnonymousUsage = settings.SendAnonymousUsage;
        SendCrashReports = settings.SendCrashReports;
        UserName = settings.UserName;
        AssistantName = settings.AssistantName;
        SystemPrompt = settings.SystemPrompt;
    }

    public override async Task ApplySettingsAsync()
    {
        var settings = ParentSettings.Settings;
        settings.DefaultModel = DefaultModel;
        settings.AutoSaveConversations = AutoSaveConversations;
        settings.AutoUpdateCheck = AutoUpdateCheck;
        settings.Language = string.IsNullOrWhiteSpace(Language) ? "English" : Language.Trim();
        settings.UiTheme = string.IsNullOrWhiteSpace(Theme) ? "Dark" : Theme.Trim();
        settings.StartWithWindows = StartWithWindows;
        settings.StartMinimized = StartMinimized;
        settings.RestoreLastSession = RestoreLastSession;
        settings.AutoLoadModel = AutoLoadModel;
        settings.AutoSaveIntervalMinutes = Math.Max(1, AutoSaveInterval);
        settings.HistoryLimit = Math.Max(1, HistoryLimit);
        settings.AutoDownloadUpdates = AutoDownloadUpdates;
        settings.SendAnonymousUsage = SendAnonymousUsage;
        settings.SendCrashReports = SendCrashReports;
        settings.UserName = string.IsNullOrWhiteSpace(UserName) ? "You" : UserName.Trim();
        settings.AssistantName = string.IsNullOrWhiteSpace(AssistantName) ? "Assistant" : AssistantName.Trim();
        settings.SystemPrompt = SystemPrompt ?? string.Empty;
        await Task.CompletedTask;
    }

    protected override void ResetToDefault()
    {
        DefaultModel = "llama3.2";
        AutoSaveConversations = true;
        AutoUpdateCheck = true;
        Language = "English";
        Theme = "Dark";
        StartWithWindows = false;
        StartMinimized = false;
        RestoreLastSession = false;
        AutoLoadModel = false;
        AutoSaveInterval = 5;
        HistoryLimit = 200;
        AutoDownloadUpdates = false;
        SendAnonymousUsage = false;
        SendCrashReports = false;
        UserName = "You";
        AssistantName = "Assistant";
        SystemPrompt = string.Empty;
    }
}

/// <summary>
/// ViewModel for Paths settings section
/// </summary>
public class PathsSettingsViewModel : SettingsSectionBase
{
    private string _modelsDirectory = "";
    private string _cacheDirectory = "";
    private int _cacheMaxSizeMB;
    private string _tempFilesLocation = "";
    private string _exportPath = "";
    // Additional paths surfaced in XAML (not persisted yet)
    private string _downloadDirectory = "";
    private string _quantizedModelsDirectory = "";
    private string _databasePath = "";
    private string _conversationsDirectory = "";
    private string _backupDirectory = "";
    private string _importDirectory = "";
    private string _templatesDirectory = "";
    private string _logsDirectory = "";
    private string _pluginsDirectory = "";

    public PathsSettingsViewModel(SettingsViewModel settings) : base(settings, "Paths")
    {
        SectionDescription = "Configure directories and file locations";
        BrowseModelsCommand = new RelayCommand(() => BrowseFolder(path => ModelsDirectory = path));
        BrowseCacheCommand = new RelayCommand(() => BrowseFolder(path => CacheDirectory = path));
        BrowseTempCommand = new RelayCommand(() => BrowseFolder(path => TempDirectory = path));
        BrowseExportCommand = new RelayCommand(() => BrowseFolder(path => ExportDirectory = path));
        // Additional browse commands used by the XAML
        BrowseDownloadCommand = new RelayCommand(() => BrowseFolder(path => DownloadDirectory = path));
        BrowseQuantizedCommand = new RelayCommand(() => BrowseFolder(path => QuantizedModelsDirectory = path));
        BrowseDatabaseCommand = new RelayCommand(() => BrowseFolder(path => DatabasePath = path));
        BrowseConversationsCommand = new RelayCommand(() => BrowseFolder(path => ConversationsDirectory = path));
        BrowseBackupCommand = new RelayCommand(() => BrowseFolder(path => BackupDirectory = path));
        BrowseImportCommand = new RelayCommand(() => BrowseFolder(path => ImportDirectory = path));
        BrowseTemplatesCommand = new RelayCommand(() => BrowseFolder(path => TemplatesDirectory = path));
        BrowseLogsCommand = new RelayCommand(() => BrowseFolder(path => LogsDirectory = path));
        BrowsePluginsCommand = new RelayCommand(() => BrowseFolder(path => PluginsDirectory = path));

        // Initialize with default values immediately
        ResetToDefault();
        CompleteInitialization();
        //
    }

    public string ModelsDirectory
    {
        get => _modelsDirectory;
        set { if (SetProperty(ref _modelsDirectory, value)) MarkAsChanged(); }
    }

    public string CacheDirectory
    {
        get => _cacheDirectory;
        set { if (SetProperty(ref _cacheDirectory, value)) MarkAsChanged(); }
    }

    public int CacheMaxSizeMB
    {
        get => _cacheMaxSizeMB;
        set { if (SetProperty(ref _cacheMaxSizeMB, value)) MarkAsChanged(); }
    }

    public string TempFilesLocation
    {
        get => _tempFilesLocation;
        set { if (SetProperty(ref _tempFilesLocation, value)) MarkAsChanged(); }
    }

    public string ExportPath
    {
        get => _exportPath;
        set { if (SetProperty(ref _exportPath, value)) MarkAsChanged(); }
    }

    // Aliases/additional properties required by XAML
    public string TempDirectory
    {
        get => _tempFilesLocation;
        set { if (SetProperty(ref _tempFilesLocation, value)) MarkAsChanged(); }
    }

    public string DownloadDirectory
    {
        get => _downloadDirectory;
        set { if (SetProperty(ref _downloadDirectory, value)) MarkAsChanged(); }
    }

    public string QuantizedModelsDirectory
    {
        get => _quantizedModelsDirectory;
        set { if (SetProperty(ref _quantizedModelsDirectory, value)) MarkAsChanged(); }
    }

    public string DatabasePath
    {
        get => _databasePath;
        set { if (SetProperty(ref _databasePath, value)) MarkAsChanged(); }
    }

    public string ConversationsDirectory
    {
        get => _conversationsDirectory;
        set { if (SetProperty(ref _conversationsDirectory, value)) MarkAsChanged(); }
    }

    public string BackupDirectory
    {
        get => _backupDirectory;
        set { if (SetProperty(ref _backupDirectory, value)) MarkAsChanged(); }
    }

    public string ExportDirectory
    {
        get => _exportPath;
        set { if (SetProperty(ref _exportPath, value)) MarkAsChanged(); }
    }

    public string ImportDirectory
    {
        get => _importDirectory;
        set { if (SetProperty(ref _importDirectory, value)) MarkAsChanged(); }
    }

    public string TemplatesDirectory
    {
        get => _templatesDirectory;
        set { if (SetProperty(ref _templatesDirectory, value)) MarkAsChanged(); }
    }

    public string LogsDirectory
    {
        get => _logsDirectory;
        set { if (SetProperty(ref _logsDirectory, value)) MarkAsChanged(); }
    }

    public string PluginsDirectory
    {
        get => _pluginsDirectory;
        set { if (SetProperty(ref _pluginsDirectory, value)) MarkAsChanged(); }
    }

    public ICommand BrowseModelsCommand { get; }
    public ICommand BrowseCacheCommand { get; }
    public ICommand BrowseTempCommand { get; }
    public ICommand BrowseExportCommand { get; }
    public ICommand BrowseDownloadCommand { get; }
    public ICommand BrowseQuantizedCommand { get; }
    public ICommand BrowseDatabaseCommand { get; }
    public ICommand BrowseConversationsCommand { get; }
    public ICommand BrowseBackupCommand { get; }
    public ICommand BrowseImportCommand { get; }
    public ICommand BrowseTemplatesCommand { get; }
    public ICommand BrowseLogsCommand { get; }
    public ICommand BrowsePluginsCommand { get; }

    private void BrowseFolder(Action<string> setPath)
    {
        // Using Windows Forms folder dialog for folder selection
        // WPF doesn't have a native folder browser dialog
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Select Folder",
            FileName = "Folder Selection",
            Filter = "Directory|*.this.directory",
            CheckFileExists = false,
            CheckPathExists = true,
            RestoreDirectory = true
        };

        if (dialog.ShowDialog() == true)
        {
            var path = System.IO.Path.GetDirectoryName(dialog.FileName);
            if (!string.IsNullOrEmpty(path))
                setPath(path);
        }
    }

    public override void RefreshFromSettings()
    {
        var settings = ParentSettings.Settings;
        // Normalize existing values into AppData defaults when blank or pointing to old C:\Lazarus paths
        ModelsDirectory = NormalizePathToAppData(settings.ModelsDirectory, Lazarus.Shared.LazarusPaths.Models.RootDir);
        CacheDirectory = NormalizePathToAppData(settings.CacheDirectory, Lazarus.Shared.LazarusPaths.SystemData.Cache);
        CacheMaxSizeMB = settings.CacheMaxSizeMB;
        TempFilesLocation = NormalizePathToAppData(settings.TempFilesLocation, Lazarus.Shared.LazarusPaths.SystemData.Cache);
        ExportPath = NormalizePathToAppData(settings.ExportPath, Path.Combine(Lazarus.Shared.LazarusPaths.SharedResources.ImportExport, "Export"));

        // Populate non-persisted fields with sensible defaults
        DownloadDirectory = System.IO.Path.Combine(Lazarus.Shared.LazarusPaths.SystemData.Cache, "Downloads");
        QuantizedModelsDirectory = System.IO.Path.Combine(Lazarus.Shared.LazarusPaths.Models.RootDir, "Quantized");
        DatabasePath = Lazarus.Shared.LazarusPaths.DatabaseFile;
        ConversationsDirectory = System.IO.Path.Combine(Lazarus.Shared.LazarusPaths.Root, "Conversations");
        BackupDirectory = System.IO.Path.Combine(Lazarus.Shared.LazarusPaths.Root, "Backups");
        ImportDirectory = System.IO.Path.Combine(Lazarus.Shared.LazarusPaths.SharedResources.ImportExport, "Import");
        TemplatesDirectory = System.IO.Path.Combine(Lazarus.Shared.LazarusPaths.SharedResources.RootDir, "Templates");
        LogsDirectory = Lazarus.Shared.LazarusPaths.SystemData.Logs;
        PluginsDirectory = System.IO.Path.Combine(Lazarus.Shared.LazarusPaths.Root, "Plugins");
    }

    public override async Task ApplySettingsAsync()
    {
        var settings = ParentSettings.Settings;
        settings.ModelsDirectory = ModelsDirectory;
        settings.CacheDirectory = CacheDirectory;
        settings.CacheMaxSizeMB = CacheMaxSizeMB;
        settings.TempFilesLocation = TempFilesLocation;
        settings.ExportPath = ExportPath;
        await Task.CompletedTask;
    }

    protected override void ResetToDefault()
    {
        // Ensure AppData tree exists and use it for defaults
        try { foreach (var d in Lazarus.Shared.LazarusPaths.EnumerateAllDirectories()) Directory.CreateDirectory(d); } catch { }
        ModelsDirectory = Lazarus.Shared.LazarusPaths.Models.RootDir;
        CacheDirectory = Lazarus.Shared.LazarusPaths.SystemData.Cache;
        CacheMaxSizeMB = 2048;
        TempFilesLocation = Lazarus.Shared.LazarusPaths.SystemData.Cache;
        ExportPath = Path.Combine(Lazarus.Shared.LazarusPaths.SharedResources.ImportExport, "Export");

        DownloadDirectory = Path.Combine(Lazarus.Shared.LazarusPaths.SystemData.Cache, "Downloads");
        QuantizedModelsDirectory = Path.Combine(Lazarus.Shared.LazarusPaths.Models.RootDir, "Quantized");
        DatabasePath = Lazarus.Shared.LazarusPaths.DatabaseFile;
        ConversationsDirectory = Path.Combine(Lazarus.Shared.LazarusPaths.Root, "Conversations");
        BackupDirectory = Path.Combine(Lazarus.Shared.LazarusPaths.Root, "Backups");
        ImportDirectory = Path.Combine(Lazarus.Shared.LazarusPaths.SharedResources.ImportExport, "Import");
        TemplatesDirectory = Path.Combine(Lazarus.Shared.LazarusPaths.SharedResources.RootDir, "Templates");
        LogsDirectory = Lazarus.Shared.LazarusPaths.SystemData.Logs;
        PluginsDirectory = Path.Combine(Lazarus.Shared.LazarusPaths.Root, "Plugins");
    }

    private static string NormalizePathToAppData(string? existing, string appDataDefault)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(existing))
                return appDataDefault;

            var full = Path.GetFullPath(existing);
            var appLocal = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            // Already under AppData
            if (full.StartsWith(appLocal, StringComparison.OrdinalIgnoreCase))
                return full;

            // Migrate legacy C:\Lazarus or any non-AppData Lazarus root
            if (full.StartsWith(@"C:\Lazarus\", StringComparison.OrdinalIgnoreCase) ||
                full.IndexOf(@"\Lazarus\", StringComparison.OrdinalIgnoreCase) >= 0)
                return appDataDefault;

            return full;
        }
        catch
        {
            return appDataDefault;
        }
    }
}

/// <summary>
/// ViewModel for Orchestrator settings section
/// </summary>
public class OrchestratorSettingsViewModel : SettingsSectionBase
{
    private int _queueTimeoutSeconds;
    private int _healthCheckIntervalMs;
    private int _maxParallelTasks;
    private int _retryAttempts;

    public OrchestratorSettingsViewModel(SettingsViewModel settings) : base(settings, "Orchestrator")
    {
        SectionDescription = "Configure the orchestrator service behavior";

        // Initialize with default values immediately
        ResetToDefault();
        CompleteInitialization();
    }

    public int QueueTimeoutSeconds
    {
        get => _queueTimeoutSeconds;
        set { if (SetProperty(ref _queueTimeoutSeconds, value)) MarkAsChanged(); }
    }

    public int HealthCheckIntervalMs
    {
        get => _healthCheckIntervalMs;
        set { if (SetProperty(ref _healthCheckIntervalMs, value)) MarkAsChanged(); }
    }

    public int MaxParallelTasks
    {
        get => _maxParallelTasks;
        set { if (SetProperty(ref _maxParallelTasks, value)) MarkAsChanged(); }
    }

    public int RetryAttempts
    {
        get => _retryAttempts;
        set { if (SetProperty(ref _retryAttempts, value)) MarkAsChanged(); }
    }

    public override void RefreshFromSettings()
    {
        var settings = ParentSettings.Settings;
        QueueTimeoutSeconds = settings.QueueTimeoutSeconds;
        HealthCheckIntervalMs = settings.HealthCheckIntervalMs;
        MaxParallelTasks = settings.MaxParallelTasks;
        RetryAttempts = settings.RetryAttempts;
    }

    public override async Task ApplySettingsAsync()
    {
        var settings = ParentSettings.Settings;
        settings.QueueTimeoutSeconds = QueueTimeoutSeconds;
        settings.HealthCheckIntervalMs = HealthCheckIntervalMs;
        settings.MaxParallelTasks = MaxParallelTasks;
        settings.RetryAttempts = RetryAttempts;
        await Task.CompletedTask;
    }

    protected override void ResetToDefault()
    {
        QueueTimeoutSeconds = 30;
        HealthCheckIntervalMs = 5000;
        MaxParallelTasks = 4;
        RetryAttempts = 3;
    }
}

/// <summary>
/// ViewModel for Runners settings section
/// </summary>
public class RunnersSettingsViewModel : SettingsSectionBase
{
    private readonly IHardwareInfoService _hardwareInfoService;
    private string _executionMode = "CPU";
    private int _cpuThreads;
    private int _gpuMemoryLimitGB;
    private string _priorityLevel = "Normal";
    private string _defaultRunner = "llama.cpp";
    private bool _autoStartRunners;
    private int _cpuBatchSize = 32;
    private bool _useBlas = true;
    private string _gpuDevice = "Auto Select";
    private string _cpuName = string.Empty;
    public ObservableCollection<string> GpuDevices { get; } = new();
    private int _gpuLayers = 32;
    private bool _useFlashAttention;
    private int _contextSize = 4096;
    private int _maxTokens = 2048;
    private string _processPriority = "Normal";
    private bool _enableMetrics;
    private bool _enableProfiling;
    private bool _logTokenTiming;
    private int _responseTimeoutSeconds = 120;

    public RunnersSettingsViewModel(SettingsViewModel settings, IHardwareInfoService hardwareInfoService) : base(settings, "Runners")
    {
        _hardwareInfoService = hardwareInfoService;
        SectionDescription = "Configure model execution and hardware utilization";
        DetectHardwareCommand = new RelayCommand(DetectHardware);
        TestRunnerCommand = new RelayCommand(TestRunner);
        ResetToDefaultCommand = new RelayCommand(() => { ResetToDefault(); OnPropertyChanged(""); });

        // Initialize with default values immediately
        ResetToDefault();
        CompleteInitialization();
    }

    public string DefaultRunner
    {
        get => _defaultRunner;
        set { if (SetProperty(ref _defaultRunner, value)) MarkAsChanged(); }
    }

    public string ExecutionMode
    {
        get => _executionMode;
        set { if (SetProperty(ref _executionMode, value)) MarkAsChanged(); }
    }

    public bool AutoStartRunners
    {
        get => _autoStartRunners;
        set { if (SetProperty(ref _autoStartRunners, value)) MarkAsChanged(); }
    }

    public int CpuThreads
    {
        get => _cpuThreads;
        set { if (SetProperty(ref _cpuThreads, value)) MarkAsChanged(); }
    }

    public int CpuBatchSize
    {
        get => _cpuBatchSize;
        set { if (SetProperty(ref _cpuBatchSize, value)) MarkAsChanged(); }
    }

    public bool UseBlas
    {
        get => _useBlas;
        set { if (SetProperty(ref _useBlas, value)) MarkAsChanged(); }
    }

    public string GpuDevice
    {
        get => _gpuDevice;
        set { if (SetProperty(ref _gpuDevice, value)) MarkAsChanged(); }
    }

    public string CpuName
    {
        get => _cpuName;
        private set => SetProperty(ref _cpuName, value);
    }

    public int GpuMemoryLimitGB
    {
        get => _gpuMemoryLimitGB;
        set { if (SetProperty(ref _gpuMemoryLimitGB, value)) MarkAsChanged(); }
    }

    public int GpuLayers
    {
        get => _gpuLayers;
        set { if (SetProperty(ref _gpuLayers, value)) MarkAsChanged(); }
    }

    public bool UseFlashAttention
    {
        get => _useFlashAttention;
        set { if (SetProperty(ref _useFlashAttention, value)) MarkAsChanged(); }
    }

    public int ContextSize
    {
        get => _contextSize;
        set { if (SetProperty(ref _contextSize, value)) MarkAsChanged(); }
    }

    public int MaxTokens
    {
        get => _maxTokens;
        set { if (SetProperty(ref _maxTokens, value)) MarkAsChanged(); }
    }

    public string ProcessPriority
    {
        get => _processPriority;
        set { if (SetProperty(ref _processPriority, value)) MarkAsChanged(); }
    }

    public bool EnableMetrics
    {
        get => _enableMetrics;
        set { if (SetProperty(ref _enableMetrics, value)) MarkAsChanged(); }
    }

    public bool EnableProfiling
    {
        get => _enableProfiling;
        set { if (SetProperty(ref _enableProfiling, value)) MarkAsChanged(); }
    }

    public bool LogTokenTiming
    {
        get => _logTokenTiming;
        set { if (SetProperty(ref _logTokenTiming, value)) MarkAsChanged(); }
    }

    public int ResponseTimeoutSeconds
    {
        get => _responseTimeoutSeconds;
        set { if (SetProperty(ref _responseTimeoutSeconds, value)) MarkAsChanged(); }
    }

    public string PriorityLevel
    {
        get => _priorityLevel;
        set { if (SetProperty(ref _priorityLevel, value)) MarkAsChanged(); }
    }

    public int MaxCpuThreads => Environment.ProcessorCount;

    public ICommand DetectHardwareCommand { get; }
    public ICommand TestRunnerCommand { get; }
    public new ICommand ResetToDefaultCommand { get; }

    private void DetectHardware()
    {
        // Execute on background thread to avoid blocking UI
        _ = Task.Run(async () =>
        {
            try
            {
                var info = await _hardwareInfoService.GetHardwareInfoAsync();

                // CPU
                if (info.Cpu != null)
                {
                    CpuName = info.Cpu.Name;
                    // Suggest threads = min(current, logical processors)
                    var logical = Math.Max(1, info.Cpu.LogicalProcessors);
                    var suggested = Math.Clamp(CpuThreads > 0 ? CpuThreads : logical / 2, 1, logical);
                    CpuThreads = suggested;
                }

                // GPUs
                var anyGpu = info.Gpus != null && info.Gpus.Count > 0;

                // Rebuild list on UI thread via property change marshal
                var items = new List<string>();
                if (anyGpu)
                {
                    items.Add("Auto Select");
                    foreach (var g in info.Gpus!)
                    {
                        double gb = g.AdapterRamBytes > 0 ? g.AdapterRamBytes / 1024d / 1024d / 1024d : 0;
                        var label = gb > 0 ? $"GPU {g.Index} - {g.Name} ({gb:F0} GB)" : $"GPU {g.Index} - {g.Name}";
                        items.Add(label);
                    }
                }
                else
                {
                    items.Add("No GPU detected");
                }

                // Update ItemsSource
                System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
                {
                    GpuDevices.Clear();
                    foreach (var s in items) GpuDevices.Add(s);
                });

                // Select default
                if (anyGpu)
                {
                    GpuDevice = "Auto Select";
                    ExecutionMode = string.IsNullOrEmpty(ExecutionMode) || ExecutionMode == "CPU" ? "Auto" : ExecutionMode;

                    // Heuristic for GPU memory limit: up to (VRAM-1) GB clamped 1..64
                    var first = info.Gpus![0];
                    if (first.AdapterRamBytes > 0)
                    {
                        var vramGb = (int)Math.Max(1, Math.Round(first.AdapterRamBytes / 1024d / 1024d / 1024d));
                        GpuMemoryLimitGB = Math.Clamp(Math.Max(1, vramGb - 1), 1, 64);
                    }
                }
                else
                {
                    GpuDevice = "No GPU detected";
                    ExecutionMode = "CPU";
                }
            }
            catch
            {
                // Fallbacks on error
                CpuThreads = Environment.ProcessorCount;
                if (GpuDevices.Count == 0)
                {
                    System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
                    {
                        GpuDevices.Clear();
                        GpuDevices.Add("Auto Select");
                    });
                }
            }
        });
    }

    private void TestRunner()
    {
        // TODO: Test runner configuration
    }

    public override void RefreshFromSettings()
    {
        var settings = ParentSettings.Settings;

        // Load saved settings
        ExecutionMode = settings.ExecutionMode;
        CpuThreads = settings.CpuThreads;
        GpuMemoryLimitGB = settings.GpuMemoryLimitGB;
        PriorityLevel = settings.PriorityLevel;

        // Initialize other properties with defaults since they're not in AppSettings yet
        DefaultRunner = "llama.cpp";
        AutoStartRunners = false;
        CpuBatchSize = 32;
        UseBlas = true;
        GpuDevice = "Auto Select";
        GpuLayers = 32;
        UseFlashAttention = false;
        ContextSize = 4096;
        MaxTokens = 2048;
        ProcessPriority = "Normal";
        EnableMetrics = true;
        EnableProfiling = false;
        LogTokenTiming = false;
        ResponseTimeoutSeconds = 120;

        // Fix empty values
        if (CpuThreads == 0) CpuThreads = Environment.ProcessorCount / 2;
        if (string.IsNullOrEmpty(ExecutionMode)) ExecutionMode = "CPU";
        if (string.IsNullOrEmpty(PriorityLevel)) PriorityLevel = "Normal";
    }

    public override async Task ApplySettingsAsync()
    {
        var settings = ParentSettings.Settings;
        settings.ExecutionMode = ExecutionMode;
        settings.CpuThreads = CpuThreads;
        settings.GpuMemoryLimitGB = GpuMemoryLimitGB;
        settings.PriorityLevel = PriorityLevel;
        await Task.CompletedTask;
    }

    protected override void ResetToDefault()
    {
        DefaultRunner = "llama.cpp";
        ExecutionMode = "CPU";
        AutoStartRunners = false;
        CpuThreads = Environment.ProcessorCount / 2;
        CpuBatchSize = 32;
        UseBlas = true;
        GpuDevice = "Auto Select";
        GpuMemoryLimitGB = 4;
        GpuLayers = 32;
        UseFlashAttention = false;
        ContextSize = 4096;
        MaxTokens = 2048;
        ProcessPriority = "Normal";
        EnableMetrics = true;
        EnableProfiling = false;
        LogTokenTiming = false;
        ResponseTimeoutSeconds = 120;
        PriorityLevel = "Normal";
    }
}

/// <summary>
/// ViewModel for Models settings section
/// </summary>
public class ModelsSettingsViewModel : SettingsSectionBase
{
    private string _activeModelId = "";
    private string _quantizationLevel = "4-bit";
    private int _contextWindow;
    private bool _modelValidationOnLoad;

    public ModelsSettingsViewModel(SettingsViewModel settings) : base(settings, "Models")
    {
        SectionDescription = "Configure AI model settings and parameters";
        BrowseModelCommand = new RelayCommand(BrowseModel);

        // Initialize with default values immediately
        ResetToDefault();
        CompleteInitialization();
    }

    public string ActiveModelId
    {
        get => _activeModelId;
        set { if (SetProperty(ref _activeModelId, value)) MarkAsChanged(); }
    }

    public string QuantizationLevel
    {
        get => _quantizationLevel;
        set { if (SetProperty(ref _quantizationLevel, value)) MarkAsChanged(); }
    }

    public int ContextWindow
    {
        get => _contextWindow;
        set { if (SetProperty(ref _contextWindow, value)) MarkAsChanged(); }
    }

    public bool ModelValidationOnLoad
    {
        get => _modelValidationOnLoad;
        set { if (SetProperty(ref _modelValidationOnLoad, value)) MarkAsChanged(); }
    }

    public ICommand BrowseModelCommand { get; }

    private void BrowseModel()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Model files (*.gguf;*.bin;*.safetensors)|*.gguf;*.bin;*.safetensors|All files (*.*)|*.*",
            Title = "Select Model File"
        };

        if (dialog.ShowDialog() == true)
        {
            ActiveModelId = dialog.FileName;
        }
    }

    public override void RefreshFromSettings()
    {
        var settings = ParentSettings.Settings;
        ActiveModelId = settings.ActiveModelId;
        QuantizationLevel = settings.QuantizationLevel;
        ContextWindow = settings.ContextWindow;
        ModelValidationOnLoad = settings.ModelValidationOnLoad;
    }

    public override async Task ApplySettingsAsync()
    {
        var settings = ParentSettings.Settings;
        settings.ActiveModelId = ActiveModelId;
        settings.QuantizationLevel = QuantizationLevel;
        settings.ContextWindow = ContextWindow;
        settings.ModelValidationOnLoad = ModelValidationOnLoad;
        await Task.CompletedTask;
    }

    protected override void ResetToDefault()
    {
        ActiveModelId = "";
        QuantizationLevel = "4-bit";
        ContextWindow = 4096;
        ModelValidationOnLoad = true;
    }
}

/// <summary>
/// ViewModel for Audio settings section
/// </summary>
public class AudioSettingsViewModel : SettingsSectionBase
{
    private string _ttsEngine = "System";
    private string _sttProvider = "System";
    private bool _noiseReduction;
    private string _audioQuality = "Medium";

    public AudioSettingsViewModel(SettingsViewModel settings) : base(settings, "Audio")
    {
        SectionDescription = "Configure text-to-speech and speech-to-text settings";

        // Initialize with default values immediately
        ResetToDefault();
        CompleteInitialization();
    }

    public string TtsEngine
    {
        get => _ttsEngine;
        set { if (SetProperty(ref _ttsEngine, value)) MarkAsChanged(); }
    }

    public string SttProvider
    {
        get => _sttProvider;
        set { if (SetProperty(ref _sttProvider, value)) MarkAsChanged(); }
    }

    public bool NoiseReduction
    {
        get => _noiseReduction;
        set { if (SetProperty(ref _noiseReduction, value)) MarkAsChanged(); }
    }

    public string AudioQuality
    {
        get => _audioQuality;
        set { if (SetProperty(ref _audioQuality, value)) MarkAsChanged(); }
    }

    public override void RefreshFromSettings()
    {
        var settings = ParentSettings.Settings;
        TtsEngine = settings.TtsEngine;
        SttProvider = settings.SttProvider;
        NoiseReduction = settings.NoiseReduction;
        AudioQuality = settings.AudioQuality;
    }

    public override async Task ApplySettingsAsync()
    {
        var settings = ParentSettings.Settings;
        settings.TtsEngine = TtsEngine;
        settings.SttProvider = SttProvider;
        settings.NoiseReduction = NoiseReduction;
        settings.AudioQuality = AudioQuality;
        await Task.CompletedTask;
    }

    protected override void ResetToDefault()
    {
        TtsEngine = "System";
        SttProvider = "System";
        NoiseReduction = true;
        AudioQuality = "Medium";
    }
}

/// <summary>
/// ViewModel for Training settings section
/// </summary>
public class TrainingSettingsViewModel : SettingsSectionBase
{
    private int _checkpointFrequencyMinutes;
    private int _batchSize;
    private double _learningRate;
    private int _maxEpochs;

    public TrainingSettingsViewModel(SettingsViewModel settings) : base(settings, "Training")
    {
        SectionDescription = "Configure model training parameters";

        // Initialize with default values immediately
        ResetToDefault();
        CompleteInitialization();
    }

    public int CheckpointFrequencyMinutes
    {
        get => _checkpointFrequencyMinutes;
        set { if (SetProperty(ref _checkpointFrequencyMinutes, value)) MarkAsChanged(); }
    }

    public int BatchSize
    {
        get => _batchSize;
        set { if (SetProperty(ref _batchSize, value)) MarkAsChanged(); }
    }

    public double LearningRate
    {
        get => _learningRate;
        set { if (SetProperty(ref _learningRate, value)) MarkAsChanged(); }
    }

    public int MaxEpochs
    {
        get => _maxEpochs;
        set { if (SetProperty(ref _maxEpochs, value)) MarkAsChanged(); }
    }

    public override void RefreshFromSettings()
    {
        var settings = ParentSettings.Settings;
        CheckpointFrequencyMinutes = settings.CheckpointFrequencyMinutes;
        BatchSize = settings.BatchSize;
        LearningRate = settings.LearningRate;
        MaxEpochs = settings.MaxEpochs;
    }

    public override async Task ApplySettingsAsync()
    {
        var settings = ParentSettings.Settings;
        settings.CheckpointFrequencyMinutes = CheckpointFrequencyMinutes;
        settings.BatchSize = BatchSize;
        settings.LearningRate = LearningRate;
        settings.MaxEpochs = MaxEpochs;
        await Task.CompletedTask;
    }

    protected override void ResetToDefault()
    {
        CheckpointFrequencyMinutes = 10;
        BatchSize = 32;
        LearningRate = 0.001;
        MaxEpochs = 100;
    }
}

/// <summary>
/// ViewModel for Advanced settings section
/// </summary>
public class AdvancedSettingsViewModel : SettingsSectionBase
{
    private bool _experimentalFeatures;
    private int _memoryLimitOverrideGB;
    private string _gpuComputeMode = "Auto";
    private string _proxyUrl = "";

    public AdvancedSettingsViewModel(SettingsViewModel settings) : base(settings, "Advanced")
    {
        SectionDescription = "Advanced configuration and experimental features";

        // Initialize with default values immediately
        ResetToDefault();
        CompleteInitialization();
    }

    public bool ExperimentalFeatures
    {
        get => _experimentalFeatures;
        set { if (SetProperty(ref _experimentalFeatures, value)) MarkAsChanged(); }
    }

    public int MemoryLimitOverrideGB
    {
        get => _memoryLimitOverrideGB;
        set { if (SetProperty(ref _memoryLimitOverrideGB, value)) MarkAsChanged(); }
    }

    public string GpuComputeMode
    {
        get => _gpuComputeMode;
        set { if (SetProperty(ref _gpuComputeMode, value)) MarkAsChanged(); }
    }

    public string ProxyUrl
    {
        get => _proxyUrl;
        set { if (SetProperty(ref _proxyUrl, value)) MarkAsChanged(); }
    }

    public override void RefreshFromSettings()
    {
        var settings = ParentSettings.Settings;
        ExperimentalFeatures = settings.ExperimentalFeatures;
        MemoryLimitOverrideGB = settings.MemoryLimitOverrideGB;
        GpuComputeMode = settings.GpuComputeMode;
        ProxyUrl = settings.ProxyUrl;
    }

    public override async Task ApplySettingsAsync()
    {
        var settings = ParentSettings.Settings;
        settings.ExperimentalFeatures = ExperimentalFeatures;
        settings.MemoryLimitOverrideGB = MemoryLimitOverrideGB;
        settings.GpuComputeMode = GpuComputeMode;
        settings.ProxyUrl = ProxyUrl;
        await Task.CompletedTask;
    }

    protected override void ResetToDefault()
    {
        ExperimentalFeatures = false;
        MemoryLimitOverrideGB = 0;
        GpuComputeMode = "Auto";
        ProxyUrl = "";
    }
}
