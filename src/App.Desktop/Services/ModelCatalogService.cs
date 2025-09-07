using System.Collections.Concurrent;

namespace Lazarus.Desktop.Services;

/// <summary>
/// Filesystem-backed model catalog. Does not create new top-level folders.
/// </summary>
internal sealed class ModelCatalogService : IModelCatalogService
{
    private readonly string _root;
    private readonly string _baseModels;
    private readonly string _loras;
    private readonly string _tokenizers;
    private readonly string _embeddings;

    private volatile IReadOnlyList<ModelItem> _baseModelsCache = Array.Empty<ModelItem>();
    private volatile IReadOnlyList<ModelItem> _lorasCache = Array.Empty<ModelItem>();
    private volatile IReadOnlyList<ModelItem> _tokenizersCache = Array.Empty<ModelItem>();
    private volatile IReadOnlyList<ModelItem> _embeddingsCache = Array.Empty<ModelItem>();

    public ModelCatalogService()
    {
        _root = Lazarus.Shared.LazarusPaths.Root;
        _baseModels = Lazarus.Shared.LazarusPaths.Models.BaseModels;
        _loras = Lazarus.Shared.LazarusPaths.Models.LoRAAdapters;
        _tokenizers = Lazarus.Shared.LazarusPaths.Models.Tokenizers;
        _embeddings = Lazarus.Shared.LazarusPaths.Models.Embeddings;

        Refresh();
    }

    public void Refresh()
    {
        _baseModelsCache = ScanItems(_baseModels, "BaseModel");
        _lorasCache = ScanItems(_loras, "LoRA");
        _tokenizersCache = ScanItems(_tokenizers, "Tokenizer");
        _embeddingsCache = ScanItems(_embeddings, "Embedding");
    }

    public IReadOnlyList<ModelItem> GetBaseModels() => _baseModelsCache;
    public IReadOnlyList<ModelItem> GetLoRAAdapters() => _lorasCache;
    public IReadOnlyList<ModelItem> GetTokenizers() => _tokenizersCache;
    public IReadOnlyList<ModelItem> GetEmbeddings() => _embeddingsCache;

    private static IReadOnlyList<ModelItem> ScanItems(string directory, string kind)
    {
        try
        {
            if (!Directory.Exists(directory))
                return Array.Empty<ModelItem>();

            // Collect both files and directories; common model extensions prioritized.
            var list = new List<ModelItem>();

            foreach (var path in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories))
            {
                var ext = Path.GetExtension(path).ToLowerInvariant();
                if (IsRelevantModelFile(ext, kind))
                {
                    list.Add(new ModelItem(Path.GetFileName(path), path, kind));
                }
            }

            // Include leaf directories that might represent tokenizers/embeddings
            foreach (var dir in Directory.EnumerateDirectories(directory, "*", SearchOption.AllDirectories))
            {
                // Only include a directory if it contains something inside
                if (Directory.EnumerateFileSystemEntries(dir).Any())
                {
                    list.Add(new ModelItem(Path.GetFileName(dir), dir, kind));
                }
            }

            // Stable sort by name
            return list
                .OrderBy(m => m.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch
        {
            return Array.Empty<ModelItem>();
        }
    }

    private static bool IsRelevantModelFile(string ext, string kind)
    {
        // Keep permissive but sensible defaults
        return ext switch
        {
            ".gguf" => true,
            ".bin" => true,
            ".ggml" => true,
            ".json" => kind is "Tokenizer" or "Embedding", // vocab/tokenizer config cases
            _ => false
        };
    }
}
