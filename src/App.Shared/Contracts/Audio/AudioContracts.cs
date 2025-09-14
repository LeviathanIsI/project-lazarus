using System;
using System.Collections.Generic;
using System.Drawing;

namespace Lazarus.Shared.Contracts.AudioV2;

// Strict contracts for the new Audio workspace. Placed under AudioV2 to avoid
// conflicts with existing v1 Audio contracts used elsewhere in the app.

public sealed record AudioItem(
    Guid Id,
    string Name,
    string FullPath,
    TimeSpan Duration,
    long SizeBytes,
    DateTime ModifiedUtc,
    int SampleRate,
    int Channels,
    int BitrateKbps,
    string Format,
    string Sha256,
    string WaveformPngPath
);

public enum AudioJobType { Asr, NoiseReduce, VadTrim, Normalize, Convert, SplitOnSilence, Tts, VoiceClone }
public enum JobStatus { Pending, Running, Succeeded, Failed, Canceled }

public sealed class AudioJob
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public AudioJobType Type { get; init; }
    public IReadOnlyList<Guid> AudioItemIds { get; init; } = Array.Empty<Guid>();
    public double Progress01 { get; set; }
    public string? EtaText { get; set; }
    public JobStatus Status { get; set; } = JobStatus.Pending;
    public string? LogPath { get; set; }
    public string? OutputPath { get; set; }
    public DateTime StartedUtc { get; set; }
    public DateTime? EndedUtc { get; set; }
}

public sealed record AsrOptions(string ModelName, string Language, bool Diarize, bool WordTimestamps);
public sealed record NoiseOptions(bool UseRNNoise, bool UseNV, float Strength01);
public sealed record VadOptions(float ThresholdDb, int MinSilenceMs, int MinChunkMs);
public sealed record ConvertOptions(string Format, int? SampleRate = null, int? BitrateKbps = null);
public sealed record TtsOptions(string VoiceName, float Rate = 1f, float Pitch = 0f);

