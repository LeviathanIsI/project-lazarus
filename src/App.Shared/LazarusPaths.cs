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
        // Legacy flat folder retained for backward-compat probing; prefer structured subfolders
        public static readonly string StylePresets = Path.Combine(RootDir, "Style-Presets");
        public static readonly string StylePresets_LoRAs = Path.Combine(RootDir, "Style-Presets", "LoRAs");
        public static readonly string StylePresets_Embeddings = Path.Combine(RootDir, "Style-Presets", "Embeddings");
        public static readonly string StylePresets_Hypernetworks = Path.Combine(RootDir, "Style-Presets", "Hypernetworks");
        public static readonly string Upscale      = Path.Combine(RootDir, "Upscale-Models");
        public static readonly string Vae          = Path.Combine(RootDir, "VAE-Models");
    }

    /// <summary>
    /// Video-related assets under %LOCALAPPDATA%\Lazarus\Video-Assets
    /// </summary>
    public static class VideoAssets
    {
        public static readonly string RootDir           = Path.Combine(Root, "Video-Assets");
        public static readonly string AnimateDiff       = Path.Combine(RootDir, "AnimateDiff");
        public static readonly string TemporalLoRAs     = Path.Combine(RootDir, "Temporal-LoRAs");
        public static readonly string VideoControlNet   = Path.Combine(RootDir, "Video-ControlNet");
        public static readonly string FrameInterpolators= Path.Combine(RootDir, "Frame-Interpolators");
    }

    /// <summary>
    /// Audio-related assets under %LOCALAPPDATA%\Lazarus\Audio-Assets
    /// </summary>
    public static class AudioAssets
    {
        public static readonly string RootDir        = Path.Combine(Root, "Audio-Assets");
        public static readonly string AsrModels      = Path.Combine(RootDir, "ASR-Models");
        public static readonly string TtsVoices      = Path.Combine(RootDir, "TTS-Voices");
        public static readonly string VoiceCloning   = Path.Combine(RootDir, "Voice-Cloning");
        public static readonly string Vad            = Path.Combine(RootDir, "VAD");
        public static readonly string NoiseReduction = Path.Combine(RootDir, "Noise-Reduction");
    }

    /// <summary>
    /// 3D Avatar assets under %LOCALAPPDATA%\Lazarus\Avatar-Assets
    /// </summary>
    public static class AvatarAssets
    {
        public static readonly string RootDir   = Path.Combine(Root, "Avatar-Assets");
        public static readonly string Models3D  = Path.Combine(RootDir, "3D-Models");
        public static readonly string Rigs      = Path.Combine(RootDir, "Rigs");
        public static readonly string Textures  = Path.Combine(RootDir, "Textures");
        public static readonly string Visemes   = Path.Combine(RootDir, "Visemes");
    }

    /// <summary>
    /// RAG assets under %LOCALAPPDATA%\Lazarus\RAG-Assets
    /// </summary>
    public static class RagAssets
    {
        public static readonly string RootDir   = Path.Combine(Root, "RAG-Assets");
        public static readonly string Indexes   = Path.Combine(RootDir, "Indexes");
        public static readonly string Documents = Path.Combine(RootDir, "Documents");
        public static readonly string Presets   = Path.Combine(RootDir, "Presets");
    }

    /// <summary>
    /// Datasets at %LOCALAPPDATA%\Lazarus\Datasets
    /// </summary>
    public static class Datasets
    {
        public static readonly string RootDir       = Path.Combine(Root, "Datasets");
        public static readonly string Conversations = Path.Combine(RootDir, "Conversations");
        public static readonly string Images        = Path.Combine(RootDir, "Images");
        public static readonly string Video         = Path.Combine(RootDir, "Video");
        public static readonly string Audio         = Path.Combine(RootDir, "Audio");
    }

    /// <summary>
    /// Presets at %LOCALAPPDATA%\Lazarus\Presets
    /// </summary>
    public static class Presets
    {
        public static readonly string RootDir = Path.Combine(Root, "Presets");
        public static readonly string Image   = Path.Combine(RootDir, "Image");
        public static readonly string Video   = Path.Combine(RootDir, "Video");
        public static readonly string Audio   = Path.Combine(RootDir, "Audio");
    }

    /// <summary>
    /// Returns the first existing directory among candidates; if none exist,
    /// attempts to create the first (preferred) and returns it.
    /// </summary>
    public static string ResolveFirstExisting(params string[] candidates)
    {
        foreach (var c in candidates)
        {
            try { if (!string.IsNullOrWhiteSpace(c) && Directory.Exists(c)) return c; } catch { }
        }
        if (candidates.Length > 0 && !string.IsNullOrWhiteSpace(candidates[0]))
        {
            try { Directory.CreateDirectory(candidates[0]); } catch { }
            return candidates[0];
        }
        // Fallback to Root to avoid nulls
        return Root;
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
    /// Runner binaries under %LOCALAPPDATA%\Lazarus\Runners
    /// </summary>
    public static class Runners
    {
        public static readonly string RootDir  = Path.Combine(Root, "Runners");
        // Conventional engine folders (optional):
        public static readonly string LlamaCpp = Path.Combine(RootDir, "llama.cpp");
        public static readonly string Vllm     = Path.Combine(RootDir, "vllm");
        public static readonly string ExLlamaV2= Path.Combine(RootDir, "exllamav2");
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
            Path.Combine(GenAssets.RootDir, "Style-Presets"),
            GenAssets.StylePresets_LoRAs,
            GenAssets.StylePresets_Embeddings,
            GenAssets.StylePresets_Hypernetworks,
            GenAssets.Upscale,
            GenAssets.Vae,

            // Video assets
            VideoAssets.RootDir,
            VideoAssets.AnimateDiff,
            VideoAssets.TemporalLoRAs,
            VideoAssets.VideoControlNet,
            VideoAssets.FrameInterpolators,

            // Audio assets
            AudioAssets.RootDir,
            AudioAssets.AsrModels,
            AudioAssets.TtsVoices,
            AudioAssets.VoiceCloning,
            AudioAssets.Vad,
            AudioAssets.NoiseReduction,

            // Avatar assets
            AvatarAssets.RootDir,
            AvatarAssets.Models3D,
            AvatarAssets.Rigs,
            AvatarAssets.Textures,
            AvatarAssets.Visemes,

            // RAG assets
            RagAssets.RootDir,
            RagAssets.Indexes,
            RagAssets.Documents,
            RagAssets.Presets,

            // Datasets + Presets
            Datasets.RootDir,
            Datasets.Conversations,
            Datasets.Images,
            Datasets.Video,
            Datasets.Audio,
            Presets.RootDir,
            Presets.Image,
            Presets.Video,
            Presets.Audio,

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

            // Prepare Runners root so users can drop engines here
            Runners.RootDir,
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
