using System.IO;

namespace Lazarus.Shared;

public static class DirectoryBootstrap
{
    // Exact tree from screenshots (leaf directories only)
    public static System.Collections.Generic.IReadOnlyList<string> LeafDirectories => new[]
    {
        LazarusPaths.GenAssets.ControlNet,
        LazarusPaths.GenAssets.StableDiffusionModels,
        System.IO.Path.Combine(LazarusPaths.GenAssets.RootDir, "Style-Presets"),
        LazarusPaths.GenAssets.StylePresets_LoRAs,
        LazarusPaths.GenAssets.StylePresets_Embeddings,
        LazarusPaths.GenAssets.StylePresets_Hypernetworks,
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

        // Domainized Runners tree (idempotent)
        LazarusPaths.Runners.RootDir,
        LazarusPaths.Runners.ChatsRoot,
        LazarusPaths.Runners.ImagesRoot,
        LazarusPaths.Runners.VideosRoot,
        LazarusPaths.Runners.AudioRoot,
        LazarusPaths.Runners.AvatarsRoot,
        LazarusPaths.Runners.SharedRoot,
        LazarusPaths.Runners.Chats_LlamaCpp,
        LazarusPaths.Runners.Chats_Vllm,
        LazarusPaths.Runners.Chats_ExLlamaV2,
        LazarusPaths.Runners.Images_StableDiffusion,
        LazarusPaths.Runners.Images_ComfyUi,
        LazarusPaths.Runners.Images_SdWebUi,
        LazarusPaths.Runners.Images_InvokeAi,
        LazarusPaths.Runners.Videos_AnimateDiff,
        LazarusPaths.Runners.Videos_Svd,
        LazarusPaths.Runners.Videos_Rife,
        LazarusPaths.Runners.Audio_FasterWhisper,
        LazarusPaths.Runners.Audio_Piper,
        LazarusPaths.Runners.Audio_Rvc,
        LazarusPaths.Runners.Audio_NoiseReduction,
        LazarusPaths.Runners.Avatars_Rhubarb,
        LazarusPaths.Runners.Avatars_TripoSr,
        LazarusPaths.Runners.Avatars_Nerfstudio,
        LazarusPaths.Runners.Shared_Ffmpeg,
        LazarusPaths.Runners.Shared_Utils,

        // Video assets
        LazarusPaths.VideoAssets.RootDir,
        LazarusPaths.VideoAssets.AnimateDiff,
        LazarusPaths.VideoAssets.TemporalLoRAs,
        LazarusPaths.VideoAssets.VideoControlNet,
        LazarusPaths.VideoAssets.FrameInterpolators,

        // Audio assets
        LazarusPaths.AudioAssets.RootDir,
        LazarusPaths.AudioAssets.AsrModels,
        LazarusPaths.AudioAssets.TtsVoices,
        LazarusPaths.AudioAssets.VoiceCloning,
        LazarusPaths.AudioAssets.Vad,
        LazarusPaths.AudioAssets.NoiseReduction,

        // Avatar assets
        LazarusPaths.AvatarAssets.RootDir,
        LazarusPaths.AvatarAssets.Models3D,
        LazarusPaths.AvatarAssets.Rigs,
        LazarusPaths.AvatarAssets.Textures,
        LazarusPaths.AvatarAssets.Visemes,

        // RAG assets
        LazarusPaths.RagAssets.RootDir,
        LazarusPaths.RagAssets.Indexes,
        LazarusPaths.RagAssets.Documents,
        LazarusPaths.RagAssets.Presets,

        // Datasets
        LazarusPaths.Datasets.RootDir,
        LazarusPaths.Datasets.Conversations,
        LazarusPaths.Datasets.Images,
        LazarusPaths.Datasets.Video,
        LazarusPaths.Datasets.Audio,

        // Presets
        LazarusPaths.Presets.RootDir,
        LazarusPaths.Presets.Image,
        LazarusPaths.Presets.Video,
        LazarusPaths.Presets.Audio,
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
