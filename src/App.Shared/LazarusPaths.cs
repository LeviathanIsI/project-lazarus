using System;
using System.IO;

namespace Lazarus.Shared;

/// <summary>
/// Canonical path contract for Lazarus directories rooted at
/// %LOCALAPPDATA%\Lazarus (Environment.SpecialFolder.LocalApplicationData).
/// Do not rename or reorder folders — keep in sync with the on-disk layout.
/// </summary>
public static class LazarusPaths
{
    // Optional override for testing/portable cases; otherwise %LOCALAPPDATA%\Lazarus
    private static readonly string? OverrideRoot = Environment.GetEnvironmentVariable("LAZARUS_HOME");

    /// <summary>
    /// Application root: %LOCALAPPDATA%\Lazarus (or LAZARUS_HOME if set)
    /// </summary>
    public static string Root =>
        Path.GetFullPath(!string.IsNullOrWhiteSpace(OverrideRoot)
            ? OverrideRoot!
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Lazarus"));

    /// <summary>
    /// Flat text logs directory at %LOCALAPPDATA%\Lazarus\logs
    /// </summary>
    public static readonly string FlatLogs = Path.Combine(Root, "logs");

    /// <summary>
    /// Primary application database file path (SQLite): %LOCALAPPDATA%\Lazarus\lazarus.db
    /// </summary>
    public static readonly string DatabaseFile = Path.Combine(Root, "lazarus.db");

    /// <summary>
    /// SQLite file set at root (as seen in screenshots)
    /// </summary>
    public static readonly string SqliteDb    = Path.Combine(Root, "lazarus.db");
    public static readonly string SqliteDbShm = Path.Combine(Root, "lazarus.db-shm");
    public static readonly string SqliteDbWal = Path.Combine(Root, "lazarus.db-wal");

    /// <summary>
    /// Model storage under %LOCALAPPDATA%\Lazarus\Models
    /// </summary>
    public static class Models
    {
        public static readonly string RootDir      = Path.Combine(Root, "Models");
        public static readonly string BaseModels   = Path.Combine(RootDir, "Base-Models");
        public static readonly string Embeddings   = Path.Combine(RootDir, "Embeddings");
        public static readonly string LoRAAdapters = Path.Combine(RootDir, "LoRA-Adapters");
        public static readonly string Tokenizers   = Path.Combine(RootDir, "Tokenizers");
    }

    /// <summary>
    /// Generation assets under %LOCALAPPDATA%\Lazarus\Generation-Assets
    /// </summary>
    public static class GenAssets
    {
        public static readonly string RootDir      = Path.Combine(Root, "Generation-Assets");
        public static readonly string ControlNet   = Path.Combine(RootDir, "ControlNet");
        public static readonly string StylePresets = Path.Combine(RootDir, "Style-Presets");
        public static readonly string Upscale      = Path.Combine(RootDir, "Upscale-Models");
        public static readonly string Vae          = Path.Combine(RootDir, "VAE-Models");
    }

    /// <summary>
    /// Shared resources under %LOCALAPPDATA%\Lazarus\Shared-Resources
    /// </summary>
    public static class SharedResources
    {
        public static readonly string RootDir       = Path.Combine(Root, "Shared-Resources");
        public static readonly string ExternalLinks = Path.Combine(RootDir, "External-Links");
        public static readonly string ImportExport  = Path.Combine(RootDir, "Import-Export");
    }

    /// <summary>
    /// System data under %LOCALAPPDATA%\Lazarus\System-Data
    /// </summary>
    public static class SystemData
    {
        public static readonly string RootDir   = Path.Combine(Root, "System-Data");
        public static readonly string Cache     = Path.Combine(RootDir, "Cache");
        public static readonly string Config    = Path.Combine(RootDir, "Configuration");
        public static readonly string Database  = Path.Combine(RootDir, "Database");
        public static readonly string Logs      = Path.Combine(RootDir, "Logs");

        // We may create this subfolder for model presets.
        public static readonly string ModelPresets = Path.Combine(Config, "Model-Presets");
    }

    /// <summary>
    /// User content under %LOCALAPPDATA%\Lazarus\User-Content
    /// </summary>
    public static class UserContent
    {
        public static readonly string RootDir         = Path.Combine(Root, "User-Content");
        public static readonly string GeneratedOutput = Path.Combine(RootDir, "Generated-Output");
        public static readonly string InputFiles      = Path.Combine(RootDir, "Input-Files");
        public static readonly string Projects        = Path.Combine(RootDir, "Projects");
    }

    /// <summary>
    /// Enumerates all standard Lazarus directories to ensure on first run.
    /// Order is not guaranteed; duplicates are not returned. Do not change names.
    /// </summary>
    public static System.Collections.Generic.IEnumerable<string> EnumerateAllDirectories()
    {
        // Keep this in sync with the structure above. Do not rename or reorder folders.
        var dirs = new System.Collections.Generic.List<string>
        {
            Root,
            FlatLogs,

            Models.RootDir,
            Models.BaseModels,
            Models.Embeddings,
            Models.LoRAAdapters,
            Models.Tokenizers,

            GenAssets.RootDir,
            GenAssets.ControlNet,
            GenAssets.StylePresets,
            GenAssets.Upscale,
            GenAssets.Vae,

            SharedResources.RootDir,
            SharedResources.ExternalLinks,
            SharedResources.ImportExport,

            SystemData.RootDir,
            SystemData.Cache,
            SystemData.Config,
            SystemData.ModelPresets,
            SystemData.Database,
            SystemData.Logs,

            UserContent.RootDir,
            UserContent.GeneratedOutput,
            UserContent.InputFiles,
            UserContent.Projects,
        };

        // Distinct by path
        var seen = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var d in dirs)
        {
            if (seen.Add(d))
                yield return d;
        }
    }
}
