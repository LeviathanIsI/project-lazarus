using System;

namespace Lazarus.Shared.Contracts.Audio;

/// <summary>
/// Audio analysis results with metadata and waveform data
/// </summary>
public sealed class AudioAnalysis
{
    public string Id { get; set; } = string.Empty;
    public string FileHash { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public int Bitrate { get; set; }
    public int SampleRate { get; set; }
    public int Channels { get; set; }
    public byte[]? WaveformSmall { get; set; }  // 320x40 PNG for list view
    public byte[]? WaveformLarge { get; set; }  // 640x120 PNG for inspector
    public string? TranscriptPath { get; set; }
    public bool HasEmbedding { get; set; }
    public DateTime AnalyzedUtc { get; set; }
    public string? CodecName { get; set; }
    public long TotalSamples { get; set; }
}