using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Lazarus.Shared.Settings;

/// <summary>
/// Comprehensive settings schema for Lazarus AI application
/// </summary>
public class AppSettings : INotifyPropertyChanged
{
    // Chat persona defaults
    private string _userName = "You";
    private string _assistantName = "Assistant";
    private string _systemPrompt = string.Empty;
    private string _defaultModel = "llama3.2";
    private bool _autoSaveConversations = true;
    private bool _autoUpdateCheck = true;
    private string _modelsDirectory = @"C:\Lazarus\Models";
    private string _cacheDirectory = @"C:\Lazarus\Cache";
    private int _cacheMaxSizeMB = 2048;
    private string _tempFilesLocation = @"C:\Lazarus\Temp";
    private string _exportPath = @"C:\Lazarus\Exports";
    private int _queueTimeoutSeconds = 30;
    private int _healthCheckIntervalMs = 5000;
    private int _maxParallelTasks = 4;
    private int _retryAttempts = 3;
    private string _executionMode = "CPU";
    private int _cpuThreads = Environment.ProcessorCount / 2;
    private int _gpuMemoryLimitGB = 4;
    private string _priorityLevel = "Normal";
    private string _activeModelId = "";
    private string _quantizationLevel = "4-bit";
    private int _contextWindow = 4096;
    private bool _modelValidationOnLoad = true;
    private string _ttsEngine = "System";
    private string _sttProvider = "System";
    private bool _noiseReduction = true;
    private string _audioQuality = "Medium";
    private string _vectorDbType = "SQLite";
    private int _chunkSize = 512;
    private int _overlapSize = 50;
    private double _similarityThreshold = 0.7;
    private int _checkpointFrequencyMinutes = 10;
    private int _batchSize = 32;
    private double _learningRate = 0.001;
    private int _maxEpochs = 100;
    private string _logLevel = "Information";
    private int _maxLogSizeMB = 100;
    private int _logRetentionDays = 30;
    private bool _consoleOutput = false;
    private bool _experimentalFeatures = false;
    private int _memoryLimitOverrideGB = 0;
    private string _gpuComputeMode = "Auto";
    private string _proxyUrl = "";
    private bool _startOrchestratorWithApp = true;
    private bool _autoStartLastRunner = false;
    private int _orchestratorHealthCheckIntervalSec = 5;
    private int _orchestratorStartupTimeoutSec = 30;
    private bool _orchestratorAutoRestartOnCrash = true;
    // UI state - remember last image runner selection
    private string _lastImageRunnerPath = string.Empty;
    // Additional UI & General fields
    private string _language = "English";
    private string _uiTheme = "Dark";
    private bool _startWithWindows;
    private bool _startMinimized;
    private bool _restoreLastSession;
    private bool _autoLoadModel;
    private int _autoSaveIntervalMinutes = 5;
    private int _historyLimit = 200;
    private bool _autoDownloadUpdates;
    private bool _sendAnonymousUsage;
    private bool _sendCrashReports;

    public event PropertyChangedEventHandler? PropertyChanged;

    // General Settings
    [JsonPropertyName("defaultModel")]
    [Description("Default AI model to load on startup")]
    public string DefaultModel
    {
        get => _defaultModel;
        set { _defaultModel = value; OnPropertyChanged(nameof(DefaultModel)); }
    }

    [JsonPropertyName("autoSaveConversations")]
    [Description("Automatically save conversation history")]
    public bool AutoSaveConversations
    {
        get => _autoSaveConversations;
        set { _autoSaveConversations = value; OnPropertyChanged(nameof(AutoSaveConversations)); }
    }

    [JsonPropertyName("autoUpdateCheck")]
    [Description("Check for updates on startup")]
    public bool AutoUpdateCheck
    {
        get => _autoUpdateCheck;
        set { _autoUpdateCheck = value; OnPropertyChanged(nameof(AutoUpdateCheck)); }
    }

    // Chat Persona Settings
    [JsonPropertyName("userName")]
    [Description("Display name shown for the user in chats")] 
    public string UserName
    {
        get => string.IsNullOrWhiteSpace(_userName) ? "You" : _userName;
        set { _userName = string.IsNullOrWhiteSpace(value) ? "You" : value.Trim(); OnPropertyChanged(nameof(UserName)); }
    }

    [JsonPropertyName("assistantName")]
    [Description("Display name shown for the assistant in chats")] 
    public string AssistantName
    {
        get => string.IsNullOrWhiteSpace(_assistantName) ? "Assistant" : _assistantName;
        set { _assistantName = string.IsNullOrWhiteSpace(value) ? "Assistant" : value.Trim(); OnPropertyChanged(nameof(AssistantName)); }
    }

    [JsonPropertyName("systemPrompt")]
    [Description("Custom system prompt injected into each chat request")] 
    public string SystemPrompt
    {
        get => _systemPrompt ?? string.Empty;
        set { _systemPrompt = value?.Trim() ?? string.Empty; OnPropertyChanged(nameof(SystemPrompt)); }
    }

    // Paths Settings
    [JsonPropertyName("modelsDirectory")]
    [Description("Directory where AI models are stored")]
    public string ModelsDirectory
    {
        get => _modelsDirectory;
        set { _modelsDirectory = value; OnPropertyChanged(nameof(ModelsDirectory)); }
    }

    [JsonPropertyName("cacheDirectory")]
    [Description("Directory for temporary cache files")]
    public string CacheDirectory
    {
        get => _cacheDirectory;
        set { _cacheDirectory = value; OnPropertyChanged(nameof(CacheDirectory)); }
    }

    [JsonPropertyName("cacheMaxSizeMB")]
    [Description("Maximum cache size in megabytes")]
    public int CacheMaxSizeMB
    {
        get => _cacheMaxSizeMB;
        set { _cacheMaxSizeMB = Math.Max(100, Math.Min(10240, value)); OnPropertyChanged(nameof(CacheMaxSizeMB)); }
    }

    [JsonPropertyName("tempFilesLocation")]
    [Description("Location for temporary files")]
    public string TempFilesLocation
    {
        get => _tempFilesLocation;
        set { _tempFilesLocation = value; OnPropertyChanged(nameof(TempFilesLocation)); }
    }

    [JsonPropertyName("exportPath")]
    [Description("Default path for exported files")]
    public string ExportPath
    {
        get => _exportPath;
        set { _exportPath = value; OnPropertyChanged(nameof(ExportPath)); }
    }

    // Orchestrator Settings
    [JsonPropertyName("queueTimeoutSeconds")]
    [Description("Timeout for queued operations in seconds")]
    public int QueueTimeoutSeconds
    {
        get => _queueTimeoutSeconds;
        set { _queueTimeoutSeconds = Math.Max(5, Math.Min(300, value)); OnPropertyChanged(nameof(QueueTimeoutSeconds)); }
    }

    [JsonPropertyName("healthCheckIntervalMs")]
    [Description("Interval between health checks in milliseconds")]
    public int HealthCheckIntervalMs
    {
        get => _healthCheckIntervalMs;
        set { _healthCheckIntervalMs = Math.Max(1000, Math.Min(60000, value)); OnPropertyChanged(nameof(HealthCheckIntervalMs)); }
    }

    [JsonPropertyName("maxParallelTasks")]
    [Description("Maximum number of parallel tasks")]
    public int MaxParallelTasks
    {
        get => _maxParallelTasks;
        set { _maxParallelTasks = Math.Max(1, Math.Min(16, value)); OnPropertyChanged(nameof(MaxParallelTasks)); }
    }

    [JsonPropertyName("retryAttempts")]
    [Description("Number of retry attempts for failed operations")]
    public int RetryAttempts
    {
        get => _retryAttempts;
        set { _retryAttempts = Math.Max(0, Math.Min(10, value)); OnPropertyChanged(nameof(RetryAttempts)); }
    }

    // Runners Settings
    [JsonPropertyName("executionMode")]
    [Description("Execution mode for model inference")]
    public string ExecutionMode
    {
        get => _executionMode;
        set { _executionMode = value; OnPropertyChanged(nameof(ExecutionMode)); }
    }

    [JsonPropertyName("cpuThreads")]
    [Description("Number of CPU threads to use")]
    public int CpuThreads
    {
        get => _cpuThreads;
        set { _cpuThreads = Math.Max(1, Math.Min(Environment.ProcessorCount, value)); OnPropertyChanged(nameof(CpuThreads)); }
    }

    [JsonPropertyName("gpuMemoryLimitGB")]
    [Description("GPU memory limit in gigabytes")]
    public int GpuMemoryLimitGB
    {
        get => _gpuMemoryLimitGB;
        set { _gpuMemoryLimitGB = Math.Max(1, Math.Min(64, value)); OnPropertyChanged(nameof(GpuMemoryLimitGB)); }
    }

    [JsonPropertyName("priorityLevel")]
    [Description("Process priority level")]
    public string PriorityLevel
    {
        get => _priorityLevel;
        set { _priorityLevel = value; OnPropertyChanged(nameof(PriorityLevel)); }
    }

    // Models Settings
    [JsonPropertyName("activeModelId")]
    [Description("Currently active model identifier")]
    public string ActiveModelId
    {
        get => _activeModelId;
        set { _activeModelId = value; OnPropertyChanged(nameof(ActiveModelId)); }
    }

    [JsonPropertyName("quantizationLevel")]
    [Description("Model quantization level")]
    public string QuantizationLevel
    {
        get => _quantizationLevel;
        set { _quantizationLevel = value; OnPropertyChanged(nameof(QuantizationLevel)); }
    }

    [JsonPropertyName("contextWindow")]
    [Description("Context window size in tokens")]
    public int ContextWindow
    {
        get => _contextWindow;
        set { _contextWindow = Math.Max(512, Math.Min(32768, value)); OnPropertyChanged(nameof(ContextWindow)); }
    }

    [JsonPropertyName("modelValidationOnLoad")]
    [Description("Validate model integrity when loading")]
    public bool ModelValidationOnLoad
    {
        get => _modelValidationOnLoad;
        set { _modelValidationOnLoad = value; OnPropertyChanged(nameof(ModelValidationOnLoad)); }
    }

    // Audio Settings
    [JsonPropertyName("ttsEngine")]
    [Description("Text-to-speech engine")]
    public string TtsEngine
    {
        get => _ttsEngine;
        set { _ttsEngine = value; OnPropertyChanged(nameof(TtsEngine)); }
    }

    [JsonPropertyName("sttProvider")]
    [Description("Speech-to-text provider")]
    public string SttProvider
    {
        get => _sttProvider;
        set { _sttProvider = value; OnPropertyChanged(nameof(SttProvider)); }
    }

    [JsonPropertyName("noiseReduction")]
    [Description("Enable noise reduction for audio input")]
    public bool NoiseReduction
    {
        get => _noiseReduction;
        set { _noiseReduction = value; OnPropertyChanged(nameof(NoiseReduction)); }
    }

    [JsonPropertyName("audioQuality")]
    [Description("Audio quality setting")]
    public string AudioQuality
    {
        get => _audioQuality;
        set { _audioQuality = value; OnPropertyChanged(nameof(AudioQuality)); }
    }

    // Embeddings/RAG Settings
    [JsonPropertyName("vectorDbType")]
    [Description("Vector database type for embeddings")]
    public string VectorDbType
    {
        get => _vectorDbType;
        set { _vectorDbType = value; OnPropertyChanged(nameof(VectorDbType)); }
    }

    [JsonPropertyName("chunkSize")]
    [Description("Document chunk size in tokens")]
    public int ChunkSize
    {
        get => _chunkSize;
        set { _chunkSize = Math.Max(100, Math.Min(2000, value)); OnPropertyChanged(nameof(ChunkSize)); }
    }

    [JsonPropertyName("overlapSize")]
    [Description("Overlap size between chunks in tokens")]
    public int OverlapSize
    {
        get => _overlapSize;
        set { _overlapSize = Math.Max(0, Math.Min(500, value)); OnPropertyChanged(nameof(OverlapSize)); }
    }

    [JsonPropertyName("similarityThreshold")]
    [Description("Similarity threshold for retrieval (0.0-1.0)")]
    public double SimilarityThreshold
    {
        get => _similarityThreshold;
        set { _similarityThreshold = Math.Max(0.0, Math.Min(1.0, value)); OnPropertyChanged(nameof(SimilarityThreshold)); }
    }

    // Training Settings
    [JsonPropertyName("checkpointFrequencyMinutes")]
    [Description("Frequency of training checkpoints in minutes")]
    public int CheckpointFrequencyMinutes
    {
        get => _checkpointFrequencyMinutes;
        set { _checkpointFrequencyMinutes = Math.Max(1, Math.Min(60, value)); OnPropertyChanged(nameof(CheckpointFrequencyMinutes)); }
    }

    [JsonPropertyName("batchSize")]
    [Description("Training batch size")]
    public int BatchSize
    {
        get => _batchSize;
        set { _batchSize = Math.Max(1, Math.Min(256, value)); OnPropertyChanged(nameof(BatchSize)); }
    }

    [JsonPropertyName("learningRate")]
    [Description("Training learning rate")]
    public double LearningRate
    {
        get => _learningRate;
        set { _learningRate = Math.Max(0.00001, Math.Min(1.0, value)); OnPropertyChanged(nameof(LearningRate)); }
    }

    [JsonPropertyName("maxEpochs")]
    [Description("Maximum training epochs")]
    public int MaxEpochs
    {
        get => _maxEpochs;
        set { _maxEpochs = Math.Max(1, Math.Min(10000, value)); OnPropertyChanged(nameof(MaxEpochs)); }
    }

    // Logging Settings
    [JsonPropertyName("logLevel")]
    [Description("Logging level")]
    public string LogLevel
    {
        get => _logLevel;
        set { _logLevel = value; OnPropertyChanged(nameof(LogLevel)); }
    }

    [JsonPropertyName("maxLogSizeMB")]
    [Description("Maximum log file size in megabytes")]
    public int MaxLogSizeMB
    {
        get => _maxLogSizeMB;
        set { _maxLogSizeMB = Math.Max(1, Math.Min(1024, value)); OnPropertyChanged(nameof(MaxLogSizeMB)); }
    }

    [JsonPropertyName("logRetentionDays")]
    [Description("Number of days to retain log files")]
    public int LogRetentionDays
    {
        get => _logRetentionDays;
        set { _logRetentionDays = Math.Max(1, Math.Min(365, value)); OnPropertyChanged(nameof(LogRetentionDays)); }
    }

    [JsonPropertyName("consoleOutput")]
    [Description("Enable console output for logging")]
    public bool ConsoleOutput
    {
        get => _consoleOutput;
        set { _consoleOutput = value; OnPropertyChanged(nameof(ConsoleOutput)); }
    }

    // Advanced Settings
    [JsonPropertyName("experimentalFeatures")]
    [Description("Enable experimental features")]
    public bool ExperimentalFeatures
    {
        get => _experimentalFeatures;
        set { _experimentalFeatures = value; OnPropertyChanged(nameof(ExperimentalFeatures)); }
    }

    [JsonPropertyName("memoryLimitOverrideGB")]
    [Description("Override memory limit in gigabytes (0 = auto)")]
    public int MemoryLimitOverrideGB
    {
        get => _memoryLimitOverrideGB;
        set { _memoryLimitOverrideGB = Math.Max(0, Math.Min(256, value)); OnPropertyChanged(nameof(MemoryLimitOverrideGB)); }
    }

    [JsonPropertyName("gpuComputeMode")]
    [Description("GPU compute mode")]
    public string GpuComputeMode
    {
        get => _gpuComputeMode;
        set { _gpuComputeMode = value; OnPropertyChanged(nameof(GpuComputeMode)); }
    }

    [JsonPropertyName("proxyUrl")]
    [Description("Proxy URL for network requests")]
    public string ProxyUrl
    {
        get => _proxyUrl;
        set { _proxyUrl = value; OnPropertyChanged(nameof(ProxyUrl)); }
    }

    // Additional Orchestrator Settings
    [JsonPropertyName("startOrchestratorWithApp")]
    [Description("Start orchestrator service with application")]
    public bool StartOrchestratorWithApp
    {
        get => _startOrchestratorWithApp;
        set { _startOrchestratorWithApp = value; OnPropertyChanged(nameof(StartOrchestratorWithApp)); }
    }

    [JsonPropertyName("autoStartLastRunner")]
    [Description("Automatically start the last used runner")]
    public bool AutoStartLastRunner
    {
        get => _autoStartLastRunner;
        set { _autoStartLastRunner = value; OnPropertyChanged(nameof(AutoStartLastRunner)); }
    }

    [JsonPropertyName("orchestratorHealthCheckIntervalSec")]
    [Description("Health check interval in seconds")]
    public int OrchestratorHealthCheckIntervalSec
    {
        get => _orchestratorHealthCheckIntervalSec;
        set { _orchestratorHealthCheckIntervalSec = Math.Max(1, Math.Min(60, value)); OnPropertyChanged(nameof(OrchestratorHealthCheckIntervalSec)); }
    }

    [JsonPropertyName("orchestratorStartupTimeoutSec")]
    [Description("Orchestrator startup timeout in seconds")]
    public int OrchestratorStartupTimeoutSec
    {
        get => _orchestratorStartupTimeoutSec;
        set { _orchestratorStartupTimeoutSec = Math.Max(5, Math.Min(120, value)); OnPropertyChanged(nameof(OrchestratorStartupTimeoutSec)); }
    }

    [JsonPropertyName("orchestratorAutoRestartOnCrash")]
    [Description("Automatically restart orchestrator if it crashes")]
    public bool OrchestratorAutoRestartOnCrash
    {
        get => _orchestratorAutoRestartOnCrash;
        set { _orchestratorAutoRestartOnCrash = value; OnPropertyChanged(nameof(OrchestratorAutoRestartOnCrash)); }
    }

    // Images View - Last selected image runner path
    [JsonPropertyName("lastImageRunnerPath")]
    public string LastImageRunnerPath
    {
        get => _lastImageRunnerPath ?? string.Empty;
        set { _lastImageRunnerPath = value ?? string.Empty; OnPropertyChanged(nameof(LastImageRunnerPath)); }
    }

    // UI & General (additional)
    [JsonPropertyName("language")]
    public string Language
    {
        get => string.IsNullOrWhiteSpace(_language) ? "English" : _language;
        set { _language = string.IsNullOrWhiteSpace(value) ? "English" : value.Trim(); OnPropertyChanged(nameof(Language)); }
    }

    [JsonPropertyName("uiTheme")]
    public string UiTheme
    {
        get => string.IsNullOrWhiteSpace(_uiTheme) ? "Dark" : _uiTheme;
        set { _uiTheme = string.IsNullOrWhiteSpace(value) ? "Dark" : value.Trim(); OnPropertyChanged(nameof(UiTheme)); }
    }

    [JsonPropertyName("startWithWindows")]
    public bool StartWithWindows
    {
        get => _startWithWindows;
        set { _startWithWindows = value; OnPropertyChanged(nameof(StartWithWindows)); }
    }

    [JsonPropertyName("startMinimized")]
    public bool StartMinimized
    {
        get => _startMinimized;
        set { _startMinimized = value; OnPropertyChanged(nameof(StartMinimized)); }
    }

    [JsonPropertyName("restoreLastSession")]
    public bool RestoreLastSession
    {
        get => _restoreLastSession;
        set { _restoreLastSession = value; OnPropertyChanged(nameof(RestoreLastSession)); }
    }

    [JsonPropertyName("autoLoadModel")]
    public bool AutoLoadModel
    {
        get => _autoLoadModel;
        set { _autoLoadModel = value; OnPropertyChanged(nameof(AutoLoadModel)); }
    }

    [JsonPropertyName("autoSaveIntervalMinutes")]
    public int AutoSaveIntervalMinutes
    {
        get => _autoSaveIntervalMinutes;
        set { _autoSaveIntervalMinutes = Math.Max(1, Math.Min(1440, value)); OnPropertyChanged(nameof(AutoSaveIntervalMinutes)); }
    }

    [JsonPropertyName("historyLimit")]
    public int HistoryLimit
    {
        get => _historyLimit;
        set { _historyLimit = Math.Max(1, Math.Min(10000, value)); OnPropertyChanged(nameof(HistoryLimit)); }
    }

    [JsonPropertyName("autoDownloadUpdates")]
    public bool AutoDownloadUpdates
    {
        get => _autoDownloadUpdates;
        set { _autoDownloadUpdates = value; OnPropertyChanged(nameof(AutoDownloadUpdates)); }
    }

    [JsonPropertyName("sendAnonymousUsage")]
    public bool SendAnonymousUsage
    {
        get => _sendAnonymousUsage;
        set { _sendAnonymousUsage = value; OnPropertyChanged(nameof(SendAnonymousUsage)); }
    }

    [JsonPropertyName("sendCrashReports")]
    public bool SendCrashReports
    {
        get => _sendCrashReports;
        set { _sendCrashReports = value; OnPropertyChanged(nameof(SendCrashReports)); }
    }

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Creates a default settings instance
    /// </summary>
    public static AppSettings CreateDefault() => new();

    /// <summary>
    /// Validates all settings and returns error messages if any
    /// </summary>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(ModelsDirectory))
            errors.Add("Models directory cannot be empty");

        if (string.IsNullOrWhiteSpace(CacheDirectory))
            errors.Add("Cache directory cannot be empty");

        if (CacheMaxSizeMB < 100)
            errors.Add("Cache size must be at least 100 MB");

        if (QueueTimeoutSeconds < 5)
            errors.Add("Queue timeout must be at least 5 seconds");

        if (HealthCheckIntervalMs < 1000)
            errors.Add("Health check interval must be at least 1000 ms");

        if (MaxParallelTasks < 1)
            errors.Add("Must allow at least 1 parallel task");

        if (CpuThreads < 1 || CpuThreads > Environment.ProcessorCount)
            errors.Add($"CPU threads must be between 1 and {Environment.ProcessorCount}");

        if (ContextWindow < 512 || ContextWindow > 32768)
            errors.Add("Context window must be between 512 and 32768");

        if (ChunkSize < 100 || ChunkSize > 2000)
            errors.Add("Chunk size must be between 100 and 2000");

        if (SimilarityThreshold < 0.0 || SimilarityThreshold > 1.0)
            errors.Add("Similarity threshold must be between 0.0 and 1.0");

        if (LearningRate <= 0.0 || LearningRate > 1.0)
            errors.Add("Learning rate must be between 0.00001 and 1.0");

        return errors;
    }
}
