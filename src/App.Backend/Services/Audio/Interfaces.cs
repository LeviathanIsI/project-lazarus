using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using Lazarus.Shared.Contracts.AudioV2;

namespace Lazarus.Backend.Services.Audio;

public interface IAudioLibrary
{
    Task<IReadOnlyList<AudioItem>> ScanAsync(CancellationToken ct);
    Task<AudioItem> ImportAsync(string filePath, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
    Task<string> EnsureWaveformPreviewAsync(Guid id, Size size, CancellationToken ct);
}

public interface IAudioTransport
{
    Task PlayAsync(Guid id, string? region = null, CancellationToken ct = default);
    Task PauseAsync();
    Task StopAsync();
    IObservable<(double positionSec, double totalSec)> Timeline { get; }
    IObservable<(float peakL, float peakR, float rmsL, float rmsR)> Meters { get; }
    Task SetOutputDeviceAsync(string deviceId);
    Task SetVolumeAsync(double volume01);
}

public interface IAsrService
{
    Task<AudioJob> TranscribeAsync(Guid id, AsrOptions options, CancellationToken ct);
}

public interface INoiseService
{
    Task<AudioJob> DenoiseAsync(Guid id, NoiseOptions options, CancellationToken ct);
}

public interface IVadService
{
    Task<AudioJob> TrimAsync(Guid id, VadOptions options, CancellationToken ct);
    Task<IReadOnlyList<(TimeSpan start, TimeSpan end)>> DetectAsync(Guid id, VadOptions options, CancellationToken ct);
}

public interface IConversionService
{
    Task<AudioJob> ConvertAsync(IReadOnlyList<Guid> ids, ConvertOptions options, CancellationToken ct);
    Task<AudioJob> NormalizeAsync(IReadOnlyList<Guid> ids, float TargetLufs, CancellationToken ct);
    Task<AudioJob> SplitOnSilenceAsync(Guid id, VadOptions options, CancellationToken ct);
}

public interface ITtsService
{
    Task<AudioJob> SynthesizeAsync(string text, TtsOptions options, string outputPath, CancellationToken ct);
}

public interface IVoiceCloneService
{
    Task<AudioJob> CreateVoiceAsync(Guid id, string voiceName, CancellationToken ct);
}

