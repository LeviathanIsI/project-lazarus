using System;

namespace Lazarus.Shared.Contracts.Audio;

/// <summary>
/// Represents an audio file in the system
/// </summary>
public sealed class AudioItem
{
    public string Id { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string FileHash { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; }
    public DateTime ModifiedUtc { get; set; }
    public TimeSpan Duration { get; set; }
    public int SampleRate { get; set; }
    public int Channels { get; set; }
    public int Bitrate { get; set; }
    public string Format { get; set; } = string.Empty;
    public bool HasAnalysis { get; set; }
    public DateTime? AnalyzedUtc { get; set; }
}