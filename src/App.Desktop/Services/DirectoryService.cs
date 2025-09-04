using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading.Tasks;

namespace Lazarus.App.Desktop.Services;

/// <summary>
/// Service for managing Lazarus user profile directory structure and file system operations
/// </summary>
public class DirectoryService : IDirectoryService
{
    private readonly ILogger<DirectoryService> _logger;
    private readonly Dictionary<DirectoryType, string> _directoryPaths;
    private readonly string _userProfilePath;

    /// <summary>
    /// Directory structure definition following AI application industry standards
    /// </summary>
    private readonly Dictionary<DirectoryType, (string RelativePath, string Description)> _directoryStructure = new()
    {
        // Root
        { DirectoryType.UserProfile, ("", "Lazarus application user profile root directory") },

        // Models
        { DirectoryType.BaseModels, ("Models/Base-Models", "Large Language Models and foundation models") },
        { DirectoryType.LoRAAdapters, ("Models/LoRA-Adapters", "Low-Rank Adaptation fine-tuning models") },
        { DirectoryType.Embeddings, ("Models/Embeddings", "Text and multimodal embedding models") },
        { DirectoryType.Tokenizers, ("Models/Tokenizers", "Text tokenization and vocabulary models") },

        // Generation Assets
        { DirectoryType.VAEModels, ("Generation-Assets/VAE-Models", "Variational Auto-Encoder models for generation") },
        { DirectoryType.ControlNet, ("Generation-Assets/ControlNet", "ControlNet models for guided generation") },
        { DirectoryType.UpscaleModels, ("Generation-Assets/Upscale-Models", "Image and video upscaling models") },
        { DirectoryType.StylePresets, ("Generation-Assets/Style-Presets", "Predefined styles and artistic presets") },

        // User Content - Input Files
        { DirectoryType.InputFiles, ("User-Content/Input-Files", "User provided input files") },
        { DirectoryType.InputImages, ("User-Content/Input-Files/Images", "Input images for processing") },
        { DirectoryType.InputAudio, ("User-Content/Input-Files/Audio", "Input audio files for processing") },
        { DirectoryType.InputVideo, ("User-Content/Input-Files/Video", "Input video files for processing") },
        { DirectoryType.InputDocuments, ("User-Content/Input-Files/Documents", "Input documents and text files") },

        // User Content - Generated Output
        { DirectoryType.GeneratedOutput, ("User-Content/Generated-Output", "AI generated content output") },
        { DirectoryType.GeneratedImages, ("User-Content/Generated-Output/Images", "Generated images and artwork") },
        { DirectoryType.GeneratedAudio, ("User-Content/Generated-Output/Audio", "Generated audio and music") },
        { DirectoryType.GeneratedVideo, ("User-Content/Generated-Output/Video", "Generated video content") },
        { DirectoryType.GeneratedText, ("User-Content/Generated-Output/Text", "Generated text and documents") },

        // User Content - Projects
        { DirectoryType.Projects, ("User-Content/Projects", "User projects and workflows") },
        { DirectoryType.TrainingSessions, ("User-Content/Projects/Training-Sessions", "Model training sessions and data") },
        { DirectoryType.ConversationHistories, ("User-Content/Projects/Conversation-Histories", "Chat and conversation logs") },
        { DirectoryType.SavedWorkflows, ("User-Content/Projects/Saved-Workflows", "Saved automation workflows") },

        // System Data
        { DirectoryType.Configuration, ("System-Data/Configuration", "Application configuration files") },
        { DirectoryType.Cache, ("System-Data/Cache", "Temporary cache and performance data") },
        { DirectoryType.Logs, ("System-Data/Logs", "Application logs and diagnostics") },
        { DirectoryType.Database, ("System-Data/Database", "Local database files") },

        // Shared Resources
        { DirectoryType.ExternalLinks, ("Shared-Resources/External-Links", "Links to external model repositories") },
        { DirectoryType.ImportExport, ("Shared-Resources/Import-Export", "Data import and export staging") }
    };

    public DirectoryService(ILogger<DirectoryService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _userProfilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Lazarus");

        _directoryPaths = new Dictionary<DirectoryType, string>();
        BuildDirectoryPaths();

        _logger.LogInformation("Directory service initialized with user profile path: {UserProfilePath}", _userProfilePath);
    }

    public string UserProfilePath => _userProfilePath;

    public IReadOnlyDictionary<string, string> DirectoryPaths => 
        _directoryPaths.ToDictionary(kvp => kvp.Key.ToString(), kvp => kvp.Value);

    private void BuildDirectoryPaths()
    {
        foreach (var (directoryType, (relativePath, _)) in _directoryStructure)
        {
            var fullPath = string.IsNullOrEmpty(relativePath) 
                ? _userProfilePath 
                : Path.Combine(_userProfilePath, relativePath);
            
            _directoryPaths[directoryType] = fullPath;
        }
    }

    public async Task<DirectoryInitializationResult> InitializeUserProfileAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new DirectoryInitializationResult();

        try
        {
            _logger.LogInformation("Starting user profile directory initialization");

            foreach (var (directoryType, path) in _directoryPaths)
            {
                try
                {
                    if (Directory.Exists(path))
                    {
                        result.ExistingDirectories.Add(path);
                        _logger.LogDebug("Directory already exists: {Path}", path);
                    }
                    else
                    {
                        await CreateDirectoryWithPermissionsAsync(path);
                        result.CreatedDirectories.Add(path);
                        _logger.LogInformation("Created directory: {Path}", path);
                    }

                    // Create README files for key directories
                    await CreateReadmeFileAsync(directoryType, path);
                }
                catch (Exception ex)
                {
                    var error = CreateDirectoryError(directoryType, path, ex);
                    result.Errors.Add(error);
                    _logger.LogError(ex, "Failed to create directory {Path} for type {DirectoryType}", path, directoryType);
                }
            }

            // Create configuration templates
            await CreateConfigurationTemplatesAsync();

            result.Success = result.Errors.Count == 0;
            result.Message = result.Success 
                ? $"Successfully initialized {result.CreatedDirectories.Count + result.ExistingDirectories.Count} directories" 
                : $"Initialized with {result.Errors.Count} errors";

            stopwatch.Stop();
            result.InitializationTime = stopwatch.Elapsed;

            _logger.LogInformation("Directory initialization completed in {ElapsedMs}ms. Success: {Success}, Created: {Created}, Errors: {Errors}",
                stopwatch.ElapsedMilliseconds, result.Success, result.CreatedDirectories.Count, result.Errors.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error during directory initialization");
            result.Success = false;
            result.Message = $"Critical initialization error: {ex.Message}";
            return result;
        }
    }

    private async Task CreateDirectoryWithPermissionsAsync(string path)
    {
        Directory.CreateDirectory(path);

        // Set appropriate permissions on Windows
        if (OperatingSystem.IsWindows())
        {
            try
            {
                var directoryInfo = new DirectoryInfo(path);
                var security = directoryInfo.GetAccessControl();
                var currentUser = WindowsIdentity.GetCurrent();

                if (currentUser.User != null)
                {
                    var accessRule = new FileSystemAccessRule(
                        currentUser.User,
                        FileSystemRights.FullControl,
                        InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                        PropagationFlags.None,
                        AccessControlType.Allow);

                    security.AddAccessRule(accessRule);
                    directoryInfo.SetAccessControl(security);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not set enhanced permissions for directory {Path}", path);
            }
        }

        await Task.CompletedTask;
    }

    private async Task CreateReadmeFileAsync(DirectoryType directoryType, string directoryPath)
    {
        if (!_directoryStructure.TryGetValue(directoryType, out var dirInfo))
            return;

        var readmePath = Path.Combine(directoryPath, "README.txt");
        
        if (!File.Exists(readmePath))
        {
            var content = $"""
                Lazarus AI Application - {directoryType} Directory

                Purpose: {dirInfo.Description}

                This directory is part of the Lazarus AI application user profile structure.
                Location: {directoryPath}

                """;

            // Add specific usage notes for different directory types
            content += directoryType switch
            {
                DirectoryType.BaseModels => """
                    Usage Notes:
                    - Place downloaded LLM model files (.gguf, .safetensors, etc.) here
                    - Organize models by type or size for easier management
                    - Large files may take time to load - ensure sufficient disk space

                    """,
                DirectoryType.GeneratedImages => """
                    Usage Notes:
                    - All AI-generated images will be saved here automatically
                    - Files are organized by generation date and time
                    - Supported formats: PNG, JPEG, WebP, TIFF

                    """,
                DirectoryType.Configuration => """
                    Usage Notes:
                    - Contains application settings and user preferences
                    - Modify configuration files carefully to avoid application issues
                    - Backup recommended before making changes

                    """,
                DirectoryType.Cache => """
                    Usage Notes:
                    - Temporary files and performance cache data
                    - Safe to delete contents if experiencing issues
                    - Files in this directory may be automatically cleaned up

                    """,
                _ => """
                    Usage Notes:
                    - This directory is managed automatically by Lazarus
                    - Refer to application documentation for specific usage guidelines

                    """
            };

            content += $"""
                Created: {DateTime.Now:yyyy-MM-dd HH:mm:ss}
                Lazarus AI Application v1.0
                """;

            await File.WriteAllTextAsync(readmePath, content);
            _logger.LogDebug("Created README file for {DirectoryType} at {ReadmePath}", directoryType, readmePath);
        }
    }

    private async Task CreateConfigurationTemplatesAsync()
    {
        var configPath = GetDirectoryPath(DirectoryType.Configuration);

        // Create main application settings template
        var settingsPath = Path.Combine(configPath, "user-settings.json");
        if (!File.Exists(settingsPath))
        {
            var settingsTemplate = """
                {
                    "theme": "Dark",
                    "viewMode": "Enthusiast",
                    "autoSave": true,
                    "maxCacheSize": "10GB",
                    "defaultModel": "",
                    "generation": {
                        "defaultImageSize": "512x512",
                        "defaultSteps": 20,
                        "defaultSampler": "DPM++ 2M Karras"
                    },
                    "performance": {
                        "enableHardwareAcceleration": true,
                        "maxConcurrentTasks": 4,
                        "memoryLimit": "8GB"
                    },
                    "directories": {
                        "autoCleanupCache": true,
                        "cleanupIntervalDays": 7,
                        "maxLogFileSize": "100MB"
                    }
                }
                """;
            
            await File.WriteAllTextAsync(settingsPath, settingsTemplate);
            _logger.LogInformation("Created user settings template at {SettingsPath}", settingsPath);
        }

        // Create model registry template
        var modelRegistryPath = Path.Combine(configPath, "model-registry.json");
        if (!File.Exists(modelRegistryPath))
        {
            var registryTemplate = """
                {
                    "models": [],
                    "lastUpdated": "",
                    "autoDiscovery": true,
                    "verifyIntegrity": true
                }
                """;
            
            await File.WriteAllTextAsync(modelRegistryPath, registryTemplate);
            _logger.LogInformation("Created model registry template at {RegistryPath}", modelRegistryPath);
        }
    }

    public async Task<DirectoryValidationResult> ValidateDirectoryStructureAsync()
    {
        var result = new DirectoryValidationResult { IsValid = true };

        foreach (var (directoryType, path) in _directoryPaths)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    result.MissingDirectories.Add(path);
                    result.IsValid = false;
                    _logger.LogWarning("Missing directory: {Path} for type {DirectoryType}", path, directoryType);
                }
                else if (!await IsDirectoryAccessibleAsync(path))
                {
                    result.InaccessibleDirectories.Add(path);
                    result.IsValid = false;
                    _logger.LogWarning("Inaccessible directory: {Path} for type {DirectoryType}", path, directoryType);
                }
                else
                {
                    result.ValidDirectories.Add(path);
                    _logger.LogDebug("Validated directory: {Path}", path);
                }
            }
            catch (Exception ex)
            {
                var error = CreateDirectoryError(directoryType, path, ex);
                result.ValidationErrors.Add(error);
                result.IsValid = false;
                _logger.LogError(ex, "Validation error for directory {Path}", path);
            }
        }

        _logger.LogInformation("Directory validation completed. Valid: {ValidCount}, Missing: {MissingCount}, Inaccessible: {InaccessibleCount}, Errors: {ErrorCount}",
            result.ValidDirectories.Count, result.MissingDirectories.Count, result.InaccessibleDirectories.Count, result.ValidationErrors.Count);

        return result;
    }

    private async Task<bool> IsDirectoryAccessibleAsync(string path)
    {
        try
        {
            var testFile = Path.Combine(path, $"test_{Guid.NewGuid():N}.tmp");
            await File.WriteAllTextAsync(testFile, "test");
            File.Delete(testFile);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public string GetDirectoryPath(DirectoryType directoryType)
    {
        if (_directoryPaths.TryGetValue(directoryType, out var path))
            return path;

        throw new ArgumentException($"Unknown directory type: {directoryType}", nameof(directoryType));
    }

    public async Task<bool> EnsureDirectoryExistsAsync(DirectoryType directoryType)
    {
        try
        {
            var path = GetDirectoryPath(directoryType);
            
            if (!Directory.Exists(path))
            {
                await CreateDirectoryWithPermissionsAsync(path);
                await CreateReadmeFileAsync(directoryType, path);
                _logger.LogInformation("Created missing directory: {Path} for type {DirectoryType}", path, directoryType);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ensure directory exists for type {DirectoryType}", directoryType);
            return false;
        }
    }

    public async Task CleanupTemporaryFilesAsync()
    {
        try
        {
            var cachePath = GetDirectoryPath(DirectoryType.Cache);
            var cutoffTime = DateTime.Now.AddDays(-7); // Clean files older than 7 days

            if (Directory.Exists(cachePath))
            {
                var files = Directory.GetFiles(cachePath, "*", SearchOption.AllDirectories);
                var deletedCount = 0;

                foreach (var file in files)
                {
                    try
                    {
                        var fileInfo = new FileInfo(file);
                        if (fileInfo.LastAccessTime < cutoffTime)
                        {
                            File.Delete(file);
                            deletedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete cache file: {FilePath}", file);
                    }
                }

                _logger.LogInformation("Cleanup completed: deleted {DeletedCount} temporary files", deletedCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during temporary file cleanup");
        }

        await Task.CompletedTask;
    }

    public async Task<DirectoryUsageStatistics> GetDirectoryUsageAsync()
    {
        var stats = new DirectoryUsageStatistics
        {
            LastUpdated = DateTime.Now,
            DirectoryBreakdown = new Dictionary<DirectoryType, DirectoryStats>()
        };

        foreach (var (directoryType, path) in _directoryPaths)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    var dirStats = await CalculateDirectoryStatsAsync(path);
                    stats.DirectoryBreakdown[directoryType] = dirStats;
                    
                    stats.TotalSizeBytes += dirStats.SizeBytes;
                    stats.TotalFiles += dirStats.FileCount;
                    stats.TotalDirectories += dirStats.SubdirectoryCount + 1;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not calculate stats for directory {Path}", path);
            }
        }

        return stats;
    }

    private async Task<DirectoryStats> CalculateDirectoryStatsAsync(string path)
    {
        var stats = new DirectoryStats { Path = path };

        try
        {
            var directoryInfo = new DirectoryInfo(path);
            stats.LastModified = directoryInfo.LastWriteTime;

            var files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
            stats.FileCount = files.Length;

            var directories = Directory.GetDirectories(path, "*", SearchOption.AllDirectories);
            stats.SubdirectoryCount = directories.Length;

            foreach (var file in files)
            {
                try
                {
                    var fileInfo = new FileInfo(file);
                    stats.SizeBytes += fileInfo.Length;
                }
                catch
                {
                    // Skip files that can't be accessed
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error calculating directory stats for {Path}", path);
        }

        await Task.CompletedTask;
        return stats;
    }

    private DirectoryError CreateDirectoryError(DirectoryType directoryType, string path, Exception exception)
    {
        var errorType = exception switch
        {
            UnauthorizedAccessException => DirectoryErrorType.PermissionDenied,
            PathTooLongException => DirectoryErrorType.PathTooLong,
            IOException when exception.Message.Contains("not enough space") => DirectoryErrorType.DiskFull,
            ArgumentException => DirectoryErrorType.InvalidPath,
            _ => DirectoryErrorType.UnknownError
        };

        return new DirectoryError
        {
            DirectoryType = directoryType,
            Path = path,
            ErrorMessage = exception.Message,
            Exception = exception,
            ErrorType = errorType
        };
    }
}