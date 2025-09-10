using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lazarus.Desktop.ViewModels;

namespace Lazarus.Desktop.Services;

/// <summary>
/// Service interface for audio file management and playback
/// </summary>
public interface IAudioService : IDisposable
{
    /// <summary>
    /// Gets all audio files from the configured directory
    /// </summary>
    Task<IReadOnlyList<AudioItem>> GetAudioFilesAsync();

    /// <summary>
    /// Imports an audio file to the managed directory
    /// </summary>
    Task ImportFileAsync(string filePath);

    /// <summary>
    /// Starts audio recording
    /// </summary>
    Task<string> StartRecordingAsync();

    /// <summary>
    /// Stops the current recording
    /// </summary>
    Task StopRecordingAsync();

    /// <summary>
    /// Plays an audio file
    /// </summary>
    Task PlayAsync(string filePath);

    /// <summary>
    /// Pauses the current playback
    /// </summary>
    Task PauseAsync();

    /// <summary>
    /// Stops the current playback
    /// </summary>
    void Stop();

    /// <summary>
    /// Gets the current playback position
    /// </summary>
    TimeSpan? GetPlaybackPosition();

    /// <summary>
    /// Deletes an audio file
    /// </summary>
    Task DeleteFileAsync(string filePath);
}