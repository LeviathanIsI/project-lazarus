using System;
using System.Collections.Generic;
// System.Drawing is avoided in stub to keep cross-platform build clean
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lazarus.Shared;
using Lazarus.Shared.Contracts.AudioV2;

namespace Lazarus.Backend.Services.Audio;

internal sealed class SimpleSubject<T> : IObservable<T>, IDisposable
{
    private readonly List<IObserver<T>> _observers = new();
    private bool _disposed;
    public IDisposable Subscribe(IObserver<T> observer)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(SimpleSubject<T>));
        if (!_observers.Contains(observer)) _observers.Add(observer);
        return new Unsubscriber(_observers, observer);
    }
    public void OnNext(T value)
    {
        foreach (var o in _observers.ToArray()) o.OnNext(value);
    }
    public void OnCompleted() { foreach (var o in _observers.ToArray()) o.OnCompleted(); }
    public void OnError(Exception error) { foreach (var o in _observers.ToArray()) o.OnError(error); }
    public void Dispose() { _disposed = true; _observers.Clear(); }
    private sealed class Unsubscriber : IDisposable
    {
        private readonly List<IObserver<T>> _list; private readonly IObserver<T> _obs;
        public Unsubscriber(List<IObserver<T>> list, IObserver<T> obs) { _list = list; _obs = obs; }
        public void Dispose() { if (_list.Contains(_obs)) _list.Remove(_obs); }
    }
}

public sealed class AudioLibraryStub : IAudioLibrary
{
    public Task<IReadOnlyList<AudioItem>> ScanAsync(CancellationToken ct)
    {
        LazarusPaths.Audio.EnsureDirectories();
        // Placeholder: return empty to trigger UI placeholder mode
        return Task.FromResult((IReadOnlyList<AudioItem>)Array.Empty<AudioItem>());
    }

    public Task<AudioItem> ImportAsync(string filePath, CancellationToken ct)
    {
        LazarusPaths.Audio.EnsureDirectories();
        var dest = Path.Combine(LazarusPaths.Audio.Input, Path.GetFileName(filePath));
        Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
        File.Copy(filePath, dest, overwrite: false);
        var fi = new FileInfo(dest);
        var item = new AudioItem(
            Guid.NewGuid(), fi.Name, fi.FullName, TimeSpan.Zero, fi.Length,
            fi.LastWriteTimeUtc, 44100, 2, 192, Path.GetExtension(fi.Name).Trim('.').ToUpperInvariant(), "", "");
        return Task.FromResult(item);
    }

    public Task DeleteAsync(Guid id, CancellationToken ct)
    {
        // No-op in stub
        return Task.CompletedTask;
    }

    public Task<string> EnsureWaveformPreviewAsync(Guid id, System.Drawing.Size size, CancellationToken ct)
    {
        LazarusPaths.Audio.EnsureDirectories();
        var path = Path.Combine(LazarusPaths.Audio.Waveforms, id.ToString("N") + $"_{size.Width}x{size.Height}.png");
        if (!File.Exists(path))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            // Write an empty file as placeholder; UI will handle missing image content gracefully
            using var fs = File.Create(path);
        }
        return Task.FromResult(path);
    }
}

public sealed class AudioTransportStub : IAudioTransport, IDisposable
{
    private readonly SimpleSubject<(double pos, double tot)> _timeline = new();
    private readonly SimpleSubject<(float, float, float, float)> _meters = new();
    private readonly System.Timers.Timer _timer;
    private double _pos; private double _tot = 120;
    public IObservable<(double positionSec, double totalSec)> Timeline => _timeline;
    public IObservable<(float peakL, float peakR, float rmsL, float rmsR)> Meters => _meters;
    public AudioTransportStub()
    {
        _timer = new System.Timers.Timer(200);
        _timer.Elapsed += (_, _) =>
        {
            _pos = Math.Min(_tot, _pos + 0.2);
            _timeline.OnNext((_pos, _tot));
            var t = (float)(_pos % 1.0);
            _meters.OnNext((0.3f + t * 0.4f, 0.35f + t * 0.35f, 0.2f + t * 0.2f, 0.22f + t * 0.18f));
        };
    }
    public Task PlayAsync(Guid id, string? region = null, CancellationToken ct = default) { _timer.Start(); return Task.CompletedTask; }
    public Task PauseAsync() { _timer.Stop(); return Task.CompletedTask; }
    public Task StopAsync() { _timer.Stop(); _pos = 0; _timeline.OnNext((_pos, _tot)); return Task.CompletedTask; }
    public Task SetOutputDeviceAsync(string deviceId) { return Task.CompletedTask; }
    public Task SetVolumeAsync(double volume01) { return Task.CompletedTask; }
    public void Dispose() { _timer.Dispose(); _timeline.Dispose(); _meters.Dispose(); }
}

public sealed class AsrServiceStub : IAsrService
{
    public Task<AudioJob> TranscribeAsync(Guid id, AsrOptions options, CancellationToken ct)
        => Task.FromResult(new AudioJob { Type = AudioJobType.Asr, AudioItemIds = new[] { id }, Status = JobStatus.Pending, StartedUtc = DateTime.UtcNow });
}

public sealed class NoiseServiceStub : INoiseService
{
    public Task<AudioJob> DenoiseAsync(Guid id, NoiseOptions options, CancellationToken ct)
        => Task.FromResult(new AudioJob { Type = AudioJobType.NoiseReduce, AudioItemIds = new[] { id }, Status = JobStatus.Pending, StartedUtc = DateTime.UtcNow });
}

public sealed class VadServiceStub : IVadService
{
    public Task<AudioJob> TrimAsync(Guid id, VadOptions options, CancellationToken ct)
        => Task.FromResult(new AudioJob { Type = AudioJobType.VadTrim, AudioItemIds = new[] { id }, Status = JobStatus.Pending, StartedUtc = DateTime.UtcNow });
    public Task<IReadOnlyList<(TimeSpan start, TimeSpan end)>> DetectAsync(Guid id, VadOptions options, CancellationToken ct)
        => Task.FromResult((IReadOnlyList<(TimeSpan, TimeSpan)>)Array.Empty<(TimeSpan, TimeSpan)>());
}

public sealed class ConversionServiceStub : IConversionService
{
    public Task<AudioJob> ConvertAsync(IReadOnlyList<Guid> ids, ConvertOptions options, CancellationToken ct)
        => Task.FromResult(new AudioJob { Type = AudioJobType.Convert, AudioItemIds = ids, Status = JobStatus.Pending, StartedUtc = DateTime.UtcNow });
    public Task<AudioJob> NormalizeAsync(IReadOnlyList<Guid> ids, float TargetLufs, CancellationToken ct)
        => Task.FromResult(new AudioJob { Type = AudioJobType.Normalize, AudioItemIds = ids, Status = JobStatus.Pending, StartedUtc = DateTime.UtcNow });
    public Task<AudioJob> SplitOnSilenceAsync(Guid id, VadOptions options, CancellationToken ct)
        => Task.FromResult(new AudioJob { Type = AudioJobType.SplitOnSilence, AudioItemIds = new[] { id }, Status = JobStatus.Pending, StartedUtc = DateTime.UtcNow });
}

public sealed class TtsServiceStub : ITtsService
{
    public Task<AudioJob> SynthesizeAsync(string text, TtsOptions options, string outputPath, CancellationToken ct)
        => Task.FromResult(new AudioJob { Type = AudioJobType.Tts, AudioItemIds = Array.Empty<Guid>(), Status = JobStatus.Pending, StartedUtc = DateTime.UtcNow, OutputPath = outputPath });
}

public sealed class VoiceCloneServiceStub : IVoiceCloneService
{
    public Task<AudioJob> CreateVoiceAsync(Guid id, string voiceName, CancellationToken ct)
        => Task.FromResult(new AudioJob { Type = AudioJobType.VoiceClone, AudioItemIds = new[] { id }, Status = JobStatus.Pending, StartedUtc = DateTime.UtcNow });
}
