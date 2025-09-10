using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Lazarus.Desktop.Services;

public sealed class DesignAudioService : IAudioService
{
    public Task<bool> IsSynthesisReadyAsync(CancellationToken ct) => Task.FromResult(true);

    public Task<IReadOnlyList<AudioItem>> ImportAsync(IEnumerable<string> paths, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<AudioItem>>(Array.Empty<AudioItem>());

    public Task<AudioItem?> GenerateAsync(AudioGenRequest request, CancellationToken ct)
        => Task.FromResult<AudioItem?>(null);

    public Task<AudioStats> GetStatsAsync(CancellationToken ct)
        => Task.FromResult(new AudioStats(
            TotalFiles: 7,
            GeneratedToday: 2,
            TotalDuration: TimeSpan.FromMinutes(23) + TimeSpan.FromSeconds(11)));
}

