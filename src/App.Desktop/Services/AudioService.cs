using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lazarus.Shared;
using Microsoft.Extensions.Logging;
using NAudio.Wave;

namespace Lazarus.Desktop.Services;

public sealed class AudioService : IAudioService
{
    private readonly ILogger<AudioService> _log;
    private readonly string _root;

    public AudioService(ILogger<AudioService> log)
    {
        _log = log;
        _root = Path.Combine(LazarusPaths.Root, "Audio");
        Directory.CreateDirectory(ImportedDir);
        Directory.CreateDirectory(GeneratedDir);
    }

    private string ImportedDir => Path.Combine(_root, "Imported");
    private string GeneratedDir => Path.Combine(_root, "Generated");

    public Task<bool> IsSynthesisReadyAsync(CancellationToken ct)
    {
        // Stub for now  later: check Piper binary/voice presence, etc.
        return Task.FromResult(true);
    }

    public Task<IReadOnlyList<AudioItem>> ListAsync(CancellationToken ct)
    {
        var list = EnumerateSafe(ImportedDir).Concat(EnumerateSafe(GeneratedDir)).ToList();
        return Task.FromResult<IReadOnlyList<AudioItem>>(list);
    }

    public async Task<IReadOnlyList<AudioItem>> ImportAsync(IEnumerable<string> paths, CancellationToken ct)
    {
        var results = new List<AudioItem>();
        foreach (var src in paths.Where(File.Exists))
        {
            ct.ThrowIfCancellationRequested();

            var dest = Path.Combine(ImportedDir, Path.GetFileName(src));
            // If collision, add suffix
            dest = EnsureUnique(dest);
            File.Copy(src, dest);

            var dur = TryGetDuration(dest);
            results.Add(new AudioItem(
                Id: Guid.NewGuid().ToString("n", CultureInfo.InvariantCulture),
                FilePath: dest,
                FileName: Path.GetFileName(dest),
                Duration: dur,
                CreatedUtc: File.GetCreationTimeUtc(dest),
                IsGenerated: false));
        }

        return await Task.FromResult(results);
    }

    public async Task<AudioItem?> GenerateAsync(AudioGenRequest request, CancellationToken ct)
    {
        // Stub generation: write a silent WAV with the requested sample rate so the pipeline is alive.
        // Later, replace with Piper/runner call.
        var file = Path.Combine(GeneratedDir,
            $"gen_{DateTime.UtcNow:yyyyMMdd_HHmmssfff}.wav");

        CreateSilentWav(file, request.SampleRate, seconds: 2);

        var item = new AudioItem(
            Id: Guid.NewGuid().ToString("n"),
            FilePath: file,
            FileName: Path.GetFileName(file),
            Duration: TryGetDuration(file),
            CreatedUtc: DateTime.UtcNow,
            IsGenerated: true);

        return await Task.FromResult(item);
    }

    public async Task<AudioStats> GetStatsAsync(CancellationToken ct)
    {
        var all = EnumerateSafe(ImportedDir).Concat(EnumerateSafe(GeneratedDir)).ToList();
        var todayUtc = DateTime.UtcNow.Date;

        int total = all.Count;
        int genToday = all.Count(p => p.IsGenerated && p.CreatedUtc.Date == todayUtc);
        var totalDur = new TimeSpan(all.Sum(p => p.Duration.Ticks));

        return await Task.FromResult(new AudioStats(total, genToday, totalDur));
    }

    // helpers
    private static string EnsureUnique(string path)
    {
        if (!File.Exists(path)) return path;
        var dir = Path.GetDirectoryName(path)!;
        var name = Path.GetFileNameWithoutExtension(path);
        var ext = Path.GetExtension(path);
        int i = 1;
        string candidate;
        do
        {
            candidate = Path.Combine(dir, $"{name} ({i++}){ext}");
        } while (File.Exists(candidate));
        return candidate;
    }

    private static TimeSpan TryGetDuration(string file)
    {
        try
        {
            using var reader = new AudioFileReader(file);
            return reader.TotalTime;
        }
        catch
        {
            return TimeSpan.Zero;
        }
    }

    private IEnumerable<AudioItem> EnumerateSafe(string dir)
    {
        if (!Directory.Exists(dir)) yield break;

        foreach (var f in Directory.EnumerateFiles(dir))
        {
            var isGen = dir.EndsWith("Generated", StringComparison.OrdinalIgnoreCase);
            yield return new AudioItem(
                Id: Guid.NewGuid().ToString("n"),
                FilePath: f,
                FileName: Path.GetFileName(f),
                Duration: TryGetDuration(f),
                CreatedUtc: File.GetCreationTimeUtc(f),
                IsGenerated: isGen);
        }
    }

    private static void CreateSilentWav(string path, int sampleRate, int seconds)
    {
        using var sw = new WaveFileWriter(path, new WaveFormat(sampleRate, 16, 1));
        var bytes = new byte[sampleRate * seconds * 2];
        sw.Write(bytes, 0, bytes.Length);
    }
}

