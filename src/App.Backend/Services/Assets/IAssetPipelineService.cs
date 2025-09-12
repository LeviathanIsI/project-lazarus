using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Lazarus.Backend.Services.Assets;

public enum AssetTool
{
    Ffmpeg,
    Toktx,
    Basisu,
    GltfTransform,
    Gltfpack
}

public sealed class AssetProcessResult
{
    public int ExitCode { get; init; }
    public string StdOut { get; init; } = string.Empty;
    public string StdErr { get; init; } = string.Empty;
}

public interface IAssetPipelineService
{
    Task<AssetProcessResult> RunAsync(AssetTool tool, string arguments, string? workingDirectory = null, CancellationToken cancellationToken = default);
}

