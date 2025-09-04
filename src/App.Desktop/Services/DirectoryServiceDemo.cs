using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Lazarus.App.Desktop.Services;

/// <summary>
/// Demonstration class for DirectoryService functionality
/// </summary>
public static class DirectoryServiceDemo
{
    /// <summary>
    /// Demonstrates the DirectoryService functionality with comprehensive logging
    /// </summary>
    /// <param name="logger">Logger instance for demonstration output</param>
    /// <returns>Task representing the async demo operation</returns>
    public static async Task RunDemoAsync(ILogger logger)
    {
        logger.LogInformation("=== LAZARUS DIRECTORY SERVICE DEMONSTRATION ===");
        
        try
        {
            // Create directory service instance
            using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var directoryLogger = loggerFactory.CreateLogger<DirectoryService>();
            var directoryService = new DirectoryService(directoryLogger);
            
            logger.LogInformation("User Profile Path: {UserProfilePath}", directoryService.UserProfilePath);
            logger.LogInformation("Total Directory Types: {DirectoryCount}", directoryService.DirectoryPaths.Count);
            
            // Initialize directory structure
            logger.LogInformation("\n--- INITIALIZING DIRECTORY STRUCTURE ---");
            var initResult = await directoryService.InitializeUserProfileAsync();
            
            logger.LogInformation("Initialization Result:");
            logger.LogInformation("  Success: {Success}", initResult.Success);
            logger.LogInformation("  Message: {Message}", initResult.Message);
            logger.LogInformation("  Created Directories: {CreatedCount}", initResult.CreatedDirectories.Count);
            logger.LogInformation("  Existing Directories: {ExistingCount}", initResult.ExistingDirectories.Count);
            logger.LogInformation("  Errors: {ErrorCount}", initResult.Errors.Count);
            logger.LogInformation("  Initialization Time: {InitTime}ms", initResult.InitializationTime.TotalMilliseconds);
            
            if (initResult.CreatedDirectories.Count > 0)
            {
                logger.LogInformation("\nCreated Directories:");
                foreach (var dir in initResult.CreatedDirectories)
                {
                    logger.LogInformation("  + {Directory}", dir);
                }
            }
            
            if (initResult.Errors.Count > 0)
            {
                logger.LogWarning("\nErrors Encountered:");
                foreach (var error in initResult.Errors)
                {
                    logger.LogWarning("  ! {ErrorType}: {ErrorMessage}", error.ErrorType, error.ErrorMessage);
                }
            }
            
            // Validate directory structure
            logger.LogInformation("\n--- VALIDATING DIRECTORY STRUCTURE ---");
            var validationResult = await directoryService.ValidateDirectoryStructureAsync();
            
            logger.LogInformation("Validation Result:");
            logger.LogInformation("  Is Valid: {IsValid}", validationResult.IsValid);
            logger.LogInformation("  Valid Directories: {ValidCount}", validationResult.ValidDirectories.Count);
            logger.LogInformation("  Missing Directories: {MissingCount}", validationResult.MissingDirectories.Count);
            logger.LogInformation("  Inaccessible Directories: {InaccessibleCount}", validationResult.InaccessibleDirectories.Count);
            
            // Show usage statistics
            logger.LogInformation("\n--- DIRECTORY USAGE STATISTICS ---");
            var usageStats = await directoryService.GetDirectoryUsageAsync();
            
            logger.LogInformation("Usage Statistics:");
            logger.LogInformation("  Total Size: {TotalSizeMB:F2} MB", usageStats.TotalSizeBytes / (1024.0 * 1024.0));
            logger.LogInformation("  Total Files: {TotalFiles}", usageStats.TotalFiles);
            logger.LogInformation("  Total Directories: {TotalDirectories}", usageStats.TotalDirectories);
            logger.LogInformation("  Last Updated: {LastUpdated}", usageStats.LastUpdated);
            
            // Show directory breakdown
            if (usageStats.DirectoryBreakdown.Count > 0)
            {
                logger.LogInformation("\nDirectory Breakdown (Top 5 by Size):");
                var topDirectories = usageStats.DirectoryBreakdown
                    .OrderByDescending(kvp => kvp.Value.SizeBytes)
                    .Take(5);
                
                foreach (var (directoryType, stats) in topDirectories)
                {
                    logger.LogInformation("  {DirectoryType}: {SizeMB:F2} MB ({FileCount} files)", 
                        directoryType, stats.SizeBytes / (1024.0 * 1024.0), stats.FileCount);
                }
            }
            
            // Demonstrate specific directory access
            logger.LogInformation("\n--- SPECIFIC DIRECTORY ACCESS ---");
            var modelPath = directoryService.GetDirectoryPath(DirectoryType.BaseModels);
            var configPath = directoryService.GetDirectoryPath(DirectoryType.Configuration);
            var outputPath = directoryService.GetDirectoryPath(DirectoryType.GeneratedImages);
            
            logger.LogInformation("Key Directory Paths:");
            logger.LogInformation("  Base Models: {ModelPath}", modelPath);
            logger.LogInformation("  Configuration: {ConfigPath}", configPath);
            logger.LogInformation("  Generated Images: {OutputPath}", outputPath);
            
            // Test ensure directory functionality
            logger.LogInformation("\n--- TESTING ENSURE DIRECTORY FUNCTIONALITY ---");
            var ensureResult = await directoryService.EnsureDirectoryExistsAsync(DirectoryType.StylePresets);
            logger.LogInformation("Ensure Style Presets Directory: {EnsureResult}", ensureResult);
            
            logger.LogInformation("\n=== DIRECTORY SERVICE DEMONSTRATION COMPLETED ===");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Directory service demonstration failed");
        }
    }
}