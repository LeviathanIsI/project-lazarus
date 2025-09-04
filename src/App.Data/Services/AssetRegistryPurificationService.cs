using System.Security.Cryptography;
using System.Text;
using Lazarus.App.Data.Repositories;
using Lazarus.App.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lazarus.App.Data.Services;

/// <summary>
/// Service implementation for purifying the asset registry by eliminating phantom entries
/// and maintaining strict filesystem-first validation
/// </summary>
public class AssetRegistryPurificationService : IAssetRegistryPurificationService
{
    private readonly LazarusDbContext _context;
    private readonly ILlmAssetRepository _assetRepository;
    private readonly ILogger<AssetRegistryPurificationService> _logger;

    /// <summary>
    /// Supported asset file extensions
    /// </summary>
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".gguf", ".safetensors", ".bin", ".json", ".yaml", ".yml", ".txt", ".csv", ".jsonl"
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="AssetRegistryPurificationService"/> class
    /// </summary>
    /// <param name="context">The database context</param>
    /// <param name="assetRepository">The asset repository</param>
    /// <param name="logger">The logger</param>
    public AssetRegistryPurificationService(
        LazarusDbContext context,
        ILlmAssetRepository assetRepository,
        ILogger<AssetRegistryPurificationService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _assetRepository = assetRepository ?? throw new ArgumentNullException(nameof(assetRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<AssetRegistryPurificationResult> PurgeAllPhantomEntriesAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            _logger.LogInformation("Beginning complete database exorcism - purging all phantom asset entries");

            // Get count before purification
            var phantomCount = await _context.LlmAssets.CountAsync(cancellationToken);
            
            // NUCLEAR OPTION: Truncate entire LlmAssets table to eliminate all phantom consciousness
            if (phantomCount > 0)
            {
                _logger.LogWarning("PURGING {PhantomCount} phantom asset entries from database", phantomCount);
                
                // Use raw SQL for complete table truncation - more aggressive than soft delete
                await _context.Database.ExecuteSqlRawAsync("DELETE FROM LlmAssets", cancellationToken);
                
                // Reset identity seed if using auto-increment (SQLite specific)
                await _context.Database.ExecuteSqlRawAsync("DELETE FROM sqlite_sequence WHERE name='LlmAssets'", cancellationToken);
                
                _logger.LogInformation("Successfully eliminated {PhantomCount} phantom asset entries", phantomCount);
            }

            stopwatch.Stop();

            return new AssetRegistryPurificationResult
            {
                PhantomsEliminated = phantomCount,
                Success = true,
                Duration = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Failed to purge phantom entries from asset registry");
            
            return new AssetRegistryPurificationResult
            {
                PhantomsEliminated = 0,
                Success = false,
                ErrorMessage = ex.Message,
                Duration = stopwatch.Elapsed
            };
        }
    }

    /// <inheritdoc />
    public async Task<AssetRegistryCleanupResult> CleanupOrphanedAssetsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting orphaned asset cleanup - validating all database entries against filesystem");

            var allAssets = await _context.LlmAssets.ToListAsync(cancellationToken);
            var orphansRemoved = 0;
            var validAssetsRetained = 0;
            var removedPaths = new List<string>();

            foreach (var asset in allAssets)
            {
                var fileExists = await ValidateAssetFileExistsAsync(asset.FilePath, cancellationToken);
                
                if (!fileExists)
                {
                    _logger.LogWarning("Removing orphaned asset entry: {AssetName} - File not found at {FilePath}", 
                        asset.Name, asset.FilePath);
                    
                    _context.LlmAssets.Remove(asset);
                    orphansRemoved++;
                    removedPaths.Add(asset.FilePath);
                }
                else
                {
                    // Verify file hash and update if necessary
                    var currentHash = await ComputeFileHashAsync(asset.FilePath, cancellationToken);
                    if (currentHash != asset.FileHash)
                    {
                        _logger.LogInformation("Updating file hash for asset: {AssetName}", asset.Name);
                        asset.FileHash = currentHash;
                        asset.Status = LlmAssetStatus.Available;
                    }
                    
                    validAssetsRetained++;
                }
            }

            if (orphansRemoved > 0)
            {
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Cleanup complete: removed {OrphansRemoved} orphaned entries, retained {ValidAssetsRetained} valid entries", 
                    orphansRemoved, validAssetsRetained);
            }

            return new AssetRegistryCleanupResult
            {
                OrphansRemoved = orphansRemoved,
                ValidAssetsRetained = validAssetsRetained,
                Success = true,
                RemovedAssetPaths = removedPaths
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup orphaned assets");
            
            return new AssetRegistryCleanupResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <inheritdoc />
    public async Task<bool> ValidateAssetFileExistsAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return false;
        }

        try
        {
            // Use Task.Run to avoid blocking on file I/O
            return await Task.Run(() =>
            {
                return File.Exists(filePath) && IsAccessibleFile(filePath);
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to validate file existence: {FilePath}", filePath);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<LlmAsset?> RegisterAssetWithValidationAsync(string filePath, LlmAssetType assetType, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            _logger.LogWarning("Cannot register asset: file path is null or empty");
            return null;
        }

        // FILESYSTEM-FIRST VALIDATION: Only register if file physically exists
        var fileExists = await ValidateAssetFileExistsAsync(filePath, cancellationToken);
        if (!fileExists)
        {
            _logger.LogWarning("Cannot register asset: file does not exist at path {FilePath}", filePath);
            return null;
        }

        try
        {
            // Check if asset already exists in database
            var existingAsset = await _assetRepository.GetByFilePathAsync(filePath, cancellationToken);
            if (existingAsset != null)
            {
                _logger.LogInformation("Asset already registered: {FilePath}", filePath);
                return existingAsset;
            }

            // Get file information
            var fileInfo = new FileInfo(filePath);
            var fileHash = await ComputeFileHashAsync(filePath, cancellationToken);
            
            // Create new asset with strict validation
            var asset = new LlmAsset
            {
                Id = Guid.NewGuid(),
                Name = Path.GetFileNameWithoutExtension(filePath),
                FilePath = Path.GetFullPath(filePath), // Ensure absolute path
                FileHash = fileHash,
                AssetType = assetType,
                FileSizeBytes = fileInfo.Length,
                Status = LlmAssetStatus.Available,
                IsValidated = true,
                ValidationResult = "File exists and is accessible",
                QuantizationFormat = DetectQuantizationFormat(filePath),
                ParameterCount = DetectParameterCount(filePath),
                Architecture = DetectArchitecture(filePath),
                CompatibleRunners = GetCompatibleRunners(assetType, filePath)
            };

            // Set VRAM estimate based on file size and quantization
            asset.VramEstimateGb = EstimateVramRequirement(asset);

            var registeredAsset = await _assetRepository.AddAsync(asset, cancellationToken);
            
            _logger.LogInformation("Successfully registered asset: {AssetName} at {FilePath} with hash {FileHash}", 
                asset.Name, asset.FilePath, asset.FileHash);

            return registeredAsset;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register asset: {FilePath}", filePath);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<AssetRegistryHygieneResult> PerformRegistryHygieneAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Performing automatic registry hygiene check");

            var allAssets = await _context.LlmAssets.ToListAsync(cancellationToken);
            var missingFiles = 0;
            var cleanedUp = 0;

            foreach (var asset in allAssets)
            {
                var fileExists = await ValidateAssetFileExistsAsync(asset.FilePath, cancellationToken);
                
                if (!fileExists)
                {
                    missingFiles++;
                    
                    // Mark as missing rather than immediately removing to allow recovery
                    if (asset.Status != LlmAssetStatus.Missing)
                    {
                        asset.Status = LlmAssetStatus.Missing;
                        asset.ValidationResult = $"File not found at {asset.FilePath} during hygiene check at {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss}";
                        cleanedUp++;
                        
                        _logger.LogWarning("Marked asset as missing: {AssetName} at {FilePath}", asset.Name, asset.FilePath);
                    }
                }
                else if (asset.Status == LlmAssetStatus.Missing)
                {
                    // File was missing but now exists - restore it
                    asset.Status = LlmAssetStatus.Available;
                    asset.ValidationResult = "File restored during hygiene check";
                    cleanedUp++;
                    
                    _logger.LogInformation("Restored previously missing asset: {AssetName} at {FilePath}", asset.Name, asset.FilePath);
                }
            }

            if (cleanedUp > 0)
            {
                await _context.SaveChangesAsync(cancellationToken);
            }

            return new AssetRegistryHygieneResult
            {
                MissingFilesDetected = missingFiles,
                EntriesCleanedUp = cleanedUp,
                Success = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to perform registry hygiene");
            
            return new AssetRegistryHygieneResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <inheritdoc />
    public async Task<AssetDiscoveryResult> DiscoverAndReconcileAssetsAsync(IEnumerable<string> directoriesToScan, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting asset discovery and reconciliation in {DirectoryCount} directories", 
                directoriesToScan.Count());

            var discoveredPaths = new List<string>();
            var newAssetsRegistered = 0;
            var existingAssetsUpdated = 0;

            foreach (var directory in directoriesToScan)
            {
                if (!Directory.Exists(directory))
                {
                    _logger.LogWarning("Directory not found: {Directory}", directory);
                    continue;
                }

                var files = Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories)
                    .Where(f => SupportedExtensions.Contains(Path.GetExtension(f)))
                    .ToList();

                discoveredPaths.AddRange(files);

                foreach (var file in files)
                {
                    var assetType = DetermineAssetType(file);
                    var existingAsset = await _assetRepository.GetByFilePathAsync(file, cancellationToken);

                    if (existingAsset == null)
                    {
                        // New asset discovered
                        var registeredAsset = await RegisterAssetWithValidationAsync(file, assetType, cancellationToken);
                        if (registeredAsset != null)
                        {
                            newAssetsRegistered++;
                        }
                    }
                    else
                    {
                        // Existing asset - verify hash
                        var currentHash = await ComputeFileHashAsync(file, cancellationToken);
                        if (currentHash != existingAsset.FileHash)
                        {
                            existingAsset.FileHash = currentHash;
                            existingAsset.Status = LlmAssetStatus.Available;
                            await _assetRepository.UpdateAsync(existingAsset, cancellationToken);
                            existingAssetsUpdated++;
                        }
                    }
                }
            }

            _logger.LogInformation("Asset discovery complete: {FilesDiscovered} files discovered, {NewAssets} new assets registered, {ExistingAssets} existing assets updated",
                discoveredPaths.Count, newAssetsRegistered, existingAssetsUpdated);

            return new AssetDiscoveryResult
            {
                FilesDiscovered = discoveredPaths.Count,
                NewAssetsRegistered = newAssetsRegistered,
                ExistingAssetsUpdated = existingAssetsUpdated,
                Success = true,
                DiscoveredAssetPaths = discoveredPaths
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to discover and reconcile assets");
            
            return new AssetDiscoveryResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Computes SHA256 hash of a file
    /// </summary>
    private async Task<string> ComputeFileHashAsync(string filePath, CancellationToken cancellationToken = default)
    {
        using var sha256 = SHA256.Create();
        using var stream = File.OpenRead(filePath);
        
        var hashBytes = await sha256.ComputeHashAsync(stream, cancellationToken);
        return Convert.ToHexString(hashBytes);
    }

    /// <summary>
    /// Checks if a file is accessible for reading
    /// </summary>
    private bool IsAccessibleFile(string filePath)
    {
        try
        {
            using var stream = File.OpenRead(filePath);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Determines the asset type based on file extension and path
    /// </summary>
    private LlmAssetType DetermineAssetType(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        var fileName = Path.GetFileName(filePath).ToLowerInvariant();

        return extension switch
        {
            ".gguf" => LlmAssetType.BaseModel,
            ".safetensors" when fileName.Contains("lora") || fileName.Contains("adapter") => LlmAssetType.LoRAAdapter,
            ".safetensors" => LlmAssetType.BaseModel,
            ".bin" when fileName.Contains("lora") || fileName.Contains("adapter") => LlmAssetType.LoRAAdapter,
            ".bin" => LlmAssetType.BaseModel,
            ".json" when fileName.Contains("tokenizer") => LlmAssetType.Tokenizer,
            ".json" => LlmAssetType.Config,
            ".yaml" or ".yml" => LlmAssetType.Config,
            ".txt" or ".csv" or ".jsonl" => LlmAssetType.Dataset,
            _ => LlmAssetType.Config
        };
    }

    /// <summary>
    /// Detects quantization format from filename
    /// </summary>
    private string? DetectQuantizationFormat(string filePath)
    {
        var fileName = Path.GetFileName(filePath).ToUpperInvariant();
        
        var quantFormats = new[] { "Q4_K_M", "Q5_K_M", "Q8_0", "Q4_0", "Q5_0", "Q6_K", "F16", "F32" };
        
        return quantFormats.FirstOrDefault(format => fileName.Contains(format));
    }

    /// <summary>
    /// Detects parameter count from filename
    /// </summary>
    private string? DetectParameterCount(string filePath)
    {
        var fileName = Path.GetFileName(filePath).ToUpperInvariant();
        
        var paramCounts = new[] { "70B", "32B", "13B", "7B", "3B", "1B" };
        
        return paramCounts.FirstOrDefault(param => fileName.Contains(param));
    }

    /// <summary>
    /// Detects model architecture from filename
    /// </summary>
    private string? DetectArchitecture(string filePath)
    {
        var fileName = Path.GetFileName(filePath).ToLowerInvariant();
        
        if (fileName.Contains("llama")) return "Llama";
        if (fileName.Contains("qwen")) return "Qwen";
        if (fileName.Contains("mistral")) return "Mistral";
        if (fileName.Contains("phi")) return "Phi";
        if (fileName.Contains("gemma")) return "Gemma";
        
        return null;
    }

    /// <summary>
    /// Gets compatible runners for an asset type
    /// </summary>
    private string GetCompatibleRunners(LlmAssetType assetType, string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        var runners = new List<string>();

        switch (assetType)
        {
            case LlmAssetType.BaseModel:
                if (extension == ".gguf")
                {
                    runners.AddRange(new[] { "llama.cpp", "koboldcpp" });
                }
                else if (extension == ".safetensors")
                {
                    runners.AddRange(new[] { "vllm", "exllamav2", "transformers" });
                }
                break;
            
            case LlmAssetType.LoRAAdapter:
                runners.AddRange(new[] { "vllm", "transformers" });
                break;
                
            default:
                runners.Add("universal");
                break;
        }

        return System.Text.Json.JsonSerializer.Serialize(runners);
    }

    /// <summary>
    /// Estimates VRAM requirement based on file size and quantization
    /// </summary>
    private decimal? EstimateVramRequirement(LlmAsset asset)
    {
        if (asset.AssetType != LlmAssetType.BaseModel)
        {
            return null;
        }

        // Base estimate from file size (rough approximation)
        var baseEstimate = asset.FileSizeBytes / (1024m * 1024m * 1024m);

        // Adjust for quantization
        if (!string.IsNullOrEmpty(asset.QuantizationFormat))
        {
            var multiplier = asset.QuantizationFormat switch
            {
                "Q4_K_M" => 1.2m,
                "Q5_K_M" => 1.4m,
                "Q8_0" => 1.8m,
                "F16" => 2.2m,
                "F32" => 4.0m,
                _ => 1.5m
            };
            
            return Math.Round(baseEstimate * multiplier, 1);
        }

        return Math.Round(baseEstimate * 1.5m, 1);
    }
}