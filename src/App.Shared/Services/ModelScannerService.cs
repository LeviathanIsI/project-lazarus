using Lazarus.App.Shared.Models;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Lazarus.App.Shared.Services;

/// <summary>
/// Implementation of the Model Scanner service for discovering and analyzing LLM files
/// </summary>
public class ModelScannerService : IModelScannerService
{
    private readonly ILogger<ModelScannerService> _logger;
    
    // Supported file extensions for different model formats
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".gguf",        // GGML Unified Format
        ".safetensors", // SafeTensors format
        ".bin",         // PyTorch binary format
        ".pth",         // PyTorch format
        ".pt",          // PyTorch format
        ".json",        // Configuration files
        ".model"        // Generic model format
    };

    // Architecture patterns for extracting model information from filenames
    private static readonly Dictionary<string, Regex> ArchitecturePatterns = new()
    {
        { "Llama", new Regex(@"llama", RegexOptions.IgnoreCase | RegexOptions.Compiled) },
        { "Qwen", new Regex(@"qwen", RegexOptions.IgnoreCase | RegexOptions.Compiled) },
        { "Mistral", new Regex(@"mistral", RegexOptions.IgnoreCase | RegexOptions.Compiled) },
        { "CodeLlama", new Regex(@"code[-_]?llama", RegexOptions.IgnoreCase | RegexOptions.Compiled) },
        { "Falcon", new Regex(@"falcon", RegexOptions.IgnoreCase | RegexOptions.Compiled) },
        { "Alpaca", new Regex(@"alpaca", RegexOptions.IgnoreCase | RegexOptions.Compiled) },
        { "Vicuna", new Regex(@"vicuna", RegexOptions.IgnoreCase | RegexOptions.Compiled) },
        { "WizardLM", new Regex(@"wizard[-_]?lm", RegexOptions.IgnoreCase | RegexOptions.Compiled) }
    };

    // Parameter count patterns
    private static readonly Regex ParameterCountPattern = new(@"(\d+(?:\.\d+)?)[bB]", RegexOptions.Compiled);
    
    // Quantization format patterns
    private static readonly Regex QuantizationPattern = new(@"[QF](\d+(?:_[KM])?(?:_[SML])?)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Initializes a new instance of the <see cref="ModelScannerService"/> class
    /// </summary>
    /// <param name="logger">The logger instance</param>
    public ModelScannerService(ILogger<ModelScannerService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<IEnumerable<string>> ScanDirectoryAsync(string directoryPath, bool includeSubdirectories = true, CancellationToken cancellationToken = default)
    {
        var timestamp = DateTime.Now;
        _logger.LogInformation("[ASSET.KEEPER.DEBUG] {Timestamp:HH:mm:ss.fff} STARTING DirectoryScan - Path: '{DirectoryPath}', Recursive: {IncludeSubdirectories}", 
            timestamp, directoryPath, includeSubdirectories);

        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            var exception = new ArgumentException("Directory path cannot be null or empty", nameof(directoryPath));
            _logger.LogError("[ASSET.KEEPER.DEBUG] {Timestamp:HH:mm:ss.fff} FATAL: Null/Empty directory path provided", DateTime.Now);
            throw exception;
        }

        // Log resolved absolute path
        var absolutePath = Path.GetFullPath(directoryPath);
        _logger.LogInformation("[ASSET.KEEPER.DEBUG] {Timestamp:HH:mm:ss.fff} RESOLVED PATH: '{DirectoryPath}' -> '{AbsolutePath}'", 
            DateTime.Now, directoryPath, absolutePath);

        if (!Directory.Exists(absolutePath))
        {
            _logger.LogWarning("[ASSET.KEEPER.DEBUG] {Timestamp:HH:mm:ss.fff} DIRECTORY NOT FOUND: '{AbsolutePath}' (original: '{DirectoryPath}')", 
                DateTime.Now, absolutePath, directoryPath);
            
            // Try to check parent directory existence for better diagnostics
            var parentDir = Path.GetDirectoryName(absolutePath);
            if (!string.IsNullOrEmpty(parentDir))
            {
                var parentExists = Directory.Exists(parentDir);
                _logger.LogInformation("[ASSET.KEEPER.DEBUG] {Timestamp:HH:mm:ss.fff} PARENT DIRECTORY '{ParentDir}' exists: {Exists}", 
                    DateTime.Now, parentDir, parentExists);
            }
            
            return Enumerable.Empty<string>();
        }

        _logger.LogInformation("[ASSET.KEEPER.DEBUG] {Timestamp:HH:mm:ss.fff} CONFIRMED: Directory exists and is accessible", DateTime.Now);

        var modelFiles = new List<string>();
        var totalFilesScanned = 0;
        var supportedFilesFound = 0;
        var unsupportedFilesSkipped = 0;

        try
        {
            var searchOption = includeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            _logger.LogInformation("[ASSET.KEEPER.DEBUG] {Timestamp:HH:mm:ss.fff} ENUMERATING FILES: SearchOption={SearchOption}", 
                DateTime.Now, searchOption);
            
            var allFiles = Directory.EnumerateFiles(absolutePath, "*.*", searchOption);
            _logger.LogInformation("[ASSET.KEEPER.DEBUG] {Timestamp:HH:mm:ss.fff} FILE ENUMERATION: Started processing files", DateTime.Now);

            foreach (var filePath in allFiles)
            {
                totalFilesScanned++;
                cancellationToken.ThrowIfCancellationRequested();

                var fileName = Path.GetFileName(filePath);
                var extension = Path.GetExtension(filePath);
                
                _logger.LogDebug("[ASSET.KEEPER.DEBUG] {Timestamp:HH:mm:ss.fff} SCANNING FILE #{FileNumber}: '{FileName}' (ext: {Extension})", 
                    DateTime.Now, totalFilesScanned, fileName, extension);

                if (await IsSupportedModelFileAsync(filePath, cancellationToken))
                {
                    modelFiles.Add(filePath);
                    supportedFilesFound++;
                    _logger.LogInformation("[ASSET.KEEPER.DEBUG] {Timestamp:HH:mm:ss.fff} SUPPORTED MODEL FOUND #{SupportedCount}: '{FilePath}'", 
                        DateTime.Now, supportedFilesFound, filePath);
                }
                else
                {
                    unsupportedFilesSkipped++;
                    _logger.LogDebug("[ASSET.KEEPER.DEBUG] {Timestamp:HH:mm:ss.fff} UNSUPPORTED FILE SKIPPED #{SkippedCount}: '{FileName}' (ext: {Extension})", 
                        DateTime.Now, unsupportedFilesSkipped, fileName, extension);
                }
                
                // Log progress every 10 files
                if (totalFilesScanned % 10 == 0)
                {
                    _logger.LogInformation("[ASSET.KEEPER.DEBUG] {Timestamp:HH:mm:ss.fff} SCAN PROGRESS: {TotalScanned} files scanned, {SupportedFound} models found", 
                        DateTime.Now, totalFilesScanned, supportedFilesFound);
                }
            }

            var scanDuration = DateTime.Now - timestamp;
            _logger.LogInformation("[ASSET.KEEPER.DEBUG] {Timestamp:HH:mm:ss.fff} SCAN COMPLETED: {SupportedFound}/{TotalScanned} files (duration: {Duration}ms)", 
                DateTime.Now, supportedFilesFound, totalFilesScanned, scanDuration.TotalMilliseconds);
                
            _logger.LogInformation("[ASSET.KEEPER.DEBUG] SCAN SUMMARY - Directory: '{DirectoryPath}', Total Files: {TotalScanned}, Models Found: {ModelsFound}, Unsupported: {Unsupported}", 
                absolutePath, totalFilesScanned, supportedFilesFound, unsupportedFilesSkipped);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError("[ASSET.KEEPER.DEBUG] {Timestamp:HH:mm:ss.fff} ACCESS DENIED scanning directory '{DirectoryPath}': {Error}", 
                DateTime.Now, absolutePath, ex.Message);
            throw;
        }
        catch (DirectoryNotFoundException ex)
        {
            _logger.LogError("[ASSET.KEEPER.DEBUG] {Timestamp:HH:mm:ss.fff} DIRECTORY NOT FOUND during scan '{DirectoryPath}': {Error}", 
                DateTime.Now, absolutePath, ex.Message);
            throw;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "[ASSET.KEEPER.DEBUG] {Timestamp:HH:mm:ss.fff} UNEXPECTED ERROR scanning directory '{DirectoryPath}': {Error}", 
                DateTime.Now, absolutePath, ex.Message);
            throw;
        }

        return modelFiles;
    }

    /// <inheritdoc />
    public async Task<bool> IsSupportedModelFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var timestamp = DateTime.Now;
        _logger.LogDebug("[ASSET.KEEPER.DEBUG] {Timestamp:HH:mm:ss.fff} VALIDATING FILE: '{FilePath}'", timestamp, filePath);

        if (string.IsNullOrWhiteSpace(filePath))
        {
            _logger.LogDebug("[ASSET.KEEPER.DEBUG] {Timestamp:HH:mm:ss.fff} VALIDATION FAILED: Null/empty file path", DateTime.Now);
            return false;
        }

        var fileName = Path.GetFileName(filePath);
        _logger.LogDebug("[ASSET.KEEPER.DEBUG] {Timestamp:HH:mm:ss.fff} CHECKING FILE EXISTENCE: '{FileName}'", DateTime.Now, fileName);

        if (!File.Exists(filePath))
        {
            _logger.LogDebug("[ASSET.KEEPER.DEBUG] {Timestamp:HH:mm:ss.fff} VALIDATION FAILED: File does not exist - '{FilePath}'", DateTime.Now, filePath);
            return false;
        }

        var extension = Path.GetExtension(filePath);
        _logger.LogDebug("[ASSET.KEEPER.DEBUG] {Timestamp:HH:mm:ss.fff} FILE EXTENSION: '{Extension}' for '{FileName}'", DateTime.Now, extension, fileName);
        
        if (!SupportedExtensions.Contains(extension))
        {
            _logger.LogDebug("[ASSET.KEEPER.DEBUG] {Timestamp:HH:mm:ss.fff} VALIDATION FAILED: Unsupported extension '{Extension}' for '{FileName}'", 
                DateTime.Now, extension, fileName);
            _logger.LogDebug("[ASSET.KEEPER.DEBUG] Supported extensions: [{SupportedExtensions}]", 
                string.Join(", ", SupportedExtensions));
            return false;
        }

        _logger.LogDebug("[ASSET.KEEPER.DEBUG] {Timestamp:HH:mm:ss.fff} EXTENSION VALIDATION PASSED: '{Extension}' for '{FileName}'", 
            DateTime.Now, extension, fileName);

        // Additional validation for specific formats
        if (extension.Equals(".gguf", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogDebug("[ASSET.KEEPER.DEBUG] {Timestamp:HH:mm:ss.fff} PERFORMING GGUF VALIDATION: '{FileName}'", DateTime.Now, fileName);
            var isValidGguf = await IsValidGgufFileAsync(filePath, cancellationToken);
            _logger.LogDebug("[ASSET.KEEPER.DEBUG] {Timestamp:HH:mm:ss.fff} GGUF VALIDATION RESULT: {IsValid} for '{FileName}'", 
                DateTime.Now, isValidGguf, fileName);
            return isValidGguf;
        }

        if (extension.Equals(".safetensors", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogDebug("[ASSET.KEEPER.DEBUG] {Timestamp:HH:mm:ss.fff} PERFORMING SAFETENSORS VALIDATION: '{FileName}'", DateTime.Now, fileName);
            var isValidSafeTensors = await IsValidSafeTensorsFileAsync(filePath, cancellationToken);
            _logger.LogDebug("[ASSET.KEEPER.DEBUG] {Timestamp:HH:mm:ss.fff} SAFETENSORS VALIDATION RESULT: {IsValid} for '{FileName}'", 
                DateTime.Now, isValidSafeTensors, fileName);
            return isValidSafeTensors;
        }

        // For other formats, basic extension check is sufficient
        _logger.LogDebug("[ASSET.KEEPER.DEBUG] {Timestamp:HH:mm:ss.fff} VALIDATION PASSED: Basic extension check for '{FileName}' ({Extension})", 
            DateTime.Now, fileName, extension);
        return true;
    }

    /// <inheritdoc />
    public async Task<ModelMetadata?> ExtractMetadataAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (!await IsSupportedModelFileAsync(filePath, cancellationToken))
        {
            return null;
        }

        _logger.LogDebug("Extracting metadata from file: {FilePath}", filePath);

        var fileInfo = new FileInfo(filePath);
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        var extension = Path.GetExtension(filePath);

        var metadata = new ModelMetadata
        {
            Name = ExtractModelNameFromFilename(fileName),
            FileFormat = GetFileFormatFromExtension(extension),
            FileSizeBytes = fileInfo.Length,
            AssetType = GetAssetTypeFromPath(filePath)
        };

        // Extract architecture information
        metadata.Architecture = ExtractArchitecture(fileName);

        // Extract parameter count
        metadata.ParameterCount = ExtractParameterCount(fileName);

        // Extract quantization format
        metadata.QuantizationFormat = ExtractQuantizationFormat(fileName);

        // Perform format-specific metadata extraction
        try
        {
            if (extension.Equals(".gguf", StringComparison.OrdinalIgnoreCase))
            {
                await ExtractGgufMetadataAsync(filePath, metadata, cancellationToken);
            }
            else if (extension.Equals(".safetensors", StringComparison.OrdinalIgnoreCase))
            {
                await ExtractSafeTensorsMetadataAsync(filePath, metadata, cancellationToken);
            }
            else if (extension.Equals(".json", StringComparison.OrdinalIgnoreCase))
            {
                await ExtractJsonMetadataAsync(filePath, metadata, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract detailed metadata from {FilePath}", filePath);
            metadata.ValidationResult = $"Metadata extraction warning: {ex.Message}";
            metadata.IsValid = false;
        }

        _logger.LogDebug("Extracted metadata for {FilePath}: Architecture={Architecture}, Parameters={Parameters}, Quantization={Quantization}", 
            filePath, metadata.Architecture, metadata.ParameterCount, metadata.QuantizationFormat);

        return metadata;
    }

    /// <inheritdoc />
    public async Task<string> ComputeFileHashAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            throw new FileNotFoundException("File not found", filePath);
        }

        _logger.LogDebug("Computing SHA256 hash for file: {FilePath}", filePath);

        using var sha256 = SHA256.Create();
        using var stream = File.OpenRead(filePath);

        var hashBytes = await Task.Run(() => sha256.ComputeHash(stream), cancellationToken);
        var hashString = Convert.ToHexString(hashBytes).ToLowerInvariant();

        _logger.LogDebug("Computed hash for {FilePath}: {Hash}", filePath, hashString);
        return hashString;
    }

    /// <inheritdoc />
    public IEnumerable<string> GetSupportedExtensions()
    {
        return SupportedExtensions.ToArray();
    }

    /// <inheritdoc />
    public decimal EstimateVramRequirement(ModelMetadata metadata)
    {
        // Base estimation logic - this can be enhanced with more sophisticated algorithms
        var baseEstimate = EstimateBaseVramFromParameters(metadata.ParameterCount);
        var quantizationMultiplier = GetQuantizationMultiplier(metadata.QuantizationFormat);

        var estimate = baseEstimate * quantizationMultiplier;

        _logger.LogDebug("Estimated VRAM for {Name}: {Estimate}GB (Base: {Base}GB, Multiplier: {Multiplier})", 
            metadata.Name, estimate, baseEstimate, quantizationMultiplier);

        return estimate;
    }

    /// <inheritdoc />
    public IEnumerable<string> GetCompatibleRunners(ModelMetadata metadata)
    {
        var runners = new List<string>();

        // GGUF files are primarily compatible with llama.cpp
        if (metadata.FileFormat.Equals("GGUF", StringComparison.OrdinalIgnoreCase))
        {
            runners.Add("llama.cpp");
            runners.Add("llamafile");
            runners.Add("ollama");
        }

        // SafeTensors and other formats are compatible with vLLM and transformers
        if (metadata.FileFormat.Equals("SafeTensors", StringComparison.OrdinalIgnoreCase) ||
            metadata.FileFormat.Equals("PyTorch", StringComparison.OrdinalIgnoreCase))
        {
            runners.Add("vLLM");
            runners.Add("transformers");
            runners.Add("ExLlamaV2");
        }

        // All formats can potentially work with generic runners
        runners.Add("generic");

        return runners.Distinct();
    }

    #region Private Helper Methods

    private string ExtractModelNameFromFilename(string fileName)
    {
        // Clean up common filename patterns to extract a readable model name
        var cleanName = fileName
            .Replace("_", " ")
            .Replace("-", " ");

        // Remove common suffixes
        var suffixesToRemove = new[] { "gguf", "safetensors", "bin", "pth", "pt", "Q4", "Q5", "Q6", "Q8", "F16", "F32" };
        
        foreach (var suffix in suffixesToRemove)
        {
            cleanName = Regex.Replace(cleanName, $@"\b{suffix}\b", "", RegexOptions.IgnoreCase);
        }

        return cleanName.Trim();
    }

    private string GetFileFormatFromExtension(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".gguf" => "GGUF",
            ".safetensors" => "SafeTensors",
            ".bin" => "PyTorch",
            ".pth" => "PyTorch",
            ".pt" => "PyTorch",
            ".json" => "JSON",
            ".model" => "Generic",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// Determines asset type exclusively from folder hierarchy - enforces strict folder-based taxonomy
    /// </summary>
    /// <param name="filePath">The full file path</param>
    /// <returns>Asset type based on folder location</returns>
    private LlmAssetType GetAssetTypeFromPath(string filePath)
    {
        var timestamp = DateTime.Now;
        _logger.LogDebug("[ASSET.KEEPER.CLASSIFICATION] {Timestamp:HH:mm:ss.fff} FOLDER-BASED CLASSIFICATION: '{FilePath}'", timestamp, filePath);
        
        var normalizedPath = Path.GetFullPath(filePath).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        _logger.LogDebug("[ASSET.KEEPER.CLASSIFICATION] {Timestamp:HH:mm:ss.fff} NORMALIZED PATH: '{NormalizedPath}'", DateTime.Now, normalizedPath);
        
        // Extract directory segments for analysis
        var pathSegments = normalizedPath.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
        
        // Find the folder that determines asset type (search backwards from file location)
        for (int i = pathSegments.Length - 2; i >= 0; i--) // -2 to skip the filename itself
        {
            var segment = pathSegments[i];
            _logger.LogDebug("[ASSET.KEEPER.CLASSIFICATION] {Timestamp:HH:mm:ss.fff} CHECKING SEGMENT: '{Segment}'", DateTime.Now, segment);
            
            var assetType = segment.ToLowerInvariant() switch
            {
                "base-models" => LlmAssetType.BaseModel,
                "lora-adapters" => LlmAssetType.LoRAAdapter,
                "embeddings" => LlmAssetType.Embedding,
                "tokenizers" => LlmAssetType.Tokenizer,
                "configs" or "config" => LlmAssetType.Config,
                "datasets" => LlmAssetType.Dataset,
                _ => (LlmAssetType?)null
            };
            
            if (assetType.HasValue)
            {
                _logger.LogInformation("[ASSET.KEEPER.CLASSIFICATION] {Timestamp:HH:mm:ss.fff} FOLDER-BASED TYPE DETERMINED: '{FilePath}' -> {AssetType} (from folder '{Segment}')", 
                    DateTime.Now, filePath, assetType.Value, segment);
                return assetType.Value;
            }
        }
        
        // Default fallback - log the classification failure
        _logger.LogWarning("[ASSET.KEEPER.CLASSIFICATION] {Timestamp:HH:mm:ss.fff} FOLDER-BASED CLASSIFICATION FAILED: '{FilePath}' - no recognized folder hierarchy found. Defaulting to BaseModel.", 
            DateTime.Now, filePath);
        _logger.LogWarning("[ASSET.KEEPER.CLASSIFICATION] Path segments analyzed: [{Segments}]", string.Join(" > ", pathSegments));
        
        return LlmAssetType.BaseModel;
    }
    
    /// <summary>
    /// Determines asset type exclusively from folder hierarchy - DEPRECATED, use GetAssetTypeFromPath
    /// </summary>
    /// <param name="filePath">The full file path</param>
    /// <param name="fileName">The filename (ignored - kept for compatibility)</param>
    /// <returns>Asset type based on folder location</returns>
    private LlmAssetType DetermineAssetType(string filePath, string fileName)
    {
        _logger.LogWarning("[ASSET.KEEPER.CLASSIFICATION] Using deprecated DetermineAssetType method. Redirecting to folder-based classification.");
        return GetAssetTypeFromPath(filePath);
    }

    private string? ExtractArchitecture(string fileName)
    {
        foreach (var (architecture, pattern) in ArchitecturePatterns)
        {
            if (pattern.IsMatch(fileName))
            {
                return architecture;
            }
        }

        return null;
    }

    private string? ExtractParameterCount(string fileName)
    {
        var match = ParameterCountPattern.Match(fileName);
        return match.Success ? $"{match.Groups[1].Value}B" : null;
    }

    private string? ExtractQuantizationFormat(string fileName)
    {
        var match = QuantizationPattern.Match(fileName);
        return match.Success ? match.Value.ToUpperInvariant() : null;
    }

    private async Task<bool> IsValidGgufFileAsync(string filePath, CancellationToken cancellationToken)
    {
        try
        {
            using var stream = File.OpenRead(filePath);
            var buffer = new byte[4];
            await stream.ReadAsync(buffer, 0, 4, cancellationToken);
            
            // GGUF files start with the magic bytes "GGUF"
            return Encoding.ASCII.GetString(buffer) == "GGUF";
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> IsValidSafeTensorsFileAsync(string filePath, CancellationToken cancellationToken)
    {
        try
        {
            using var stream = File.OpenRead(filePath);
            var buffer = new byte[8];
            await stream.ReadAsync(buffer, 0, 8, cancellationToken);
            
            // SafeTensors files have a specific header structure
            return buffer.Length == 8; // Simple validation - could be enhanced
        }
        catch
        {
            return false;
        }
    }

    private async Task ExtractGgufMetadataAsync(string filePath, ModelMetadata metadata, CancellationToken cancellationToken)
    {
        // GGUF metadata extraction would require parsing the binary format
        // This is a placeholder for more sophisticated parsing
        await Task.Delay(1, cancellationToken); // Simulate async work
        
        metadata.AdditionalMetadata["format_version"] = "GGUF";
        metadata.AdditionalMetadata["extracted_from"] = "header_analysis";
    }

    private async Task ExtractSafeTensorsMetadataAsync(string filePath, ModelMetadata metadata, CancellationToken cancellationToken)
    {
        // SafeTensors metadata extraction
        await Task.Delay(1, cancellationToken); // Simulate async work
        
        metadata.AdditionalMetadata["format_version"] = "SafeTensors";
        metadata.AdditionalMetadata["extracted_from"] = "header_analysis";
    }

    private async Task ExtractJsonMetadataAsync(string filePath, ModelMetadata metadata, CancellationToken cancellationToken)
    {
        try
        {
            var jsonContent = await File.ReadAllTextAsync(filePath, cancellationToken);
            var jsonDoc = JsonDocument.Parse(jsonContent);
            
            // Extract relevant fields from JSON configuration
            if (jsonDoc.RootElement.TryGetProperty("model_type", out var modelType))
            {
                metadata.Architecture ??= modelType.GetString();
            }
            
            if (jsonDoc.RootElement.TryGetProperty("vocab_size", out var vocabSize))
            {
                metadata.AdditionalMetadata["vocab_size"] = vocabSize.GetInt32();
            }

            metadata.AssetType = LlmAssetType.Config;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse JSON metadata from {FilePath}", filePath);
        }
    }

    private decimal EstimateBaseVramFromParameters(string? parameterCount)
    {
        if (string.IsNullOrWhiteSpace(parameterCount))
        {
            return 4.0m; // Default estimate
        }

        var match = Regex.Match(parameterCount, @"(\d+(?:\.\d+)?)");
        if (!match.Success || !decimal.TryParse(match.Groups[1].Value, out var parameters))
        {
            return 4.0m;
        }

        // Rough estimation: parameters (in billions) * 2 bytes per parameter for FP16
        return parameters * 2.0m;
    }

    private decimal GetQuantizationMultiplier(string? quantizationFormat)
    {
        if (string.IsNullOrWhiteSpace(quantizationFormat))
        {
            return 1.0m;
        }

        return quantizationFormat.ToUpperInvariant() switch
        {
            var q when q.StartsWith("Q4") => 0.5m,  // 4-bit quantization
            var q when q.StartsWith("Q5") => 0.625m, // 5-bit quantization
            var q when q.StartsWith("Q6") => 0.75m,  // 6-bit quantization
            var q when q.StartsWith("Q8") => 1.0m,   // 8-bit quantization
            var q when q.StartsWith("F16") => 2.0m,  // 16-bit float
            var q when q.StartsWith("F32") => 4.0m,  // 32-bit float
            _ => 1.0m
        };
    }

    #endregion
}