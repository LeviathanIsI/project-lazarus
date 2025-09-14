using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Lazarus.Shared.Contracts.Audio;

namespace Lazarus.Backend.Services.Audio;

/// <summary>
/// Service interface for audio file management and playback
/// </summary>
public interface IAudioService
{
    /// <summary>
    /// Lists all audio files in the import/export directory
    /// </summary>
    Task<IReadOnlyList<AudioItem>> ListAsync(CancellationToken ct = default);

    /// <summary>
    /// Imports an audio file into the managed directory
    /// </summary>
    Task<AudioItem?> ImportAsync(string sourcePath, CancellationToken ct = default);

    /// <summary>
    /// Deletes an audio file and its associated data
    /// </summary>
    Task DeleteAsync(string id, CancellationToken ct = default);

    /// <summary>
    /// Analyzes an audio file and generates waveform visualizations
    /// </summary>
    Task<AudioAnalysis> AnalyzeAsync(string id, bool force, CancellationToken ct = default);

    /// <summary>
    /// Starts playback of an audio file
    /// </summary>
    IPlaybackSession Play(string id);
}

/// <summary>
/// Audio playback session control
/// </summary>
public interface IPlaybackSession : IDisposable
{
    string AudioId { get; }
    TimeSpan Position { get; set; }
    TimeSpan Duration { get; }
    PlaybackState State { get; }
    float Volume { get; set; }

    void Pause();
    void Resume();
    void Stop();

    event EventHandler<PlaybackStateChangedEventArgs>? StateChanged;
    event EventHandler<TimeSpan>? PositionChanged;
}

public enum PlaybackState
{
    Stopped,
    Playing,
    Paused
}

public sealed class PlaybackStateChangedEventArgs : EventArgs
{
    public PlaybackState OldState { get; }
    public PlaybackState NewState { get; }

    public PlaybackStateChangedEventArgs(PlaybackState oldState, PlaybackState newState)
    {
        OldState = oldState;
        NewState = newState;
    }
}