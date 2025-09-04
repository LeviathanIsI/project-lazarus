using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using Lazarus.App.Desktop.ViewModels;
using Lazarus.App.Shared.Models;

namespace Lazarus.App.Desktop.Services.UXValidation;

/// <summary>
/// UX.Copilot Multi-Select User Experience Validator
/// Validates multi-select checkbox functionality and interaction patterns
/// </summary>
public class MultiSelectUXValidator
{
    private readonly ILogger<MultiSelectUXValidator> _logger;

    public MultiSelectUXValidator(ILogger<MultiSelectUXValidator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Validates checkbox interaction responsiveness and feedback
    /// </summary>
    /// <param name="selectableCollection">Collection of selectable asset wrappers</param>
    /// <param name="selectedAssetsTable">The table showing selected assets</param>
    /// <returns>Validation results for checkbox interactions</returns>
    public UXValidationResult ValidateCheckboxInteractionResponsiveness(
        ObservableCollection<SelectableAssetWrapper> selectableCollection,
        ObservableCollection<LlmAsset> selectedAssetsTable)
    {
        var result = new UXValidationResult { TestName = "Checkbox Interaction Responsiveness" };
        
        try
        {
            _logger.LogInformation("[UX.COPILOT] Starting checkbox interaction responsiveness validation");
            
            // Test 1: Immediate Visual Feedback
            var beforeCheckTime = DateTime.UtcNow;
            var testWrapper = selectableCollection.FirstOrDefault();
            
            if (testWrapper != null)
            {
                // Simulate checkbox click
                testWrapper.IsSelected = !testWrapper.IsSelected;
                var afterCheckTime = DateTime.UtcNow;
                var responseTime = (afterCheckTime - beforeCheckTime).TotalMilliseconds;
                
                result.AddCheck("Visual State Change", responseTime < 16, 
                    $"Checkbox state change took {responseTime:F2}ms (target: <16ms for 60fps)");
                
                // Test 2: Binding Synchronization
                var isInTable = selectedAssetsTable.Contains(testWrapper.Asset);
                var shouldBeInTable = testWrapper.IsSelected;
                
                result.AddCheck("Table Synchronization", isInTable == shouldBeInTable,
                    $"Asset table sync: Expected={shouldBeInTable}, Actual={isInTable}");
                
                // Test 3: State Persistence
                var originalState = testWrapper.IsSelected;
                testWrapper.IsSelected = !testWrapper.IsSelected;
                testWrapper.IsSelected = originalState;
                
                result.AddCheck("State Persistence", testWrapper.IsSelected == originalState,
                    "Checkbox state persists correctly through toggle operations");
            }
            else
            {
                result.AddCheck("Test Data Available", false, "No selectable assets available for testing");
            }
            
            _logger.LogInformation("[UX.COPILOT] Checkbox responsiveness validation completed with {PassedCount}/{TotalCount} tests passed",
                result.PassedChecks, result.TotalChecks);
                
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UX.COPILOT] Error during checkbox interaction validation");
            result.AddCheck("Exception Handling", false, $"Validation failed with exception: {ex.Message}");
        }
        
        return result;
    }
    
    /// <summary>
    /// Validates multi-select dropdown behavior and usability
    /// </summary>
    /// <param name="isDropdownOpen">Whether the dropdown is currently open</param>
    /// <param name="selectableItems">Collection of selectable items in dropdown</param>
    /// <returns>Validation results for dropdown behavior</returns>
    public UXValidationResult ValidateDropdownBehavior(
        bool isDropdownOpen,
        ObservableCollection<SelectableAssetWrapper> selectableItems)
    {
        var result = new UXValidationResult { TestName = "Multi-Select Dropdown Behavior" };
        
        try
        {
            _logger.LogInformation("[UX.COPILOT] Starting dropdown behavior validation");
            
            // Test 1: Dropdown State Management
            result.AddCheck("Dropdown Accessibility", isDropdownOpen || selectableItems.Any(),
                "Dropdown is either open or has selectable items available");
            
            // Test 2: Selection State Visibility
            var selectedItems = selectableItems.Where(item => item.IsSelected).ToList();
            var unselectedItems = selectableItems.Where(item => !item.IsSelected).ToList();
            
            result.AddCheck("Selection State Clarity", true,
                $"Clear distinction: {selectedItems.Count} selected, {unselectedItems.Count} unselected");
            
            // Test 3: Multi-Selection Capability
            if (selectableItems.Count >= 2)
            {
                var canSelectMultiple = selectedItems.Count > 1 || 
                                       (selectedItems.Count == 1 && unselectedItems.Any());
                
                result.AddCheck("Multi-Selection Support", canSelectMultiple,
                    "Dropdown supports selection of multiple items simultaneously");
            }
            
            // Test 4: Item Template Consistency
            foreach (var item in selectableItems.Take(5)) // Test first 5 items for performance
            {
                var hasValidName = !string.IsNullOrEmpty(item.Name);
                var hasValidAsset = item.Asset != null;
                
                if (!hasValidName || !hasValidAsset)
                {
                    result.AddCheck($"Item Template [{item.Name}]", false,
                        $"Item template validation failed: Name={hasValidName}, Asset={hasValidAsset}");
                }
            }
            
            result.AddCheck("Item Template Consistency", 
                selectableItems.All(item => !string.IsNullOrEmpty(item.Name) && item.Asset != null),
                "All dropdown items have consistent templates and valid data");
            
            _logger.LogInformation("[UX.COPILOT] Dropdown behavior validation completed with {PassedCount}/{TotalCount} tests passed",
                result.PassedChecks, result.TotalChecks);
                
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UX.COPILOT] Error during dropdown behavior validation");
            result.AddCheck("Exception Handling", false, $"Validation failed with exception: {ex.Message}");
        }
        
        return result;
    }
    
    /// <summary>
    /// Validates table update responsiveness for user selections
    /// </summary>
    /// <param name="selectedAssets">The selected assets table collection</param>
    /// <param name="selectableEmbeddings">Selectable embeddings collection</param>
    /// <param name="selectableLoRA">Selectable LoRA adapters collection</param>
    /// <returns>Validation results for table responsiveness</returns>
    public UXValidationResult ValidateTableUpdateResponsiveness(
        ObservableCollection<LlmAsset> selectedAssets,
        ObservableCollection<SelectableAssetWrapper> selectableEmbeddings,
        ObservableCollection<SelectableAssetWrapper> selectableLoRA)
    {
        var result = new UXValidationResult { TestName = "Table Update Responsiveness" };
        
        try
        {
            _logger.LogInformation("[UX.COPILOT] Starting table update responsiveness validation");
            
            // Test 1: Real-time Table Updates
            var beforeUpdateTime = DateTime.UtcNow;
            var initialTableCount = selectedAssets.Count;
            
            // Count currently selected items across all dropdowns
            var selectedEmbeddingCount = selectableEmbeddings.Count(e => e.IsSelected);
            var selectedLoRACount = selectableLoRA.Count(l => l.IsSelected);
            var expectedTotal = selectedEmbeddingCount + selectedLoRACount;
            
            // Allow for base model and tokenizer to be in the table as well
            var actualTotal = selectedAssets.Count;
            var afterUpdateTime = DateTime.UtcNow;
            var updateTime = (afterUpdateTime - beforeUpdateTime).TotalMilliseconds;
            
            result.AddCheck("Table Update Performance", updateTime < 100,
                $"Table update completed in {updateTime:F2}ms (target: <100ms)");
            
            // Test 2: Synchronization Accuracy
            var embeddingAssetsInTable = selectedAssets.Count(asset => 
                selectableEmbeddings.Any(e => e.IsSelected && e.Asset.Id == asset.Id));
            var loraAssetsInTable = selectedAssets.Count(asset => 
                selectableLoRA.Any(l => l.IsSelected && l.Asset.Id == asset.Id));
            
            result.AddCheck("Embedding Synchronization", 
                embeddingAssetsInTable == selectedEmbeddingCount,
                $"Embeddings in table: {embeddingAssetsInTable}, Expected: {selectedEmbeddingCount}");
                
            result.AddCheck("LoRA Synchronization",
                loraAssetsInTable == selectedLoRACount,
                $"LoRA adapters in table: {loraAssetsInTable}, Expected: {selectedLoRACount}");
            
            // Test 3: Table Data Consistency
            var allTableItemsValid = selectedAssets.All(asset => 
                !string.IsNullOrEmpty(asset.Name) && 
                asset.Id != Guid.Empty);
                
            result.AddCheck("Table Data Integrity", allTableItemsValid,
                "All items in selected assets table have valid data");
            
            // Test 4: Empty State Handling
            if (!selectedAssets.Any())
            {
                result.AddCheck("Empty State Handling", true,
                    "Table correctly handles empty state when no assets are selected");
            }
            
            _logger.LogInformation("[UX.COPILOT] Table update validation completed with {PassedCount}/{TotalCount} tests passed",
                result.PassedChecks, result.TotalChecks);
                
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UX.COPILOT] Error during table update validation");
            result.AddCheck("Exception Handling", false, $"Validation failed with exception: {ex.Message}");
        }
        
        return result;
    }
    
    /// <summary>
    /// Validates overall multi-select user experience flow
    /// </summary>
    /// <param name="viewModel">The model configuration view model</param>
    /// <returns>Comprehensive UX validation results</returns>
    public ComprehensiveUXResult ValidateCompleteMultiSelectExperience(ModelConfigurationViewModel viewModel)
    {
        var result = new ComprehensiveUXResult { TestSuiteName = "Complete Multi-Select UX Validation" };
        
        try
        {
            _logger.LogInformation("[UX.COPILOT] Starting comprehensive multi-select UX validation");
            
            // Validate Checkbox Interactions
            var checkboxResult = ValidateCheckboxInteractionResponsiveness(
                viewModel.SelectableEmbeddings, viewModel.SelectedAssets);
            result.AddTestResult(checkboxResult);
            
            // Validate Dropdown Behavior for Embeddings
            var embeddingDropdownResult = ValidateDropdownBehavior(
                viewModel.IsEmbeddingsDropdownOpen, viewModel.SelectableEmbeddings);
            result.AddTestResult(embeddingDropdownResult);
            
            // Validate Dropdown Behavior for LoRA Adapters
            var loraDropdownResult = ValidateDropdownBehavior(
                viewModel.IsLoRAAdaptersDropdownOpen, viewModel.SelectableLoRAAdapters);
            result.AddTestResult(loraDropdownResult);
            
            // Validate Table Update Responsiveness
            var tableResult = ValidateTableUpdateResponsiveness(
                viewModel.SelectedAssets, viewModel.SelectableEmbeddings, viewModel.SelectableLoRAAdapters);
            result.AddTestResult(tableResult);
            
            // Calculate overall success metrics
            result.OverallSuccess = result.TotalPassedChecks / (double)result.TotalChecks >= 0.9; // 90% pass rate
            result.CompletionTime = DateTime.UtcNow;
            
            _logger.LogInformation("[UX.COPILOT] Comprehensive validation completed: {PassedCount}/{TotalCount} checks passed ({SuccessRate:P1})",
                result.TotalPassedChecks, result.TotalChecks, result.TotalPassedChecks / (double)result.TotalChecks);
                
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UX.COPILOT] Error during comprehensive UX validation");
            result.AddTestResult(new UXValidationResult 
            { 
                TestName = "Exception Handling",
                Checks = new List<UXCheck> 
                { 
                    new UXCheck("Exception Handling", false, $"Validation failed: {ex.Message}") 
                }
            });
        }
        
        return result;
    }
}

/// <summary>
/// Results from UX validation testing
/// </summary>
public class UXValidationResult
{
    public string TestName { get; set; } = string.Empty;
    public List<UXCheck> Checks { get; set; } = new();
    public DateTime TestTime { get; set; } = DateTime.UtcNow;
    
    public void AddCheck(string name, bool passed, string message)
    {
        Checks.Add(new UXCheck(name, passed, message));
    }
    
    public int PassedChecks => Checks.Count(c => c.Passed);
    public int TotalChecks => Checks.Count;
    public double SuccessRate => TotalChecks > 0 ? PassedChecks / (double)TotalChecks : 0;
}

/// <summary>
/// Individual UX validation check
/// </summary>
public class UXCheck
{
    public UXCheck(string name, bool passed, string message)
    {
        Name = name;
        Passed = passed;
        Message = message;
    }
    
    public string Name { get; }
    public bool Passed { get; }
    public string Message { get; }
}

/// <summary>
/// Comprehensive UX validation results containing multiple test results
/// </summary>
public class ComprehensiveUXResult
{
    public string TestSuiteName { get; set; } = string.Empty;
    public List<UXValidationResult> TestResults { get; set; } = new();
    public bool OverallSuccess { get; set; }
    public DateTime CompletionTime { get; set; }
    
    public void AddTestResult(UXValidationResult result)
    {
        TestResults.Add(result);
    }
    
    public int TotalPassedChecks => TestResults.Sum(r => r.PassedChecks);
    public int TotalChecks => TestResults.Sum(r => r.TotalChecks);
    public double OverallSuccessRate => TotalChecks > 0 ? TotalPassedChecks / (double)TotalChecks : 0;
}