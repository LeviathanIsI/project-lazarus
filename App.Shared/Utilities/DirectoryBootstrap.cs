using System;
using System.IO;

namespace Lazarus.Shared.Utilities
{
    public static class DirectoryBootstrap
    {
        private static readonly string[] RequiredDirs = new[]
        {
            "models/main",
            "models/loras",
            "models/embeddings",
            "models/vaes",
            "models/controlnets",
            "models/hypernetworks",
            "models/safetensors",
            "audio/tts",
            "audio/asr",
            "audio/visemes",
            "plugins",
            "jobs/training",
            "jobs/outputs",
            "datasets",
            "logs"
        };

        public static string GetRootPath()
        {
            // User-mode install (AppData). 
            // Swap this for portable mode if desired.
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Lazarus"
            );
        }

        public static void EnsureDirectories()
        {
            var rootPath = GetRootPath();

            foreach (var dir in RequiredDirs)
            {
                var fullPath = Path.Combine(rootPath, dir);
                if (!Directory.Exists(fullPath))
                    Directory.CreateDirectory(fullPath);
            }
        }
    }
}
