using System.Collections.ObjectModel;

namespace Lazarus.Desktop.Services;

public sealed record ModelItem(string Name, string FullPath, string Kind);

/// <summary>
/// Provides filesystem-backed catalog of available models and related assets.
/// </summary>
public interface IModelCatalogService
{
    /// <summary>
    /// Refreshes the catalog from disk.
    /// </summary>
    void Refresh();

    /// <summary>
    /// Gets base models found under %LOCALAPPDATA%\Lazarus\Models\Base-Models
    /// </summary>
    IReadOnlyList<ModelItem> GetBaseModels();

    /// <summary>
    /// Gets LoRA adapters found under %LOCALAPPDATA%\Lazarus\Models\LoRA-Adapters
    /// </summary>
    IReadOnlyList<ModelItem> GetLoRAAdapters();

    /// <summary>
    /// Gets tokenizers found under %LOCALAPPDATA%\Lazarus\Models\Tokenizers
    /// </summary>
    IReadOnlyList<ModelItem> GetTokenizers();

    /// <summary>
    /// Gets embeddings found under %LOCALAPPDATA%\Lazarus\Models\Embeddings
    /// </summary>
    IReadOnlyList<ModelItem> GetEmbeddings();
}

