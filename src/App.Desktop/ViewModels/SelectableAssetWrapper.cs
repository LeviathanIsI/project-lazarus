using CommunityToolkit.Mvvm.ComponentModel;
using Lazarus.App.Shared.Models;

namespace Lazarus.App.Desktop.ViewModels;

/// <summary>
/// Wrapper class for LlmAsset that provides selection state for multi-select UI controls
/// </summary>
public partial class SelectableAssetWrapper : ObservableObject
{
    private bool _isSelected;

    /// <summary>
    /// Initializes a new instance of the SelectableAssetWrapper class
    /// </summary>
    /// <param name="asset">The asset to wrap</param>
    public SelectableAssetWrapper(LlmAsset asset)
    {
        Asset = asset ?? throw new ArgumentNullException(nameof(asset));
    }

    /// <summary>
    /// Gets the wrapped asset
    /// </summary>
    public LlmAsset Asset { get; }

    /// <summary>
    /// Gets or sets whether this asset is selected
    /// </summary>
    public bool IsSelected
    {
        get => _isSelected;
        set 
        {
            if (SetProperty(ref _isSelected, value))
            {
                // Notify that selection state changed for data binding
                SelectionChanged?.Invoke(this, value);
            }
        }
    }
    
    /// <summary>
    /// Event raised when the selection state changes
    /// </summary>
    public event EventHandler<bool>? SelectionChanged;

    /// <summary>
    /// Gets the asset name (delegated from wrapped asset)
    /// </summary>
    public string Name => Asset.Name;

    /// <summary>
    /// Gets the asset type (delegated from wrapped asset)
    /// </summary>
    public LlmAssetType AssetType => Asset.AssetType;

    /// <summary>
    /// Gets the asset architecture (delegated from wrapped asset)
    /// </summary>
    public string? Architecture => Asset.Architecture;

    /// <summary>
    /// Gets the asset description (delegated from wrapped asset)
    /// </summary>
    public string? Description => Asset.Description;

    /// <summary>
    /// Gets the asset parameter count (delegated from wrapped asset)
    /// </summary>
    public string? ParameterCount => Asset.ParameterCount;

    /// <summary>
    /// Gets the asset status (delegated from wrapped asset)
    /// </summary>
    public LlmAssetStatus Status => Asset.Status;

    /// <summary>
    /// Gets the asset VRAM estimate (delegated from wrapped asset)
    /// </summary>
    public decimal? VramEstimateGb => Asset.VramEstimateGb;

    /// <summary>
    /// Gets the asset file size (delegated from wrapped asset)
    /// </summary>
    public long FileSizeBytes => Asset.FileSizeBytes;

    /// <summary>
    /// Gets the asset file path (delegated from wrapped asset)
    /// </summary>
    public string FilePath => Asset.FilePath;

    /// <summary>
    /// Gets the asset ID (delegated from wrapped asset)
    /// </summary>
    public Guid Id => Asset.Id;
}