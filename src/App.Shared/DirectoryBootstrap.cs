using System.IO;

namespace Lazarus.Shared;

public static class DirectoryBootstrap
{
    // Exact tree from screenshots (leaf directories only)
    public static System.Collections.Generic.IReadOnlyList<string> LeafDirectories => new[]
    {
        LazarusPaths.GenAssets.ControlNet,
        LazarusPaths.GenAssets.StylePresets,
        LazarusPaths.GenAssets.Upscale,
        LazarusPaths.GenAssets.Vae,

        LazarusPaths.Models.BaseModels,
        LazarusPaths.Models.Embeddings,
        LazarusPaths.Models.LoRAAdapters,
        LazarusPaths.Models.Tokenizers,

        LazarusPaths.SharedResources.ExternalLinks,
        LazarusPaths.SharedResources.ImportExport,
        System.IO.Path.Combine(LazarusPaths.SharedResources.ImportExport, "Export"),
        System.IO.Path.Combine(LazarusPaths.SharedResources.ImportExport, "Import"),

        LazarusPaths.SystemData.Cache,
        System.IO.Path.Combine(LazarusPaths.SystemData.Cache, "Downloads"),
        LazarusPaths.SystemData.Config,
        LazarusPaths.SystemData.Database,
        LazarusPaths.SystemData.Logs,
        LazarusPaths.SystemData.ModelPresets,

        LazarusPaths.UserContent.GeneratedOutput,
        LazarusPaths.UserContent.InputFiles,
        LazarusPaths.UserContent.Projects,

        LazarusPaths.FlatLogs,

        // Additional top-level convenience folders wired in the Paths UI
        System.IO.Path.Combine(LazarusPaths.Models.RootDir, "Quantized"),
        System.IO.Path.Combine(LazarusPaths.Root, "Conversations"),
        System.IO.Path.Combine(LazarusPaths.Root, "Backups"),
        System.IO.Path.Combine(LazarusPaths.SharedResources.RootDir, "Templates"),
        System.IO.Path.Combine(LazarusPaths.Root, "Plugins"),
    };

    public static void EnsureAll()
    {
        var dirs = LeafDirectories;
        foreach (var d in dirs)
        {
            Directory.CreateDirectory(d);
        }
    }
}
