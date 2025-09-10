using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
namespace Lazarus.Desktop.Services;
public interface IAudioService
{
    Task<bool> IsSynthesisReadyAsync(CancellationToken ct);
    Task<IReadOnlyList<AudioItem>> ImportAsync(IEnumerable<string> paths, CancellationToken ct);
    Task<AudioItem?> GenerateAsync(AudioGenRequest request, CancellationToken ct);
    Task<AudioStats> GetStatsAsync(CancellationToken ct);
}

public sealed record AudioItem(
    string Id,
    string FilePath,
    string FileName,
    TimeSpan Duration,
    DateTime CreatedUtc,
    bool IsGenerated);

public sealed record AudioGenRequest(
    string Text,
    string Voice,
    int SampleRate);

public sealed record AudioStats(
    int TotalFiles,
    int GeneratedToday,
    TimeSpan TotalDuration);

