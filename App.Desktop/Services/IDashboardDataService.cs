using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using App.Shared.Enums;

namespace Lazarus.Desktop.Services
{
    /// <summary>
    /// Core data service interfaces for Dashboard system
    /// Provides data abstraction for different ViewMode implementations
    /// </summary>
    
    public interface ISystemMonitor
    {
        SystemStatus GetCurrentStatus();
        MemoryUsage GetMemoryUsage();
        ObservableCollection<ProcessInfo> GetRunningProcesses();
        ObservableCollection<DataPoint> GetCpuHistory(TimeSpan timeRange);
        ObservableCollection<DataPoint> GetMemoryHistory(TimeSpan timeRange);
        double GetTokensPerSecond();
        event EventHandler<SystemStatusChangedEventArgs> StatusChanged;
    }

    public interface IModelManager
    {
        ModelInfo GetCurrentModel();
        ObservableCollection<ModelInfo> GetAvailableModels();
        PerformanceMetrics GetModelPerformance();
        Task SwitchModelAsync(ModelInfo model);
        event EventHandler<ModelChangedEventArgs> ModelChanged;
    }

    public interface ITrainingService
    {
        ObservableCollection<TrainingJob> GetActiveJobs();
        ObservableCollection<TrainingJob> GetQueuedJobs();
        ObservableCollection<TrainingJob> GetRecentlyCompleted();
        TrainingJob GetJobById(string jobId);
        Task<string> StartTrainingAsync(TrainingConfiguration config);
        event EventHandler<TrainingJobStatusChangedEventArgs> JobStatusChanged;
    }

    public interface IDiagnosticsService
    {
        SystemDiagnostics GetFullSystemState();
        ObservableCollection<ProcessDiagnostic> GetProcessDiagnostics();
        ObservableCollection<LogEntry> GetRealTimeLogStream();
        Dictionary<string, EndpointMetrics> GetApiEndpointStats();
        DatabaseMetrics GetDatabaseMetrics();
        event EventHandler<DiagnosticDataChangedEventArgs> DiagnosticDataChanged;
    }

    public interface IConfigurationService
    {
        Dictionary<string, ConfigValue> GetCurrentConfig();
        ObservableCollection<ConfigChange> GetRecentChanges();
        Task<bool> UpdateConfigAsync(string key, object value);
        Task<bool> ValidateConfigAsync(Dictionary<string, object> config);
    }

    // Data Models
    public class SystemStatus
    {
        public bool IsOnline { get; set; }
        public string CurrentRunner { get; set; } = "";
        public Dictionary<RunnerType, RunnerStatus> RunnerStates { get; set; } = new();
        public TimeSpan ApiResponseTime { get; set; }
        public double CpuUsagePercent { get; set; }
        public double GpuUtilizationPercent { get; set; }
        public string ApiStatus { get; set; } = "";
        public string ApiStatusColor { get; set; } = "";
    }

    public class MemoryUsage
    {
        public long TotalSystemRam { get; set; }
        public long UsedSystemRam { get; set; }
        public long TotalVram { get; set; }
        public long UsedVram { get; set; }
        public double SystemRamPercentage => TotalSystemRam > 0 ? (double)UsedSystemRam / TotalSystemRam * 100 : 0;
        public double VramPercentage => TotalVram > 0 ? (double)UsedVram / TotalVram * 100 : 0;
        public string SystemRamDisplay => $"{UsedSystemRam / (1024 * 1024 * 1024.0):F1} / {TotalSystemRam / (1024 * 1024 * 1024.0):F1} GB";
        public string VramDisplay => $"{UsedVram / (1024 * 1024):F0} / {TotalVram / (1024 * 1024):F0} MB";
    }

    public class ProcessInfo
    {
        public int ProcessId { get; set; }
        public string ProcessName { get; set; } = "";
        public string Status { get; set; } = "";
        public double CpuUsage { get; set; }
        public long MemoryUsage { get; set; }
        public DateTime StartTime { get; set; }
        public string CommandLine { get; set; } = "";
    }

    public class DataPoint
    {
        public DateTime Timestamp { get; set; }
        public double Value { get; set; }
    }

    public class ModelInfo
    {
        public string Name { get; set; } = "";
        public string Path { get; set; } = "";
        public string Format { get; set; } = "";
        public RunnerType PreferredRunner { get; set; }
        public long SizeBytes { get; set; }
        public int ContextLength { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new();
        public bool IsLoaded { get; set; }
        public DateTime LastUsed { get; set; }
    }

    public class PerformanceMetrics
    {
        public double TokensPerSecond { get; set; }
        public TimeSpan AverageResponseTime { get; set; }
        public int TotalRequests { get; set; }
        public int FailedRequests { get; set; }
        public double SuccessRate => TotalRequests > 0 ? (double)(TotalRequests - FailedRequests) / TotalRequests * 100 : 0;
    }

    public class TrainingJob
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public TrainingJobStatus Status { get; set; }
        public int ProgressPercent { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public TimeSpan EstimatedTimeRemaining { get; set; }
        public string ModelType { get; set; } = "";
        public string DatasetPath { get; set; } = "";
        public Dictionary<string, object> Configuration { get; set; } = new();
        public string ErrorMessage { get; set; } = "";
    }

    public class TrainingConfiguration
    {
        public string Name { get; set; } = "";
        public string ModelType { get; set; } = "";
        public string DatasetPath { get; set; } = "";
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    public class SystemDiagnostics
    {
        public DateTime Timestamp { get; set; }
        public SystemStatus SystemStatus { get; set; } = new();
        public MemoryUsage MemoryUsage { get; set; } = new();
        public ObservableCollection<ProcessInfo> Processes { get; set; } = new();
        public Dictionary<string, object> RawMetrics { get; set; } = new();
    }

    public class ProcessDiagnostic
    {
        public ProcessInfo ProcessInfo { get; set; } = new();
        public ObservableCollection<DataPoint> CpuHistory { get; set; } = new();
        public ObservableCollection<DataPoint> MemoryHistory { get; set; } = new();
        public Dictionary<string, string> Environment { get; set; } = new();
    }

    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public LogLevel Level { get; set; }
        public string Source { get; set; } = "";
        public string Message { get; set; } = "";
        public string? Exception { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new();
    }

    public class EndpointMetrics
    {
        public string Endpoint { get; set; } = "";
        public int TotalCalls { get; set; }
        public int SuccessfulCalls { get; set; }
        public TimeSpan AverageResponseTime { get; set; }
        public DateTime LastCalled { get; set; }
        public ObservableCollection<DataPoint> ResponseTimeHistory { get; set; } = new();
    }

    public class DatabaseMetrics
    {
        public long DatabaseSize { get; set; }
        public int TableCount { get; set; }
        public ObservableCollection<QueryPerformance> SlowQueries { get; set; } = new();
        public double AverageQueryTime { get; set; }
        public int QueriesPerSecond { get; set; }
    }

    public class QueryPerformance
    {
        public string Query { get; set; } = "";
        public TimeSpan ExecutionTime { get; set; }
        public int ExecutionCount { get; set; }
        public DateTime LastExecuted { get; set; }
    }

    public class ConfigValue
    {
        public string Key { get; set; } = "";
        public object Value { get; set; } = new();
        public string Description { get; set; } = "";
        public bool IsReadOnly { get; set; }
        public string[] AllowedValues { get; set; } = Array.Empty<string>();
    }

    public class ConfigChange
    {
        public string Key { get; set; } = "";
        public object OldValue { get; set; } = new();
        public object NewValue { get; set; } = new();
        public DateTime Timestamp { get; set; }
        public string ChangedBy { get; set; } = "";
    }

    // Enums
    public enum RunnerType
    {
        LlamaCpp,
        LlamaServer,
        vLLM,
        ExLlamaV2,
        Ollama
    }

    public enum RunnerStatus
    {
        Offline,
        Starting,
        Online,
        Error,
        Stopping
    }

    public enum TrainingJobStatus
    {
        Queued,
        Starting,
        Running,
        Paused,
        Completed,
        Failed,
        Cancelled
    }

    public enum LogLevel
    {
        Trace,
        Debug,
        Information,
        Warning,
        Error,
        Critical
    }

    // Event Args
    public class SystemStatusChangedEventArgs : EventArgs
    {
        public SystemStatus SystemStatus { get; set; } = new();
    }

    public class ModelChangedEventArgs : EventArgs
    {
        public ModelInfo? OldModel { get; set; }
        public ModelInfo? NewModel { get; set; }
    }

    public class TrainingJobStatusChangedEventArgs : EventArgs
    {
        public TrainingJob Job { get; set; } = new();
        public TrainingJobStatus OldStatus { get; set; }
        public TrainingJobStatus NewStatus { get; set; }
    }

    public class DiagnosticDataChangedEventArgs : EventArgs
    {
        public SystemDiagnostics Diagnostics { get; set; } = new();
    }
}
