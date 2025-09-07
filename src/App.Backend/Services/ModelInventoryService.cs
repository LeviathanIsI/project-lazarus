using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lazarus.Shared;

namespace Lazarus.Backend.Services;

public interface IModelInventoryService
{
    ModelInventory Scan();
}

/// <summary>
/// Reads the user's Lazarus folders (no creation) and returns available artifacts.
/// Uses the shared LazarusPaths contract to avoid guessing paths.
/// </summary>
public sealed class ModelInventoryService : IModelInventoryService
{
    public ModelInventory Scan()
    {
        var baseModels = ScanBaseModels();

        return new ModelInventory
        {
            BaseModels = baseModels,
            Loras      = ScanFiles(LazarusPaths.Models.LoRAAdapters,  new[] { ".safetensors", ".gguf", ".pt", ".bin" })
                          .Select(p => new AdapterInfo(Path.GetFileNameWithoutExtension(p), p)).ToList(),
            Tokenizers = ScanFiles(LazarusPaths.Models.Tokenizers,    new[] { "tokenizer.json", ".model", ".spm", ".txt", ".vocab" })
                          .Select(p => new TokenizerInfo(Path.GetFileName(p), p)).ToList(),
            Embeddings = ScanFiles(LazarusPaths.Models.Embeddings,    new[] { ".gguf", ".bin", ".onnx", ".safetensors" })
                          .Select(p => new EmbeddingInfo(Path.GetFileNameWithoutExtension(p), p)).ToList()
        };
    }

    private static List<BaseModelInfo> ScanBaseModels()
    {
        var list = new List<BaseModelInfo>();
        if (!Directory.Exists(LazarusPaths.Models.BaseModels)) return list;

        foreach (var path in Directory.EnumerateFiles(LazarusPaths.Models.BaseModels, "*.*", SearchOption.AllDirectories))
        {
            var ext = Path.GetExtension(path).ToLowerInvariant();
            if (ext == ".gguf")
            {
                var key = Path.GetFileNameWithoutExtension(path);
                list.Add(new BaseModelInfo(
                    ModelKey: key,
                    DisplayName: key.Replace('_',' ').Replace('-',' '),
                    Format: ModelFormat.GGUF,
                    FilePath: path,
                    PreferredRunner: RunnerKind.LlamaCpp
                ));
            }
            else if (Path.GetFileName(path).Equals("config.json", StringComparison.OrdinalIgnoreCase) ||
                     ext == ".safetensors")
            {
                // Heuristic for HF folders: presence of config.json or safetensors
                var folder = Directory.GetParent(path)!.FullName;
                var key = new DirectoryInfo(folder).Name;
                var mainFile = File.Exists(Path.Combine(folder, "config.json")) ? Path.Combine(folder, "config.json") : path;
                list.Add(new BaseModelInfo(
                    ModelKey: key,
                    DisplayName: key.Replace('_',' ').Replace('-',' '),
                    Format: ModelFormat.HF,
                    FilePath: mainFile,
                    PreferredRunner: RunnerKind.Vllm
                ));
            }
        }
        // de-dupe by key, prefer GGUF if both detected
        return list.GroupBy(m => m.ModelKey).Select(g =>
            g.OrderBy(m => m.Format == ModelFormat.GGUF ? 0 : 1).First()
        ).OrderBy(m => m.DisplayName).ToList();
    }

    private static IEnumerable<string> ScanFiles(string root, string[] patterns)
    {
        if (!Directory.Exists(root)) yield break;
        foreach (var file in Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories))
        {
            var name = Path.GetFileName(file).ToLowerInvariant();
            var ext  = Path.GetExtension(file).ToLowerInvariant();
            if (patterns.Any(p => p.StartsWith(".") ? ext == p : name == p))
                yield return file;
        }
    }

    private static BaseModelInfo? ToBaseModelInfoFromFile(string file)
    {
        var ext = Path.GetExtension(file).ToLowerInvariant();
        var key = Path.GetFileNameWithoutExtension(file);
        var display = key;

        var (format, runner) = ext switch
        {
            ".gguf" => (ModelFormat.GGUF, RunnerKind.LlamaCpp),
            ".safetensors" => (ModelFormat.HF, RunnerKind.Vllm),
            ".bin" => (ModelFormat.HF, RunnerKind.Vllm),
            _ => (ModelFormat.Unknown, RunnerKind.Unknown)
        };

        if (format == ModelFormat.Unknown)
            return null;

        return new BaseModelInfo(key, display, format, file, runner);
    }

    private static BaseModelInfo? ToBaseModelInfoFromDirectory(string dir)
    {
        // Heuristic: a directory with files is considered an HF layout
        if (!Directory.EnumerateFileSystemEntries(dir).Any())
            return null;

        var key = Path.GetFileName(dir);
        var display = key;
        return new BaseModelInfo(key, display, ModelFormat.HF, dir, RunnerKind.Vllm);
    }

    private static IEnumerable<string> EnumerateFilesSafe(string root)
    {
        try
        {
            return Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories);
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    private static IEnumerable<string> EnumerateDirectoriesSafe(string root)
    {
        try
        {
            return Directory.EnumerateDirectories(root, "*", SearchOption.AllDirectories);
        }
        catch
        {
            return Array.Empty<string>();
        }
    }
}
