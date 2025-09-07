using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Lazarus.Shared.Settings;

/// <summary>
/// Defines the strongly-typed, versioned settings schema for Lazarus.
/// </summary>
public static class SettingsSchema
{
    public const int CurrentVersion = 1;
}

/// <summary>
/// Root application settings object (flat MVP schema).
/// </summary>
public sealed class AppSettings
{
    // Always bump when the shape changes in a breaking way.
    public int SchemaVersion { get; set; } = SettingsSchema.CurrentVersion;

    // ---- General ----
    public string? PreferredTheme { get; set; } = "Dark"; // Dark | Light | System
    public string? Language { get; set; } = "en-US";
    public bool CheckForUpdatesOnStart { get; set; } = false; // future use
    public bool AutoSaveConversations { get; set; } = true;

    // ---- Paths ----
    public string ModelsDirectory { get; set; } = @"D:\models"; // user can change
    public string CacheDirectory { get; set; } = @"%LOCALAPPDATA%\Lazarus\cache";
    public string ExportedChatsDirectory { get; set; } = @"%LOCALAPPDATA%\Lazarus\exported-chats";

    // ---- Orchestrator ----
    public string OrchestratorBaseUrl { get; set; } = "http://127.0.0.1:11711";
    public int OrchestratorStartupTimeoutSec { get; set; } = 45;
    public int OrchestratorHealthCheckIntervalSec { get; set; } = 10;
    public bool OrchestratorAutoRestartOnCrash { get; set; } = true;
    public bool StartOrchestratorWithApp { get; set; } = true;

    // ---- Runner (active + per-runner opts) ----
    public string ActiveRunner { get; set; } = "llama.cpp"; // llama.cpp | vllm | exllamav2
    public bool AutoStartLastRunner { get; set; } = false;

    public LlamaCppSettings LlamaCpp { get; set; } = new();
    public VllmSettings Vllm { get; set; } = new();
    public ExLlamaV2Settings ExLlamaV2 { get; set; } = new();

    public string? ActiveModelId { get; set; } = null;

    // Additional sections
    public TrainingSettings Training { get; set; } = new();
    public AudioSettings Audio { get; set; } = new();
    public RagSettings Rag { get; set; } = new();
    public UiSettings Ui { get; set; } = new();
    public LoggingSettings Logging { get; set; } = new();
    
    // Global controls
    public int MaxConcurrentTasks { get; set; } = 2;
    public bool ExperimentalFeatures { get; set; } = false;
    public int MemoryLimitMb { get; set; } = 0; // 0 = no limit
    public string? NetworkProxy { get; set; } = null;
    public bool DeveloperMode { get; set; } = false;

    /// <summary>
    /// Creates default settings aligned with the MVP schema.
    /// </summary>
    public static AppSettings CreateDefault() => new();
}

/// <summary>
/// Minimal llama.cpp runner settings for MVP.
/// </summary>
public sealed class LlamaCppSettings
{
    public string ServerExecutablePath { get; set; } = @"%LOCALAPPDATA%\Lazarus\Runners\llama.cpp\llama-server.exe";
    public string AdditionalArgs { get; set; } = string.Empty;
    public int Port { get; set; } = 8080;
    public int GpuLayers { get; set; } = 9999;
    public bool UseCuda { get; set; } = true;
    public int MemoryLimitPercent { get; set; } = 100; // 10-100
}

/// <summary>
/// Minimal vLLM runner settings for MVP.
/// </summary>
public sealed class VllmSettings
{
    public string PythonEnvPath { get; set; } = @"%LOCALAPPDATA%\Lazarus\Runners\vllm\py";
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 8000;
    public string LaunchArgs { get; set; } = string.Empty;
}

/// <summary>
/// Minimal ExLlamaV2 runner settings for MVP.
/// </summary>
public sealed class ExLlamaV2Settings
{
    public string ServerPath { get; set; } = @"%LOCALAPPDATA%\Lazarus\Runners\exllamav2\server.exe";
    public string LaunchArgs { get; set; } = string.Empty;
}

public sealed class TrainingSettings
{
    public string DefaultTrainer { get; set; } = "llama-factory";
    public string WorkingDirectory { get; set; } = @"%LOCALAPPDATA%\Lazarus\training";
    public int CheckpointIntervalMinutes { get; set; } = 15;
    public int DataFractionPercent { get; set; } = 100; // 1-100
    public double LearningRate { get; set; } = 3e-4;
}

public sealed class AudioSettings
{
    public bool EnableTts { get; set; } = false;
    public string PiperExecutable { get; set; } = @"%LOCALAPPDATA%\Lazarus\audio\piper.exe";
    public string PiperVoice { get; set; } = "en_US-amy-medium";
    public bool EnableAsr { get; set; } = false;
    public string FasterWhisperExecutable { get; set; } = @"%LOCALAPPDATA%\Lazarus\audio\faster-whisper.exe";
    public bool NoiseSuppression { get; set; } = true;
    public string Quality { get; set; } = "Balanced"; // Low | Balanced | High
    public string SpeechRecognition { get; set; } = "Faster-Whisper"; // Faster-Whisper | System | None
}

public sealed class RagSettings
{
    public bool EnableVectorStore { get; set; } = false;
    public string DatabasePath { get; set; } = @"%LOCALAPPDATA%\Lazarus\lazarus.db";
    public bool UseSQLiteVss { get; set; } = false;
    public int DocumentChunkTokens { get; set; } = 512;
    public double SimilarityThreshold { get; set; } = 0.75; // 0-1
    public string StorageEngine { get; set; } = "SQLite"; // SQLite | SQLite-VSS | FAISS
}

public sealed class UiSettings
{
    public bool ShowTokenStream { get; set; } = true;
    public double FontSize { get; set; } = 13.0;
    public HotkeySettings Hotkeys { get; set; } = new();
}

public sealed class HotkeySettings
{
    public string NewScreenshot { get; set; } = "Ctrl+Shift+4";
    public string NewChat { get; set; } = "Ctrl+N";
    public string EmergencyStop { get; set; } = "Ctrl+Esc";
    public string SearchSettings { get; set; } = "Ctrl+F";
}

public sealed class LoggingSettings
{
    public string Level { get; set; } = "Information";
    public bool EnableStructured { get; set; } = true;
    public int RetentionDays { get; set; } = 7;
    public bool SendCrashReports { get; set; } = false;
}
