using Lazarus.App.Data.Repositories;
using Lazarus.App.Shared.Models;
using Lazarus.App.Shared.Services;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Diagnostics;

namespace Lazarus.App.Data.Services;

/// <summary>
/// Implementation of the Asset Keeper service for managing LLM assets
/// </summary>
public class AssetKeeperService : IAssetKeeperService
{
    private readonly ILlmAssetRepository _assetRepository;
    private readonly IModelScannerService _scannerService;
    private readonly ILogger<AssetKeeperService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AssetKeeperService"/> class
    /// </summary>
    /// <param name="assetRepository">The asset repository</param>
    /// <param name="scannerService">The model scanner service</param>
    /// <param name="logger">The logger</param>
    public AssetKeeperService(
        ILlmAssetRepository assetRepository,
        IModelScannerService scannerService,
        ILogger<AssetKeeperService> logger)
    {
        _assetRepository = assetRepository ?? throw new ArgumentNullException(nameof(assetRepository));
        _scannerService = scannerService ?? throw new ArgumentNullException(nameof(scannerService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<int> ScanAndRegisterModelsAsync(string directoryPath, bool includeSubdirectories = true, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            throw new ArgumentException("Directory path cannot be null or empty", nameof(directoryPath));
        }

        // DIAGNOSTIC LOGGING: Enhanced filesystem scanning diagnostics
        _logger.LogInformation("[ASSET.KEEPER DIAGNOSTIC] Starting enhanced asset scan and registration");
        _logger.LogInformation("[ASSET.KEEPER DIAGNOSTIC] Target directory: {DirectoryPath}", directoryPath);
        _logger.LogInformation("[ASSET.KEEPER DIAGNOSTIC] Include subdirectories: {IncludeSubdirectories}", includeSubdirectories);
        _logger.LogInformation("[ASSET.KEEPER DIAGNOSTIC] Directory exists: {DirectoryExists}", Directory.Exists(directoryPath));
        
        if (Directory.Exists(directoryPath))
        {
            try
            {
                var allFiles = Directory.GetFiles(directoryPath, "*.*", includeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
                _logger.LogInformation("[ASSET.KEEPER DIAGNOSTIC] Raw filesystem scan found {FileCount} files", allFiles.Length);
                
                foreach (var file in allFiles.Take(10)) // Log first 10 files for diagnosis
                {
                    var extension = Path.GetExtension(file).ToLowerInvariant();
                    _logger.LogDebug("[ASSET.KEEPER DIAGNOSTIC] Found file: {FilePath} (ext: {Extension})", file, extension);
                }
                
                if (allFiles.Length > 10)
                {
                    _logger.LogDebug("[ASSET.KEEPER DIAGNOSTIC] ... and {RemainingCount} more files", allFiles.Length - 10);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ASSET.KEEPER DIAGNOSTIC] Error during raw filesystem scan of {DirectoryPath}", directoryPath);
            }
        }
        else
        {
            _logger.LogWarning("[ASSET.KEEPER DIAGNOSTIC] Target directory does not exist: {DirectoryPath}", directoryPath);
        }

        var discoveredFiles = await _scannerService.ScanDirectoryAsync(directoryPath, includeSubdirectories, cancellationToken);
        var newAssetsCount = 0;
        
        // DIAGNOSTIC LOGGING: Model scanner service results
        _logger.LogInformation("[ASSET.KEEPER DIAGNOSTIC] Scanner service discovered {FileCount} potential asset files", discoveredFiles.Count());
        
        var discoveredList = discoveredFiles.ToList();
        foreach (var discoveredFile in discoveredList)
        {
            _logger.LogDebug("[ASSET.KEEPER DIAGNOSTIC] Scanner discovered: {FilePath}", discoveredFile);
        }

        foreach (var filePath in discoveredFiles)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Check if asset is already registered
                var existingAsset = await _assetRepository.GetByFilePathAsync(filePath, cancellationToken);
                if (existingAsset != null)
                {
                    _logger.LogDebug("Asset already registered: {FilePath}", filePath);
                    continue;
                }

                // Register new asset
                var registeredAsset = await RegisterModelAsync(filePath, cancellationToken);
                if (registeredAsset != null)
                {
                    newAssetsCount++;
                    _logger.LogDebug("Successfully registered new asset: {AssetName}", registeredAsset.Name);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Failed to register asset from file: {FilePath}", filePath);
                // Continue with next file instead of failing entire operation
            }
        }

        _logger.LogInformation("[ASSET.KEEPER DIAGNOSTIC] Asset scan completed. Registered {NewCount} new assets from {TotalCount} discovered files", 
            newAssetsCount, discoveredFiles.Count());
        _logger.LogInformation("[ASSET.KEEPER DIAGNOSTIC] Scan summary - Directory: {DirectoryPath}, New Assets: {NewAssetsCount}, Success Rate: {SuccessRate:P}", 
            directoryPath, newAssetsCount, discoveredFiles.Any() ? (double)newAssetsCount / discoveredFiles.Count() : 0.0);

        return newAssetsCount;
    }

    /// <inheritdoc />
    public async Task<LlmAsset?> RegisterModelAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            _logger.LogWarning("File does not exist: {FilePath}", filePath);
            return null;
        }

        _logger.LogDebug("Registering asset: {FilePath}", filePath);

        try
        {
            // Extract metadata from file
            var metadata = await _scannerService.ExtractMetadataAsync(filePath, cancellationToken);
            if (metadata == null)
            {
                _logger.LogWarning("Failed to extract metadata from file: {FilePath}", filePath);
                return null;
            }

            // Compute file hash for integrity verification
            var fileHash = await _scannerService.ComputeFileHashAsync(filePath, cancellationToken);

            // Check if asset with same hash already exists
            var existingByHash = await _assetRepository.GetByFileHashAsync(fileHash, cancellationToken);
            if (existingByHash != null)
            {
                _logger.LogInformation("Asset with same hash already exists: {ExistingPath} matches {NewPath}", 
                    existingByHash.FilePath, filePath);
                return existingByHash;
            }

            // Estimate VRAM requirements
            var vramEstimate = _scannerService.EstimateVramRequirement(metadata);

            // Get compatible runners
            var compatibleRunners = _scannerService.GetCompatibleRunners(metadata);

            // Create LLM asset entity
            var asset = new LlmAsset
            {
                Name = metadata.Name,
                FilePath = Path.GetFullPath(filePath),
                FileHash = fileHash,
                AssetType = metadata.AssetType,
                FileSizeBytes = metadata.FileSizeBytes,
                QuantizationFormat = metadata.QuantizationFormat,
                ParameterCount = metadata.ParameterCount,
                VramEstimateGb = vramEstimate,
                Architecture = metadata.Architecture,
                CompatibleRunners = JsonSerializer.Serialize(compatibleRunners),
                Status = metadata.IsValid ? LlmAssetStatus.Available : LlmAssetStatus.Failed,
                MetadataJson = JsonSerializer.Serialize(metadata.AdditionalMetadata),
                ValidationResult = metadata.ValidationResult,
                IsValidated = metadata.IsValid,
                Description = $"{metadata.Architecture} {metadata.ParameterCount} model ({metadata.FileFormat} format)"
            };

            // Save to repository
            var registeredAsset = await _assetRepository.AddAsync(asset, cancellationToken);

            _logger.LogInformation("Successfully registered asset: {AssetId} - {AssetName} at {FilePath}", 
                registeredAsset.Id, registeredAsset.Name, registeredAsset.FilePath);

            return registeredAsset;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering asset from file: {FilePath}", filePath);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<LlmAsset>> GetAllAssetsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving all assets");
        return await _assetRepository.GetAllAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<LlmAsset>> GetAssetsByTypeAsync(LlmAssetType assetType, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving assets by type: {AssetType}", assetType);
        return await _assetRepository.GetByTypeAsync(assetType, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<LlmAsset>> GetAssetsByStatusAsync(LlmAssetStatus status, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving assets by status: {Status}", status);
        return await _assetRepository.GetByStatusAsync(status, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<LlmAsset>> SearchAssetsAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Searching assets with term: {SearchTerm}", searchTerm);
        return await _assetRepository.SearchAsync(searchTerm, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ValidateAssetAsync(Guid assetId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Validating asset: {AssetId}", assetId);

        var asset = await _assetRepository.GetByIdAsync(assetId, cancellationToken);
        if (asset == null)
        {
            _logger.LogWarning("Asset not found for validation: {AssetId}", assetId);
            return false;
        }

        try
        {
            // Check if file exists
            if (!File.Exists(asset.FilePath))
            {
                _logger.LogWarning("Asset file missing: {FilePath}", asset.FilePath);
                await _assetRepository.UpdateStatusAsync(assetId, LlmAssetStatus.Missing, null, cancellationToken);
                return false;
            }

            // Verify file hash
            var currentHash = await _scannerService.ComputeFileHashAsync(asset.FilePath, cancellationToken);
            if (!string.Equals(currentHash, asset.FileHash, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Asset file hash mismatch: {FilePath}. Expected: {ExpectedHash}, Actual: {ActualHash}", 
                    asset.FilePath, asset.FileHash, currentHash);
                
                asset.ValidationResult = "File hash mismatch - file may have been modified";
                asset.IsValidated = false;
                await _assetRepository.UpdateAsync(asset, cancellationToken);
                return false;
            }

            // Validate file format
            var isSupported = await _scannerService.IsSupportedModelFileAsync(asset.FilePath, cancellationToken);
            if (!isSupported)
            {
                _logger.LogWarning("Asset file format no longer supported: {FilePath}", asset.FilePath);
                asset.ValidationResult = "File format is not supported";
                asset.IsValidated = false;
                await _assetRepository.UpdateAsync(asset, cancellationToken);
                return false;
            }

            // Update validation status
            asset.ValidationResult = "Validation passed";
            asset.IsValidated = true;
            asset.Status = LlmAssetStatus.Available;
            await _assetRepository.UpdateAsync(asset, cancellationToken);

            _logger.LogInformation("Asset validation passed: {AssetId} - {AssetName}", assetId, asset.Name);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating asset: {AssetId}", assetId);
            
            asset.ValidationResult = $"Validation error: {ex.Message}";
            asset.IsValidated = false;
            asset.Status = LlmAssetStatus.Failed;
            await _assetRepository.UpdateAsync(asset, cancellationToken);
            
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> UpdateAssetStatusAsync(Guid assetId, LlmAssetStatus status, string? activeRunnerId = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating asset status: {AssetId} to {Status} with runner {RunnerId}", assetId, status, activeRunnerId);
        return await _assetRepository.UpdateStatusAsync(assetId, status, activeRunnerId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> RemoveAssetAsync(Guid assetId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Removing asset: {AssetId}", assetId);
        return await _assetRepository.DeleteAsync(assetId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<AssetSummary> GetAssetSummaryAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Generating asset summary");

        var allAssets = await _assetRepository.GetAllAsync(cancellationToken);
        var assetsList = allAssets.ToList();

        var summary = new AssetSummary
        {
            TotalAssets = assetsList.Count,
            TotalStorageBytes = assetsList.Sum(a => a.FileSizeBytes),
            EstimatedTotalVramGb = assetsList.Sum(a => a.VramEstimateGb ?? 0)
        };

        // Count by type
        summary.CountsByType = assetsList
            .GroupBy(a => a.AssetType)
            .ToDictionary(g => g.Key, g => g.Count());

        // Count by status
        summary.CountsByStatus = assetsList
            .GroupBy(a => a.Status)
            .ToDictionary(g => g.Key, g => g.Count());

        return summary;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<LlmAsset>> GetCompatibleAssetsAsync(string runnerId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting assets compatible with runner: {RunnerId}", runnerId);
        return await _assetRepository.GetCompatibleWithRunnerAsync(runnerId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<LlmAsset?> RefreshAssetMetadataAsync(Guid assetId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Refreshing metadata for asset: {AssetId}", assetId);

        var asset = await _assetRepository.GetByIdAsync(assetId, cancellationToken);
        if (asset == null)
        {
            _logger.LogWarning("Asset not found for metadata refresh: {AssetId}", assetId);
            return null;
        }

        if (!File.Exists(asset.FilePath))
        {
            _logger.LogWarning("Asset file missing for metadata refresh: {FilePath}", asset.FilePath);
            await _assetRepository.UpdateStatusAsync(assetId, LlmAssetStatus.Missing, null, cancellationToken);
            return asset;
        }

        try
        {
            // Re-extract metadata
            var metadata = await _scannerService.ExtractMetadataAsync(asset.FilePath, cancellationToken);
            if (metadata == null)
            {
                _logger.LogWarning("Failed to refresh metadata for asset: {AssetId}", assetId);
                return asset;
            }

            // Update asset with fresh metadata
            asset.Name = metadata.Name;
            asset.QuantizationFormat = metadata.QuantizationFormat;
            asset.ParameterCount = metadata.ParameterCount;
            asset.Architecture = metadata.Architecture;
            asset.VramEstimateGb = _scannerService.EstimateVramRequirement(metadata);
            asset.CompatibleRunners = JsonSerializer.Serialize(_scannerService.GetCompatibleRunners(metadata));
            asset.MetadataJson = JsonSerializer.Serialize(metadata.AdditionalMetadata);
            asset.ValidationResult = metadata.ValidationResult;
            asset.IsValidated = metadata.IsValid;
            asset.Status = metadata.IsValid ? LlmAssetStatus.Available : LlmAssetStatus.Failed;

            // Update file hash in case file was modified
            asset.FileHash = await _scannerService.ComputeFileHashAsync(asset.FilePath, cancellationToken);

            // Save updated asset
            var updatedAsset = await _assetRepository.UpdateAsync(asset, cancellationToken);

            _logger.LogInformation("Successfully refreshed metadata for asset: {AssetId} - {AssetName}", assetId, asset.Name);
            return updatedAsset;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing metadata for asset: {AssetId}", assetId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<int> ValidateAssetFilesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Validating all asset files");

        var allAssets = await _assetRepository.GetAllAsync(cancellationToken);
        var missingCount = 0;

        foreach (var asset in allAssets)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!File.Exists(asset.FilePath))
                {
                    _logger.LogWarning("Asset file missing: {FilePath} for asset {AssetId}", asset.FilePath, asset.Id);
                    await _assetRepository.UpdateStatusAsync(asset.Id, LlmAssetStatus.Missing, null, cancellationToken);
                    missingCount++;
                }
                else if (asset.Status == LlmAssetStatus.Missing)
                {
                    // File was restored, mark as available
                    _logger.LogInformation("Asset file restored: {FilePath} for asset {AssetId}", asset.FilePath, asset.Id);
                    await _assetRepository.UpdateStatusAsync(asset.Id, LlmAssetStatus.Available, null, cancellationToken);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error validating asset file: {AssetId}", asset.Id);
            }
        }

        _logger.LogInformation("Asset file validation completed. Found {MissingCount} missing files", missingCount);
        return missingCount;
    }

    /// <summary>
    /// Manual debugging method to force complete filesystem diagnostics
    /// </summary>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>Detailed diagnostic report</returns>
    public async Task<string> ForceFilesystemDiagnosticsAsync(CancellationToken cancellationToken = default)
    {
        var diagnosticReport = new List<string>();
        var stopwatch = Stopwatch.StartNew();
        
        _logger.LogInformation("[ASSET.KEEPER MANUAL DEBUG] Starting comprehensive filesystem diagnostics");
        diagnosticReport.Add("=== ASSET.KEEPER FILESYSTEM DIAGNOSTICS ===");
        diagnosticReport.Add($"Started at: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
        
        try
        {
            // Step 1: Test Environment.GetFolderPath resolution
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _logger.LogInformation("[DIAGNOSTIC] LocalApplicationData resolved to: {Path}", localAppData);
            diagnosticReport.Add($"LocalApplicationData Path: {localAppData}");
            diagnosticReport.Add($"LocalApplicationData Exists: {Directory.Exists(localAppData)}");
            
            // Step 2: Test Lazarus root path construction
            var lazarusRoot = Path.Combine(localAppData, "Lazarus");
            _logger.LogInformation("[DIAGNOSTIC] Lazarus root path: {Path}", lazarusRoot);
            diagnosticReport.Add($"Lazarus Root Path: {lazarusRoot}");
            diagnosticReport.Add($"Lazarus Root Exists: {Directory.Exists(lazarusRoot)}");
            
            // Step 3: Test Models directory construction
            var modelsRoot = Path.Combine(lazarusRoot, "Models");
            _logger.LogInformation("[DIAGNOSTIC] Models root path: {Path}", modelsRoot);
            diagnosticReport.Add($"Models Root Path: {modelsRoot}");
            diagnosticReport.Add($"Models Root Exists: {Directory.Exists(modelsRoot)}");
            
            // Step 4: Test each asset subdirectory
            var assetDirectories = new Dictionary<string, string>
            {
                { "Base-Models", Path.Combine(modelsRoot, "Base-Models") },
                { "LoRA-Adapters", Path.Combine(modelsRoot, "LoRA-Adapters") },
                { "Embeddings", Path.Combine(modelsRoot, "Embeddings") },
                { "Tokenizers", Path.Combine(modelsRoot, "Tokenizers") }
            };
            
            foreach (var kvp in assetDirectories)
            {
                var exists = Directory.Exists(kvp.Value);
                _logger.LogInformation("[DIAGNOSTIC] {DirectoryType} directory: {Path} (Exists: {Exists})", kvp.Key, kvp.Value, exists);
                diagnosticReport.Add($"{kvp.Key} Directory: {kvp.Value}");
                diagnosticReport.Add($"{kvp.Key} Exists: {exists}");
                
                if (exists)
                {
                    try
                    {
                        var files = Directory.GetFiles(kvp.Value, "*.*", SearchOption.AllDirectories);
                        _logger.LogInformation("[DIAGNOSTIC] {DirectoryType} contains {FileCount} files", kvp.Key, files.Length);
                        diagnosticReport.Add($"{kvp.Key} File Count: {files.Length}");
                        
                        foreach (var file in files.Take(5))
                        {
                            var fileInfo = new FileInfo(file);
                            _logger.LogDebug("[DIAGNOSTIC] Found file: {FileName} ({Size} bytes)", file, fileInfo.Length);
                            diagnosticReport.Add($"  - {Path.GetFileName(file)} ({fileInfo.Length:N0} bytes)");
                        }
                        
                        if (files.Length > 5)
                        {
                            diagnosticReport.Add($"  ... and {files.Length - 5} more files");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[DIAGNOSTIC] Error scanning {DirectoryType} directory: {Path}", kvp.Key, kvp.Value);
                        diagnosticReport.Add($"{kvp.Key} Scan Error: {ex.Message}");
                    }
                }
            }
            
            // Step 5: Test current asset registry state
            try
            {
                var existingAssets = await _assetRepository.GetAllAsync(cancellationToken);
                var assetsList = existingAssets.ToList();
                _logger.LogInformation("[DIAGNOSTIC] Current asset registry contains {AssetCount} assets", assetsList.Count);
                diagnosticReport.Add($"Current Registry Asset Count: {assetsList.Count}");
                
                foreach (var asset in assetsList)
                {
                    var fileExists = File.Exists(asset.FilePath);
                    _logger.LogDebug("[DIAGNOSTIC] Registry asset: {AssetName} at {FilePath} (File Exists: {FileExists})", asset.Name, asset.FilePath, fileExists);
                    diagnosticReport.Add($"  - {asset.Name} ({asset.AssetType}) - File Exists: {fileExists}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[DIAGNOSTIC] Error reading asset registry");
                diagnosticReport.Add($"Registry Read Error: {ex.Message}");
            }
            
            stopwatch.Stop();
            diagnosticReport.Add($"Diagnostic completed in {stopwatch.ElapsedMilliseconds}ms");
            diagnosticReport.Add("=== END DIAGNOSTICS ===");
            
            var report = string.Join(Environment.NewLine, diagnosticReport);
            _logger.LogInformation("[ASSET.KEEPER MANUAL DEBUG] Filesystem diagnostics completed in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            Console.WriteLine(report); // Force console output for debugging
            
            return report;
        }
        catch (Exception ex)
        {
            var errorReport = $"FATAL DIAGNOSTIC ERROR: {ex.Message}";
            _logger.LogError(ex, "[DIAGNOSTIC] Fatal error during filesystem diagnostics");
            diagnosticReport.Add(errorReport);
            return string.Join(Environment.NewLine, diagnosticReport);
        }
    }

    /// <inheritdoc />
    public async Task<int> ForceInitializeAppDataDirectoriesAsync(IDictionary<string, string> directoryPaths, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[ASSET.KEEPER DIAGNOSTIC] Force initializing AppData directories with aggressive scanning and logging");
        
        // DIAGNOSTIC: Run complete filesystem diagnostics first
        var diagnosticsReport = await ForceFilesystemDiagnosticsAsync(cancellationToken);
        _logger.LogInformation("[ASSET.KEEPER DIAGNOSTIC] Filesystem diagnostics report:\n{Report}", diagnosticsReport);
        
        var totalAssetsFound = 0;
        
        try
        {
            // DIAGNOSTIC: Ensure all provided directories exist with detailed logging
            _logger.LogInformation("[ASSET.KEEPER DIAGNOSTIC] Ensuring {DirectoryCount} asset directories exist", directoryPaths.Count);
            foreach (var kvp in directoryPaths)
            {
                try
                {
                    var directoryExists = Directory.Exists(kvp.Value);
                    _logger.LogInformation("[ASSET.KEEPER DIAGNOSTIC] Directory {Type}: {Path} (Exists: {Exists})", kvp.Key, kvp.Value, directoryExists);
                    
                    if (!directoryExists)
                    {
                        _logger.LogInformation("[ASSET.KEEPER DIAGNOSTIC] Creating missing directory: {Path} ({Type})", kvp.Value, kvp.Key);
                        
                        var parentDirectory = Path.GetDirectoryName(kvp.Value);
                        if (!string.IsNullOrEmpty(parentDirectory) && !Directory.Exists(parentDirectory))
                        {
                            _logger.LogInformation("[ASSET.KEEPER DIAGNOSTIC] Parent directory missing, creating: {ParentPath}", parentDirectory);
                        }
                        
                        Directory.CreateDirectory(kvp.Value);
                        var createdSuccessfully = Directory.Exists(kvp.Value);
                        _logger.LogInformation("[ASSET.KEEPER DIAGNOSTIC] Directory creation result: {Path} (Success: {Success})", kvp.Value, createdSuccessfully);
                        
                        if (createdSuccessfully)
                        {
                            _logger.LogInformation("Created directory: {Path} ({Type})", kvp.Value, kvp.Key);
                        }
                    }
                    else
                    {
                        _logger.LogDebug("[ASSET.KEEPER DIAGNOSTIC] Directory already exists: {Path} ({Type})", kvp.Value, kvp.Key);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[ASSET.KEEPER DIAGNOSTIC] Failed to create directory: {Path} ({Type})", kvp.Value, kvp.Key);
                }
            }

            // Aggressively scan each asset directory
            foreach (var kvp in directoryPaths)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var directoryPath = kvp.Value;
                    var directoryType = kvp.Key;

                    if (!Directory.Exists(directoryPath))
                    {
                        _logger.LogWarning("Directory does not exist: {DirectoryPath} ({DirectoryType})", directoryPath, directoryType);
                        continue;
                    }

                    _logger.LogInformation("Aggressively scanning directory: {DirectoryPath} ({DirectoryType})", 
                        directoryPath, directoryType);

                    // Perform deep recursive scan
                    var foundInDirectory = await ScanAndRegisterModelsAsync(directoryPath, true, cancellationToken);
                    totalAssetsFound += foundInDirectory;

                    _logger.LogInformation("Found {Count} assets in {DirectoryType} directory", 
                        foundInDirectory, directoryType);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Error during aggressive scan of directory type: {DirectoryType}", kvp.Key);
                }
            }

            // Create sample asset registry entries for demonstration if no assets were found
            if (totalAssetsFound == 0)
            {
                _logger.LogInformation("No assets discovered in AppData directories. Creating sample registry entries for demonstration.");
                await CreateSampleAssetRegistryEntriesAsync(directoryPaths, cancellationToken);
            }

            _logger.LogInformation("ASSET.KEEPER: Aggressive initialization completed. Total assets discovered: {TotalAssets}", 
                totalAssetsFound);

            return totalAssetsFound;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error during aggressive AppData directory initialization");
            throw;
        }
    }

    /// <summary>
    /// Creates sample asset registry entries for demonstration when no real assets are present
    /// </summary>
    /// <param name="directoryPaths">The directory paths for each asset type</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    private async Task CreateSampleAssetRegistryEntriesAsync(IDictionary<string, string> directoryPaths, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Creating sample asset registry entries for dropdown demonstration");

            // Get directory paths with defaults
            var baseModelsPath = directoryPaths.TryGetValue("BaseModels", out var bmPath) ? bmPath : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Lazarus", "Models", "Base-Models");
            var embeddingsPath = directoryPaths.TryGetValue("Embeddings", out var embPath) ? embPath : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Lazarus", "Models", "Embeddings");
            var loraPath = directoryPaths.TryGetValue("LoRAAdapters", out var lrPath) ? lrPath : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Lazarus", "Models", "LoRA-Adapters");

            var sampleAssets = new[]
            {
                new LlmAsset
                {
                    Name = "Llama-3.2-1B-Instruct-Q4_K_M",
                    FilePath = Path.Combine(baseModelsPath, "llama-3.2-1b-instruct-q4_k_m.gguf"),
                    AssetType = LlmAssetType.BaseModel,
                    Status = LlmAssetStatus.Missing,
                    FileSizeBytes = 800_000_000, // 800MB approximate
                    ParameterCount = "1B",
                    Architecture = "Llama",
                    QuantizationFormat = "Q4_K_M",
                    VramEstimateGb = 1.5m,
                    Description = "Llama 3.2 1B Instruct model with Q4_K_M quantization",
                    FileHash = "sample-hash-llama-1b",
                    IsValidated = false,
                    ValidationResult = "File not present - sample entry",
                    CompatibleRunners = "[\"llama.cpp\", \"vLLM\"]",
                    MetadataJson = "{\"sample\": true, \"purpose\": \"demonstration\"}"
                },
                new LlmAsset
                {
                    Name = "Qwen2.5-3B-Instruct-Q5_K_M",
                    FilePath = Path.Combine(baseModelsPath, "qwen2.5-3b-instruct-q5_k_m.gguf"),
                    AssetType = LlmAssetType.BaseModel,
                    Status = LlmAssetStatus.Missing,
                    FileSizeBytes = 2_200_000_000, // 2.2GB approximate
                    ParameterCount = "3B",
                    Architecture = "Qwen",
                    QuantizationFormat = "Q5_K_M",
                    VramEstimateGb = 3.8m,
                    Description = "Qwen2.5 3B Instruct model with Q5_K_M quantization",
                    FileHash = "sample-hash-qwen-3b",
                    IsValidated = false,
                    ValidationResult = "File not present - sample entry",
                    CompatibleRunners = "[\"llama.cpp\", \"vLLM\"]",
                    MetadataJson = "{\"sample\": true, \"purpose\": \"demonstration\"}"
                },
                new LlmAsset
                {
                    Name = "bge-large-en-v1.5",
                    FilePath = Path.Combine(embeddingsPath, "bge-large-en-v1.5.gguf"),
                    AssetType = LlmAssetType.Embedding,
                    Status = LlmAssetStatus.Missing,
                    FileSizeBytes = 1_300_000_000, // 1.3GB approximate
                    ParameterCount = "335M",
                    Architecture = "BGE",
                    QuantizationFormat = "F16",
                    VramEstimateGb = 1.8m,
                    Description = "BGE Large English v1.5 embedding model",
                    FileHash = "sample-hash-bge-large",
                    IsValidated = false,
                    ValidationResult = "File not present - sample entry",
                    CompatibleRunners = "[\"llama.cpp\"]",
                    MetadataJson = "{\"sample\": true, \"purpose\": \"demonstration\", \"embedding\": true}"
                },
                new LlmAsset
                {
                    Name = "Llama-3.2-LoRA-Math",
                    FilePath = Path.Combine(loraPath, "llama-3.2-lora-math.bin"),
                    AssetType = LlmAssetType.LoRAAdapter,
                    Status = LlmAssetStatus.Missing,
                    FileSizeBytes = 150_000_000, // 150MB approximate
                    ParameterCount = "LoRA",
                    Architecture = "Llama",
                    QuantizationFormat = "FP32",
                    VramEstimateGb = 0.5m,
                    Description = "LoRA adapter for mathematics fine-tuning of Llama 3.2",
                    FileHash = "sample-hash-lora-math",
                    IsValidated = false,
                    ValidationResult = "File not present - sample entry",
                    CompatibleRunners = "[\"llama.cpp\"]",
                    MetadataJson = "{\"sample\": true, \"purpose\": \"demonstration\", \"lora\": true}"
                }
            };

            foreach (var sampleAsset in sampleAssets)
            {
                try
                {
                    await _assetRepository.AddAsync(sampleAsset, cancellationToken);
                    _logger.LogDebug("Created sample asset registry entry: {AssetName}", sampleAsset.Name);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to create sample asset entry: {AssetName}", sampleAsset.Name);
                }
            }

            _logger.LogInformation("Sample asset registry entries created for dropdown demonstration");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating sample asset registry entries");
        }
    }
}