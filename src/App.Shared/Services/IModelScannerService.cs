using Lazarus.App.Shared.Models;

namespace Lazarus.App.Shared.Services;

/// <summary>
/// Service interface for scanning and discovering LLM model files on the filesystem
/// </summary>
public interface IModelScannerService
{
    /// <summary>
    /// Scans a directory for supported model files
    /// </summary>
    /// <param name="directoryPath">The directory path to scan</param>
    /// <param name="includeSubdirectories">Whether to scan subdirectories recursively</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>A collection of discovered model file paths</returns>
    Task<IEnumerable<string>> ScanDirectoryAsync(string directoryPath, bool includeSubdirectories = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates if a file is a supported model format
    /// </summary>
    /// <param name="filePath">The file path to validate</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>True if the file is a supported model format; otherwise false</returns>
    Task<bool> IsSupportedModelFileAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Extracts metadata from a model file
    /// </summary>
    /// <param name="filePath">The path to the model file</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>Model metadata if extraction was successful; otherwise null</returns>
    Task<ModelMetadata?> ExtractMetadataAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Computes the SHA256 hash of a file for integrity verification
    /// </summary>
    /// <param name="filePath">The file path</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The SHA256 hash as a hexadecimal string</returns>
    Task<string> ComputeFileHashAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the supported file extensions for model files
    /// </summary>
    /// <returns>A collection of supported file extensions (including the dot)</returns>
    IEnumerable<string> GetSupportedExtensions();

    /// <summary>
    /// Estimates VRAM requirements for a model based on its metadata
    /// </summary>
    /// <param name="metadata">The model metadata</param>
    /// <returns>Estimated VRAM requirement in GB</returns>
    decimal EstimateVramRequirement(ModelMetadata metadata);

    /// <summary>
    /// Determines compatible runners for a model based on its format and metadata
    /// </summary>
    /// <param name="metadata">The model metadata</param>
    /// <returns>A collection of compatible runner identifiers</returns>
    IEnumerable<string> GetCompatibleRunners(ModelMetadata metadata);
}

/// <summary>
/// Represents metadata extracted from a model file
/// </summary>
public class ModelMetadata
{
    /// <summary>
    /// Gets or sets the model name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the model architecture (e.g., Llama, Qwen, Mistral)
    /// </summary>
    public string? Architecture { get; set; }

    /// <summary>
    /// Gets or sets the parameter count (e.g., 7B, 13B, 32B, 70B)
    /// </summary>
    public string? ParameterCount { get; set; }

    /// <summary>
    /// Gets or sets the quantization format (e.g., Q4_K_M, Q5_K_M, F16)
    /// </summary>
    public string? QuantizationFormat { get; set; }

    /// <summary>
    /// Gets or sets the file format (e.g., GGUF, SafeTensors, PyTorch)
    /// </summary>
    public string FileFormat { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the file size in bytes
    /// </summary>
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// Gets or sets the asset type
    /// </summary>
    public LlmAssetType AssetType { get; set; } = LlmAssetType.BaseModel;

    /// <summary>
    /// Gets or sets additional metadata as key-value pairs
    /// </summary>
    public Dictionary<string, object> AdditionalMetadata { get; set; } = new();

    /// <summary>
    /// Gets or sets validation result information
    /// </summary>
    public string? ValidationResult { get; set; }

    /// <summary>
    /// Gets or sets whether the file passed validation
    /// </summary>
    public bool IsValid { get; set; } = true;
}