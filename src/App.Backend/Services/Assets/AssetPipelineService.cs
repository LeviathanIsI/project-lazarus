using System.Diagnostics;
using System.Text;
using Lazarus.Shared;
using Lazarus.Shared.Settings;
using Microsoft.Extensions.Logging;

namespace Lazarus.Backend.Services.Assets;

public sealed class AssetPipelineService : IAssetPipelineService
{
    private readonly ILogger<AssetPipelineService> _logger;
    private readonly ISettingsService _settings;

    public AssetPipelineService(ILogger<AssetPipelineService> logger, ISettingsService settings)
    {
        _logger = logger;
        _settings = settings;
    }

    public async Task<AssetProcessResult> RunAsync(AssetTool tool, string arguments, string? workingDirectory = null, CancellationToken cancellationToken = default)
    {
        var exePath = ResolveToolPath(tool);
        if (string.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath))
        {
            _logger.LogWarning("Tool {Tool} not found at {Path}", tool, exePath);
            return new AssetProcessResult { ExitCode = -1, StdErr = $"Tool not found: {tool} => {exePath}" };
        }

        Directory.CreateDirectory(LazarusPaths.SystemData.Temp);
        var psi = new ProcessStartInfo(exePath)
        {
            Arguments = arguments,
            WorkingDirectory = string.IsNullOrWhiteSpace(workingDirectory) ? LazarusPaths.SystemData.Temp : workingDirectory,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        var stdout = new StringBuilder();
        var stderr = new StringBuilder();

        using var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };
        proc.OutputDataReceived += (_, e) => { if (e.Data != null) stdout.AppendLine(e.Data); };
        proc.ErrorDataReceived += (_, e) => { if (e.Data != null) stderr.AppendLine(e.Data); };

        _logger.LogInformation("Running {Tool} {Args}", tool, arguments);
        proc.Start();
        proc.BeginOutputReadLine();
        proc.BeginErrorReadLine();

        await proc.WaitForExitAsync(cancellationToken);
        var result = new AssetProcessResult { ExitCode = proc.ExitCode, StdOut = stdout.ToString(), StdErr = stderr.ToString() };
        if (result.ExitCode != 0)
        {
            _logger.LogWarning("{Tool} exited with {Code}: {Err}", tool, result.ExitCode, Truncate(result.StdErr));
        }
        return result;
    }

    private string ResolveToolPath(AssetTool tool)
    {
        var s = _settings.Current;
        var preferBundled = s.PreferBundledTools;

        string bundled(string dir, string exe) => Path.Combine(dir, exe);

        return tool switch
        {
            AssetTool.Ffmpeg => FirstExisting(
                preferBundled ? bundled(LazarusPaths.SystemData.Tools_Ffmpeg, "ffmpeg.exe") : null,
                s.ToolFfmpegPath,
                Path.Combine(Environment.GetEnvironmentVariable("ProgramFiles") ?? string.Empty, "ffmpeg", "bin", "ffmpeg.exe")
            ),
            AssetTool.Toktx => FirstExisting(
                preferBundled ? bundled(LazarusPaths.SystemData.Tools_Ktx, "toktx.exe") : null,
                s.ToolToktxPath
            ),
            AssetTool.Basisu => FirstExisting(
                preferBundled ? bundled(LazarusPaths.SystemData.Tools_Basisu, "basisu.exe") : null,
                s.ToolBasisuPath
            ),
            AssetTool.GltfTransform => FirstExisting(
                preferBundled ? bundled(LazarusPaths.SystemData.Tools_GltfTransform, "gltf-transform.cmd") : null,
                s.ToolGltfTransformPath
            ),
            AssetTool.Gltfpack => FirstExisting(
                preferBundled ? bundled(LazarusPaths.SystemData.Tools_Gltfpack, "gltfpack.exe") : null,
                s.ToolGltfpackPath
            ),
            _ => string.Empty
        } ?? string.Empty;
    }

    private static string? FirstExisting(params string?[] candidates)
    {
        foreach (var c in candidates)
        {
            try { if (!string.IsNullOrWhiteSpace(c) && File.Exists(c)) return c; } catch { }
        }
        return null;
    }

    private static string Truncate(string s, int max = 400)
        => s.Length <= max ? s : s.Substring(0, max) + "...";
}

