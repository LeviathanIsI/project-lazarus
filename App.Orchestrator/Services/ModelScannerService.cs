namespace Lazarus.Orchestrator.Services;

using Lazarus.Shared.Utilities;


public static class ModelScannerService
{
    private static readonly string[] ModelDirectories =
{
    Path.Combine(DirectoryBootstrap.GetRootPath(), "models"), // <-- Lazarus managed folder
    @"C:\Models",                                             // legacy/manual
    @"D:\AI\Models",
    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Models")
};

    public static ModelInventory ScanAll()
    {
        var inventory = new ModelInventory();

        foreach (var directory in ModelDirectories.Where(Directory.Exists))
        {
            ScanDirectory(directory, inventory);
        }

        return inventory;
    }

    private static void ScanDirectory(string directory, ModelInventory inventory)
    {
        try
        {
            var files = Directory.GetFiles(directory, "*", SearchOption.AllDirectories);

            foreach (var file in files)
            {
                var extension = Path.GetExtension(file).ToLowerInvariant();
                var fileInfo = new FileInfo(file);

                switch (extension)
                {
                    case ".gguf":
                        inventory.BaseModels.Add(CreateBaseModel(fileInfo));
                        break;
                    case ".safetensors":
                        if (IsLoRA(fileInfo))
                            inventory.LoRAs.Add(CreateLoRA(fileInfo));
                        else if (IsVAE(fileInfo))
                            inventory.VAEs.Add(CreateVAE(fileInfo));
                        else if (IsEmbedding(fileInfo))
                            inventory.Embeddings.Add(CreateEmbedding(fileInfo));
                        break;
                    case ".pt":
                    case ".pth":
                        if (IsHypernetwork(fileInfo))
                            inventory.Hypernetworks.Add(CreateHypernetwork(fileInfo));
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error scanning directory {directory}: {ex.Message}");
        }
    }

    private static BaseModelInfo CreateBaseModel(FileInfo file)
    {
        var name = Path.GetFileNameWithoutExtension(file.Name);
        var sizeGB = file.Length / (1024.0 * 1024.0 * 1024.0);

        return new BaseModelInfo
        {
            Name = name,
            FileName = file.Name,
            FilePath = file.FullName,
            SizeBytes = file.Length,
            SizeFormatted = $"{sizeGB:F1} GB",
            Format = "GGUF",
            Architecture = DetectArchitecture(name),
            ContextLength = DetectContextLength(name),
            Quantization = DetectQuantization(name),
            LastModified = file.LastWriteTime
        };
    }

    private static LoRAInfo CreateLoRA(FileInfo file)
    {
        var name = Path.GetFileNameWithoutExtension(file.Name);
        var sizeMB = file.Length / (1024.0 * 1024.0);

        return new LoRAInfo
        {
            Name = name,
            FileName = file.Name,
            FilePath = file.FullName,
            SizeBytes = file.Length,
            SizeFormatted = $"{sizeMB:F0} MB",
            Type = DetectLoRAType(name),
            Rank = 32, // Default, could be detected from metadata
            Alpha = 32,
            LastModified = file.LastWriteTime
        };
    }

    private static VAEInfo CreateVAE(FileInfo file)
    {
        var name = Path.GetFileNameWithoutExtension(file.Name);
        var sizeMB = file.Length / (1024.0 * 1024.0);

        return new VAEInfo
        {
            Name = name,
            FileName = file.Name,
            FilePath = file.FullName,
            SizeBytes = file.Length,
            SizeFormatted = $"{sizeMB:F0} MB",
            Type = DetectVAEType(name),
            Compatibility = DetectVAECompatibility(name),
            LastModified = file.LastWriteTime
        };
    }

    private static EmbeddingInfo CreateEmbedding(FileInfo file)
    {
        var name = Path.GetFileNameWithoutExtension(file.Name);
        var sizeKB = file.Length / 1024.0;

        return new EmbeddingInfo
        {
            Name = name,
            FileName = file.Name,
            FilePath = file.FullName,
            SizeBytes = file.Length,
            SizeFormatted = $"{sizeKB:F0} KB",
            Type = DetectEmbeddingType(name),
            Keyword = name.ToLowerInvariant(),
            Vectors = 16, // Default
            LastModified = file.LastWriteTime
        };
    }

    private static HypernetworkInfo CreateHypernetwork(FileInfo file)
    {
        var name = Path.GetFileNameWithoutExtension(file.Name);
        var sizeMB = file.Length / (1024.0 * 1024.0);

        return new HypernetworkInfo
        {
            Name = name,
            FileName = file.Name,
            FilePath = file.FullName,
            SizeBytes = file.Length,
            SizeFormatted = $"{sizeMB:F0} MB",
            Architecture = "Linear + Dropout",
            TrainingSteps = 50000, // Mock data
            LastModified = file.LastWriteTime
        };
    }

    // Detection helpers
    private static bool IsLoRA(FileInfo file) =>
        file.Directory?.Name.Contains("lora", StringComparison.OrdinalIgnoreCase) == true ||
        file.Name.Contains("lora", StringComparison.OrdinalIgnoreCase);

    private static bool IsVAE(FileInfo file) =>
        file.Directory?.Name.Contains("vae", StringComparison.OrdinalIgnoreCase) == true ||
        file.Name.Contains("vae", StringComparison.OrdinalIgnoreCase);

    private static bool IsEmbedding(FileInfo file) =>
        file.Directory?.Name.Contains("embedding", StringComparison.OrdinalIgnoreCase) == true ||
        file.Name.Contains("embedding", StringComparison.OrdinalIgnoreCase) ||
        file.Length < 1024 * 1024; // Small files are likely embeddings

    private static bool IsHypernetwork(FileInfo file) =>
        file.Directory?.Name.Contains("hypernetwork", StringComparison.OrdinalIgnoreCase) == true ||
        file.Name.Contains("hypernetwork", StringComparison.OrdinalIgnoreCase);

    private static string DetectArchitecture(string name) =>
        name.ToLowerInvariant() switch
        {
            var n when n.Contains("qwen") => "Qwen2.5",
            var n when n.Contains("llama") => "Llama",
            var n when n.Contains("mistral") => "Mistral",
            _ => "Unknown"
        };

    private static int DetectContextLength(string name) =>
        name.ToLowerInvariant() switch
        {
            var n when n.Contains("32k") => 32768,
            var n when n.Contains("8k") => 8192,
            var n when n.Contains("4k") => 4096,
            _ => 32768
        };

    private static string DetectQuantization(string name)
    {
        var lower = name.ToLowerInvariant();
        if (lower.Contains("q5_k_m")) return "Q5_K_M";
        if (lower.Contains("q4_k_m")) return "Q4_K_M";
        if (lower.Contains("q8_0")) return "Q8_0";
        if (lower.Contains("fp16")) return "FP16";
        return "Unknown";
    }

    private static string DetectLoRAType(string name) =>
        name.ToLowerInvariant() switch
        {
            var n when n.Contains("style") => "Style",
            var n when n.Contains("character") => "Character",
            var n when n.Contains("concept") => "Concept",
            _ => "Unknown"
        };

    private static string DetectVAEType(string name) =>
        name.ToLowerInvariant() switch
        {
            var n when n.Contains("mse") => "MSE-trained",
            var n when n.Contains("ema") => "EMA-pruned",
            var n when n.Contains("anime") => "Anime-optimized",
            _ => "Standard"
        };

    private static string DetectVAECompatibility(string name) =>
        name.ToLowerInvariant() switch
        {
            var n when n.Contains("xl") || n.Contains("sdxl") => "SDXL",
            var n when n.Contains("2.1") => "SD 2.1",
            var n when n.Contains("2.0") => "SD 2.0",
            _ => "SD 1.5"
        };

    private static string DetectEmbeddingType(string name) =>
        name.ToLowerInvariant() switch
        {
            var n when n.Contains("bad") || n.Contains("negative") => "Negative",
            var n when n.Contains("style") => "Style",
            var n when n.Contains("concept") => "Concept",
            _ => "General"
        };
}

public class ModelInventory
{
    public List<BaseModelInfo> BaseModels { get; } = new();
    public List<LoRAInfo> LoRAs { get; } = new();
    public List<VAEInfo> VAEs { get; } = new();
    public List<EmbeddingInfo> Embeddings { get; } = new();
    public List<HypernetworkInfo> Hypernetworks { get; } = new();
}

public record BaseModelInfo
{
    public string Name { get; init; } = "";
    public string FileName { get; init; } = "";
    public string FilePath { get; init; } = "";
    public long SizeBytes { get; init; }
    public string SizeFormatted { get; init; } = "";
    public string Format { get; init; } = "";
    public string Architecture { get; init; } = "";
    public int ContextLength { get; init; }
    public string Quantization { get; init; } = "";
    public DateTime LastModified { get; init; }
}

public record LoRAInfo
{
    public string Name { get; init; } = "";
    public string FileName { get; init; } = "";
    public string FilePath { get; init; } = "";
    public long SizeBytes { get; init; }
    public string SizeFormatted { get; init; } = "";
    public string Type { get; init; } = "";
    public int Rank { get; init; }
    public int Alpha { get; init; }
    public DateTime LastModified { get; init; }
}

public record VAEInfo
{
    public string Name { get; init; } = "";
    public string FileName { get; init; } = "";
    public string FilePath { get; init; } = "";
    public long SizeBytes { get; init; }
    public string SizeFormatted { get; init; } = "";
    public string Type { get; init; } = "";
    public string Compatibility { get; init; } = "";
    public DateTime LastModified { get; init; }
}

public record EmbeddingInfo
{
    public string Name { get; init; } = "";
    public string FileName { get; init; } = "";
    public string FilePath { get; init; } = "";
    public long SizeBytes { get; init; }
    public string SizeFormatted { get; init; } = "";
    public string Type { get; init; } = "";
    public string Keyword { get; init; } = "";
    public int Vectors { get; init; }
    public DateTime LastModified { get; init; }
}

public record HypernetworkInfo
{
    public string Name { get; init; } = "";
    public string FileName { get; init; } = "";
    public string FilePath { get; init; } = "";
    public long SizeBytes { get; init; }
    public string SizeFormatted { get; init; } = "";
    public string Architecture { get; init; } = "";
    public int TrainingSteps { get; init; }
    public DateTime LastModified { get; init; }
}