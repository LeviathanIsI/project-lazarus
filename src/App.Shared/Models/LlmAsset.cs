using System.ComponentModel.DataAnnotations;

namespace Lazarus.App.Shared.Models;

/// <summary>
/// Represents an LLM asset (model, LoRA adapter, tokenizer, etc.) managed by the Asset Keeper
/// </summary>
public class LlmAsset : BaseEntity
{
    /// <summary>
    /// Gets or sets the name of the asset
    /// </summary>
    [Required]
    [MaxLength(256)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the absolute file path to the asset
    /// </summary>
    [Required]
    [MaxLength(1024)]
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the SHA256 hash of the asset file for integrity verification
    /// </summary>
    [Required]
    [MaxLength(64)]
    public string FileHash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of asset (BaseModel, LoRAAdapter, Tokenizer, Config, Dataset)
    /// </summary>
    public LlmAssetType AssetType { get; set; }

    /// <summary>
    /// Gets or sets the file size in bytes
    /// </summary>
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// Gets or sets the quantization format for model files (e.g., Q4_K_M, Q5_K_M, F16, etc.)
    /// </summary>
    [MaxLength(32)]
    public string? QuantizationFormat { get; set; }

    /// <summary>
    /// Gets or sets the estimated parameter count (e.g., 7B, 13B, 32B, 70B)
    /// </summary>
    [MaxLength(16)]
    public string? ParameterCount { get; set; }

    /// <summary>
    /// Gets or sets the estimated VRAM requirement in GB
    /// </summary>
    public decimal? VramEstimateGb { get; set; }

    /// <summary>
    /// Gets or sets the model architecture family (e.g., Llama, Qwen, Mistral, etc.)
    /// </summary>
    [MaxLength(64)]
    public string? Architecture { get; set; }

    /// <summary>
    /// Gets or sets compatible inference runners as a JSON array of strings
    /// </summary>
    [MaxLength(512)]
    public string CompatibleRunners { get; set; } = "[]";

    /// <summary>
    /// Gets or sets the current loading status
    /// </summary>
    public LlmAssetStatus Status { get; set; } = LlmAssetStatus.Available;

    /// <summary>
    /// Gets or sets the timestamp when the asset was last loaded
    /// </summary>
    public DateTimeOffset? LastLoadedAt { get; set; }

    /// <summary>
    /// Gets or sets the runner ID currently using this asset (if active)
    /// </summary>
    [MaxLength(128)]
    public string? ActiveRunnerId { get; set; }

    /// <summary>
    /// Gets or sets additional metadata as JSON
    /// </summary>
    public string? MetadataJson { get; set; }

    /// <summary>
    /// Gets or sets the description or notes about this asset
    /// </summary>
    [MaxLength(1024)]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets validation result details (errors, warnings, etc.)
    /// </summary>
    [MaxLength(2048)]
    public string? ValidationResult { get; set; }

    /// <summary>
    /// Gets or sets whether this asset passed validation
    /// </summary>
    public bool IsValidated { get; set; }
}

/// <summary>
/// Defines the types of LLM assets that can be managed
/// </summary>
public enum LlmAssetType
{
    /// <summary>
    /// Base language model file (e.g., .gguf, .safetensors, .bin)
    /// </summary>
    BaseModel = 0,

    /// <summary>
    /// LoRA adapter weights
    /// </summary>
    LoRAAdapter = 1,

    /// <summary>
    /// Tokenizer files
    /// </summary>
    Tokenizer = 2,

    /// <summary>
    /// Configuration files (model config, generation config, etc.)
    /// </summary>
    Config = 3,

    /// <summary>
    /// Training or fine-tuning datasets
    /// </summary>
    Dataset = 4,

    /// <summary>
    /// Text and multimodal embedding models
    /// </summary>
    Embedding = 5
}

/// <summary>
/// Defines the possible status states of an LLM asset
/// </summary>
public enum LlmAssetStatus
{
    /// <summary>
    /// Asset is available and ready to be loaded
    /// </summary>
    Available = 0,

    /// <summary>
    /// Asset is currently being loaded into a runner
    /// </summary>
    Loading = 1,

    /// <summary>
    /// Asset is actively loaded and in use by a runner
    /// </summary>
    Active = 2,

    /// <summary>
    /// Asset failed to load or encountered an error
    /// </summary>
    Failed = 3,

    /// <summary>
    /// Asset file is missing or corrupted
    /// </summary>
    Missing = 4,

    /// <summary>
    /// Asset is being validated or scanned
    /// </summary>
    Validating = 5
}