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

        LazarusPaths.SystemData.Cache,
        LazarusPaths.SystemData.Config,
        LazarusPaths.SystemData.Database,
        LazarusPaths.SystemData.Logs,
        LazarusPaths.SystemData.ModelPresets,

        LazarusPaths.UserContent.GeneratedOutput,
        LazarusPaths.UserContent.InputFiles,
        LazarusPaths.UserContent.Projects,

        LazarusPaths.FlatLogs,
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
