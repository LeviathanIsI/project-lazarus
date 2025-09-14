using System;
using System.Collections.Generic;

namespace Lazarus.Shared;

public sealed class ModelInventory
{
    public IReadOnlyList<BaseModelInfo> BaseModels { get; init; } = Array.Empty<BaseModelInfo>();
    public IReadOnlyList<AdapterInfo> Loras { get; init; } = Array.Empty<AdapterInfo>();
    public IReadOnlyList<TokenizerInfo> Tokenizers { get; init; } = Array.Empty<TokenizerInfo>();
    public IReadOnlyList<EmbeddingInfo> Embeddings { get; init; } = Array.Empty<EmbeddingInfo>();
}

