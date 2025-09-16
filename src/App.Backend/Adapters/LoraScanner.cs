using System.Text.Json;
using System.Text.RegularExpressions;

namespace Lazarus.Backend.Adapters;

public enum LoraFormat
{
    Unknown,
    GGUF,        // llama.cpp format (.gguf files)
    Safetensors, // HuggingFace format (.safetensors files)
    PyTorch,     // PyTorch format (.pt, .pth, .bin files)
    Checkpoint   // Checkpoint format (.ckpt files)
}

public sealed record LoraOption(
    string Display,     // "Qwen3-Coder-30B-A3B-Instruct — step 716"
    string BaseModel,   // "Qwen3-Coder-30B-A3B-Instruct"
    string Path,        // full directory with adapter files
    int? Step,          // 716 or null
    bool IsFinal,       // true if "adapter" folder
    LoraFormat Format   // Detected format of the adapter
);

public static class LoraScanner
{
    private static readonly Regex StepRx = new(@"^checkpoint-(\d+)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static string GetRoot()
        => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "Lazarus","Models","LoRA-Adapters");

    public static IReadOnlyList<LoraOption> ScanAll()
    {
        var root = GetRoot();
        if (!Directory.Exists(root)) return Array.Empty<LoraOption>();

        var hits = new List<LoraOption>();
        foreach (var dir in Directory.EnumerateDirectories(root, "*", SearchOption.AllDirectories))
        {
            var format = DetectLoraFormat(dir);
            if (format == LoraFormat.Unknown) continue;

            var baseModel = GetBaseModelName(root, dir);
            var folder = Path.GetFileName(dir);
            var (step, isFinal, label) = LabelFor(folder);

            hits.Add(new LoraOption(
                Display: $"{baseModel} — {label}",
                BaseModel: baseModel,
                Path: dir,
                Step: step,
                IsFinal: isFinal,
                Format: format
            ));
        }

        return hits
            .DistinctBy(h => h.Path)
            .OrderByDescending(h => h.IsFinal)
            .ThenByDescending(h => h.Step ?? -1)
            .ThenBy(h => h.BaseModel, StringComparer.OrdinalIgnoreCase)
            .ThenBy(h => h.Display, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static (int? step, bool isFinal, string label) LabelFor(string folder)
    {
        if (string.Equals(folder, "adapter", StringComparison.OrdinalIgnoreCase))
            return (null, true, "final");

        var m = StepRx.Match(folder);
        if (m.Success && int.TryParse(m.Groups[1].Value, out var n))
            return (n, false, $"step {n}");

        return (null, false, folder);
    }

    private static string GetBaseModelName(string root, string dir)
    {
        // root = ...\LoRA-Adapters
        // dir  = ...\LoRA-Adapters\<Base>\(...)*   => take the first segment after root
        var rel = Path.GetRelativePath(root, dir);
        var first = rel.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                       .FirstOrDefault();
        return string.IsNullOrWhiteSpace(first) ? "Unknown" : first;
    }

    private static LoraFormat DetectLoraFormat(string dir)
    {
        try
        {
            // Check for ALL format-specific files in the directory
            // Prioritize GGUF format (llama.cpp compatible) since it's the most compatible
            bool hasGguf = Directory.EnumerateFiles(dir, "*.gguf", SearchOption.TopDirectoryOnly).Any();
            bool hasSafetensors = Directory.EnumerateFiles(dir, "*.safetensors", SearchOption.TopDirectoryOnly).Any() ||
                                   Directory.EnumerateFiles(dir, "adapter_model.safetensors", SearchOption.TopDirectoryOnly).Any();
            bool hasPyTorch = Directory.EnumerateFiles(dir, "*.pt", SearchOption.TopDirectoryOnly).Any() ||
                              Directory.EnumerateFiles(dir, "*.pth", SearchOption.TopDirectoryOnly).Any() ||
                              Directory.EnumerateFiles(dir, "*.bin", SearchOption.TopDirectoryOnly).Any() ||
                              Directory.EnumerateFiles(dir, "adapter_model.bin", SearchOption.TopDirectoryOnly).Any();
            bool hasCheckpoint = Directory.EnumerateFiles(dir, "*.ckpt", SearchOption.TopDirectoryOnly).Any();
            bool hasConfig = Directory.EnumerateFiles(dir, "adapter_config.json", SearchOption.TopDirectoryOnly).Any() ||
                             Directory.EnumerateFiles(dir, "lora_config.json", SearchOption.TopDirectoryOnly).Any();

            // Prefer GGUF if it exists (since it's what llama.cpp needs)
            if (hasGguf)
                return LoraFormat.GGUF;

            // Then check other formats
            if (hasSafetensors)
                return LoraFormat.Safetensors;

            if (hasPyTorch)
                return LoraFormat.PyTorch;

            if (hasCheckpoint)
                return LoraFormat.Checkpoint;

            // If only config exists, it's likely a PEFT adapter waiting for conversion
            if (hasConfig)
                return LoraFormat.Safetensors;

            return LoraFormat.Unknown;
        }
        catch
        {
            return LoraFormat.Unknown;
        }
    }
}

