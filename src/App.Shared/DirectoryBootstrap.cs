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
        LazarusPaths.Models.Diffusers,

        LazarusPaths.SharedResources.ExternalLinks,
        LazarusPaths.SharedResources.ImportExport,
        System.IO.Path.Combine(LazarusPaths.SharedResources.ImportExport, "Export"),
        System.IO.Path.Combine(LazarusPaths.SharedResources.ImportExport, "Import"),

        LazarusPaths.SystemData.Cache,
        System.IO.Path.Combine(LazarusPaths.SystemData.Cache, "Downloads"),
        LazarusPaths.SystemData.Config,
        LazarusPaths.SystemData.Config_App,
        LazarusPaths.SystemData.Config_Paths,
        LazarusPaths.SystemData.Config_Theme,
        LazarusPaths.SystemData.TrainingRecipes,
        LazarusPaths.SystemData.Pipelines,
        LazarusPaths.SystemData.Database,
        LazarusPaths.SystemData.Logs,
        LazarusPaths.SystemData.Temp,
        LazarusPaths.SystemData.ModelPresets,

        LazarusPaths.UserContent.GeneratedOutput,
        LazarusPaths.UserContent.InputFiles,
        LazarusPaths.UserContent.Projects,
        LazarusPaths.UserContent.Scratch,

        LazarusPaths.FlatLogs,

        // Additional top-level convenience folders wired in the Paths UI
        System.IO.Path.Combine(LazarusPaths.Models.RootDir, "Quantized"),
        System.IO.Path.Combine(LazarusPaths.Root, "Conversations"),
        System.IO.Path.Combine(LazarusPaths.Root, "Backups"),
        System.IO.Path.Combine(LazarusPaths.SharedResources.RootDir, "Templates"),
        System.IO.Path.Combine(LazarusPaths.Root, "Plugins"),
        // New top-level projects root (project-specific subtrees created on demand)
        System.IO.Path.Combine(LazarusPaths.Root, "Projects"),

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
        // Engine-scoped runners (per-backend)
        System.IO.Path.Combine(LazarusPaths.Root, "Runners", "LlamaCpp"),
        System.IO.Path.Combine(LazarusPaths.Root, "Runners", "LlamaCpp", "Binaries"),
        System.IO.Path.Combine(LazarusPaths.Root, "Runners", "LlamaCpp", "Config"),
        System.IO.Path.Combine(LazarusPaths.Root, "Runners", "LlamaCpp", "Cache"),
        System.IO.Path.Combine(LazarusPaths.Root, "Runners", "vLLM"),
        System.IO.Path.Combine(LazarusPaths.Root, "Runners", "vLLM", "Env"),
        System.IO.Path.Combine(LazarusPaths.Root, "Runners", "vLLM", "Config"),
        System.IO.Path.Combine(LazarusPaths.Root, "Runners", "vLLM", "Cache"),
        System.IO.Path.Combine(LazarusPaths.Root, "Runners", "ExLlamaV2"),
        System.IO.Path.Combine(LazarusPaths.Root, "Runners", "ExLlamaV2", "Binaries"),
        System.IO.Path.Combine(LazarusPaths.Root, "Runners", "ExLlamaV2", "Config"),
        System.IO.Path.Combine(LazarusPaths.Root, "Runners", "ExLlamaV2", "Cache"),

        // Trainers
        System.IO.Path.Combine(LazarusPaths.Root, "Trainers"),
        System.IO.Path.Combine(LazarusPaths.Root, "Trainers", "LLaMA-Factory"),
        System.IO.Path.Combine(LazarusPaths.Root, "Trainers", "LLaMA-Factory", "Env"),
        System.IO.Path.Combine(LazarusPaths.Root, "Trainers", "LLaMA-Factory", "Config"),
        System.IO.Path.Combine(LazarusPaths.Root, "Trainers", "LLaMA-Factory", "Logs"),
        System.IO.Path.Combine(LazarusPaths.Root, "Trainers", "Axolotl"),
        System.IO.Path.Combine(LazarusPaths.Root, "Trainers", "Axolotl", "Env"),
        System.IO.Path.Combine(LazarusPaths.Root, "Trainers", "Axolotl", "Config"),
        System.IO.Path.Combine(LazarusPaths.Root, "Trainers", "Axolotl", "Logs"),
        System.IO.Path.Combine(LazarusPaths.Root, "Trainers", "Unsloth"),
        System.IO.Path.Combine(LazarusPaths.Root, "Trainers", "Unsloth", "Env"),
        System.IO.Path.Combine(LazarusPaths.Root, "Trainers", "Unsloth", "Config"),
        System.IO.Path.Combine(LazarusPaths.Root, "Trainers", "Unsloth", "Logs"),

        // Global audio engines & voices
        System.IO.Path.Combine(LazarusPaths.Root, "Audio"),
        System.IO.Path.Combine(LazarusPaths.Root, "Audio", "ASR"),
        System.IO.Path.Combine(LazarusPaths.Root, "Audio", "ASR", "faster-whisper-Models"),
        System.IO.Path.Combine(LazarusPaths.Root, "Audio", "ASR", "Cache"),
        System.IO.Path.Combine(LazarusPaths.Root, "Audio", "TTS"),
        System.IO.Path.Combine(LazarusPaths.Root, "Audio", "TTS", "Piper-Models"),
        System.IO.Path.Combine(LazarusPaths.Root, "Audio", "TTS", "Voices"),
        System.IO.Path.Combine(LazarusPaths.Root, "Audio", "LipSync"),
        System.IO.Path.Combine(LazarusPaths.Root, "Audio", "LipSync", "Rhubarb"),

        // Global avatars assets
        System.IO.Path.Combine(LazarusPaths.Root, "Avatars"),
        System.IO.Path.Combine(LazarusPaths.Root, "Avatars", "Characters"),
        System.IO.Path.Combine(LazarusPaths.Root, "Avatars", "Rigs"),
        System.IO.Path.Combine(LazarusPaths.Root, "Avatars", "Poses"),

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
