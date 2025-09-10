using System;
using System.Collections.Generic;
using System.Linq;
using Lazarus.Shared.Runners;

namespace Lazarus.Backend.Services.Runners;

public interface IRunnerRegistry
{
    IEnumerable<RunnerDescriptor> GetAll();
    IEnumerable<RunnerDescriptor> GetByRole(RunnerRole role);
    RunnerDescriptor? GetById(string id);
}

public sealed class RunnerRegistry : IRunnerRegistry
{
    private readonly List<RunnerDescriptor> _runners;

    public RunnerRegistry()
    {
        _runners = LoadFromSettingsOrDisk();
    }

    public IEnumerable<RunnerDescriptor> GetAll() => _runners;

    public IEnumerable<RunnerDescriptor> GetByRole(RunnerRole role) =>
        _runners.Where(r => r.Role == role);

    public RunnerDescriptor? GetById(string id) =>
        _runners.FirstOrDefault(r => string.Equals(r.Id, id, StringComparison.OrdinalIgnoreCase));

    private static List<RunnerDescriptor> LoadFromSettingsOrDisk()
    {
        var list = new List<RunnerDescriptor>();
        try
        {
            var roots = new[]
            {
                Lazarus.Shared.LazarusPaths.Runners.Images_StableDiffusion,
                Lazarus.Shared.LazarusPaths.Runners.Images_SdWebUi,
                Lazarus.Shared.LazarusPaths.Runners.Images_ComfyUi,
                Lazarus.Shared.LazarusPaths.Runners.Images_InvokeAi,
                Lazarus.Shared.LazarusPaths.Runners.ImagesRoot,
                Lazarus.Shared.LazarusPaths.Runners.RootDir
            };

            var engineKindMap = new Dictionary<string, (string kind, int defaultPort)>(StringComparer.OrdinalIgnoreCase)
            {
                { "stable-diffusion", ("stable-diffusion", 7860) },
                { "sdwebui",          ("stable-diffusion", 7860) },
                { "comfyui",          ("comfyui", 8188) },
                { "invokeai",         ("stable-diffusion", 9090) }
            };

            string[] PatternsFor(string engine) => engine.ToLowerInvariant() switch
            {
                "stable-diffusion" => new[] { "webui-user.bat", "webui.bat", "launch*.bat", "start*.bat", "sd.exe", "sd*.exe" },
                "sdwebui" => new[] { "webui-user.bat", "webui.bat" },
                "comfyui" => new[] { "run*.bat", "main.py" },
                "invokeai" => new[] { "invoke*.bat", "invokeai*.exe" },
                _ => Array.Empty<string>()
            };

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var baseRoot in roots.Distinct().Where(d => !string.IsNullOrWhiteSpace(d)))
            {
                if (!System.IO.Directory.Exists(baseRoot)) continue;
                IEnumerable<string> engineDirs;
                try { engineDirs = System.IO.Directory.EnumerateDirectories(baseRoot, "*", System.IO.SearchOption.TopDirectoryOnly); }
                catch { continue; }

                foreach (var engineDir in engineDirs)
                {
                    var engine = System.IO.Path.GetFileName(engineDir);
                    if (!engineKindMap.TryGetValue(engine, out var meta)) continue;

                    foreach (var pattern in PatternsFor(engine))
                    {
                        IEnumerable<string> entries;
                        try { entries = System.IO.Directory.EnumerateFiles(engineDir, pattern, System.IO.SearchOption.AllDirectories); }
                        catch { continue; }
                        foreach (var entry in entries)
                        {
                            var folder = System.IO.Path.GetDirectoryName(entry) ?? engineDir;
                            var id = MakeId(entry);
                            if (!seen.Add(id)) continue;
                            var name = new System.IO.DirectoryInfo(folder).Name;
                            list.Add(new RunnerDescriptor
                            {
                                Id = id,
                                Name = name,
                                Kind = meta.kind,
                                ExecPath = entry,
                                BaseUrl = $"http://127.0.0.1:{meta.defaultPort}",
                                Role = RunnerRole.Image
                            });
                        }
                    }
                }
            }
        }
        catch { }

        return list;
    }

    private static string MakeId(string path)
    {
        try
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes(System.IO.Path.GetFullPath(path));
            var hash = sha.ComputeHash(bytes);
            return Convert.ToHexString(hash[..8]).ToLowerInvariant();
        }
        catch { return Guid.NewGuid().ToString("n"); }
    }
}
