using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lazarus.App.Desktop.Services;

/// <summary>
/// Service interface for managing Lazarus user profile directory structure
/// </summary>
public interface IDirectoryService
{
    /// <summary>
    /// Gets the root user profile directory path
    /// </summary>
    string UserProfilePath { get; }

    /// <summary>
    /// Gets all configured directory paths for the user profile
    /// </summary>
    IReadOnlyDictionary<string, string> DirectoryPaths { get; }

    /// <summary>
    /// Initializes the complete user profile directory structure
    /// </summary>
    /// <returns>Task representing the async operation</returns>
    Task<DirectoryInitializationResult> InitializeUserProfileAsync();

    /// <summary>
    /// Validates all required directories exist and are accessible
    /// </summary>
    /// <returns>Validation result with details of any issues</returns>
    Task<DirectoryValidationResult> ValidateDirectoryStructureAsync();

    /// <summary>
    /// Gets the path for a specific directory type
    /// </summary>
    /// <param name="directoryType">Type of directory to get path for</param>
    /// <returns>Full path to the requested directory</returns>
    string GetDirectoryPath(DirectoryType directoryType);

    /// <summary>
    /// Ensures a specific directory exists, creating it if necessary
    /// </summary>
    /// <param name="directoryType">Directory type to ensure exists</param>
    /// <returns>True if directory exists or was created successfully</returns>
    Task<bool> EnsureDirectoryExistsAsync(DirectoryType directoryType);

    /// <summary>
    /// Cleans up temporary files and cache directories
    /// </summary>
    /// <returns>Task representing the cleanup operation</returns>
    Task CleanupTemporaryFilesAsync();

    /// <summary>
    /// Gets directory usage statistics
    /// </summary>
    /// <returns>Usage statistics for all directories</returns>
    Task<DirectoryUsageStatistics> GetDirectoryUsageAsync();
}

/// <summary>
/// Types of directories in the Lazarus user profile structure
/// </summary>
public enum DirectoryType
{
    // Root directory
    UserProfile,

    // Model directories
    BaseModels,
    LoRAAdapters,
    Embeddings,
    Tokenizers,

    // Generation asset directories
    VAEModels,
    ControlNet,
    UpscaleModels,
    StylePresets,

    // User content directories
    InputFiles,
    InputImages,
    InputAudio,
    InputVideo,
    InputDocuments,
    GeneratedOutput,
    GeneratedImages,
    GeneratedAudio,
    GeneratedVideo,
    GeneratedText,
    Projects,
    TrainingSessions,
    ConversationHistories,
    SavedWorkflows,

    // System data directories
    Configuration,
    Cache,
    Logs,
    Database,

    // Shared resources
    ExternalLinks,
    ImportExport
}

/// <summary>
/// Result of directory initialization operation
/// </summary>
public class DirectoryInitializationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> CreatedDirectories { get; set; } = new();
    public List<string> ExistingDirectories { get; set; } = new();
    public List<DirectoryError> Errors { get; set; } = new();
    public TimeSpan InitializationTime { get; set; }
}

/// <summary>
/// Result of directory validation operation
/// </summary>
public class DirectoryValidationResult
{
    public bool IsValid { get; set; }
    public List<string> ValidDirectories { get; set; } = new();
    public List<DirectoryError> ValidationErrors { get; set; } = new();
    public List<string> MissingDirectories { get; set; } = new();
    public List<string> InaccessibleDirectories { get; set; } = new();
}

/// <summary>
/// Directory usage statistics
/// </summary>
public class DirectoryUsageStatistics
{
    public long TotalSizeBytes { get; set; }
    public int TotalFiles { get; set; }
    public int TotalDirectories { get; set; }
    public Dictionary<DirectoryType, DirectoryStats> DirectoryBreakdown { get; set; } = new();
    public DateTime LastUpdated { get; set; }
}

/// <summary>
/// Statistics for individual directory
/// </summary>
public class DirectoryStats
{
    public long SizeBytes { get; set; }
    public int FileCount { get; set; }
    public int SubdirectoryCount { get; set; }
    public DateTime LastModified { get; set; }
    public string Path { get; set; } = string.Empty;
}

/// <summary>
/// Directory operation error details
/// </summary>
public class DirectoryError
{
    public DirectoryType DirectoryType { get; set; }
    public string Path { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public Exception? Exception { get; set; }
    public DirectoryErrorType ErrorType { get; set; }
}

/// <summary>
/// Types of directory errors
/// </summary>
public enum DirectoryErrorType
{
    PermissionDenied,
    PathTooLong,
    DiskFull,
    InvalidPath,
    NetworkError,
    UnknownError
}