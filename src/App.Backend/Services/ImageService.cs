using Lazarus.Data.Entities;
using Microsoft.Extensions.Logging;

namespace Lazarus.Backend.Services;

public interface IImageService
{
    Task<string> GenerateAsync(ImageJob job, CancellationToken cancellationToken = default);
}

/// <summary>
/// Placeholder image generation service. Writes a small PNG to the output folder
/// and returns its path. Real implementation will integrate with engines.
/// </summary>
public sealed class ImageService : IImageService
{
    private readonly ILogger<ImageService>? _logger;
    public ImageService(ILogger<ImageService>? logger = null) { _logger = logger; }

    public Task<string> GenerateAsync(ImageJob job, CancellationToken cancellationToken = default)
    {
        var outDir = Lazarus.Shared.LazarusPaths.UserContent.GeneratedOutput;
        try { Directory.CreateDirectory(outDir); } catch { }
        var file = Path.Combine(outDir, $"dummy-{DateTime.UtcNow:yyyyMMdd-HHmmssfff}.png");
        File.WriteAllBytes(file, DummyPngBytes);
        _logger?.LogInformation("Dummy image generated at {Path}", file);
        return Task.FromResult(file);
    }

    // 1x1 transparent PNG
    private static readonly byte[] DummyPngBytes = new byte[] {
        137,80,78,71,13,10,26,10,0,0,0,13,73,72,68,82,0,0,0,1,0,0,0,1,8,6,0,0,0,31,21,196,137,
        0,0,0,12,73,68,65,84,120,156,99,248,15,4,0,9,251,3,253,164,184,61,84,0,0,0,0,73,69,78,
        68,174,66,96,130
    };
}

