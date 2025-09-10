using System;
using System.Threading;
using NAudio.Wave;

namespace Lazarus.Backend.Services.Audio;

/// <summary>
/// Internal implementation of audio playback session
/// </summary>
internal sealed class PlaybackSession : IPlaybackSession
{
    private readonly WaveOutEvent _waveOut;
    private readonly AudioFileReader _audioFile;
    private readonly Timer _positionTimer;
    private PlaybackState _state = PlaybackState.Stopped;
    private bool _disposed;

    public string AudioId { get; }
    public TimeSpan Duration { get; }
    public PlaybackState State => _state;

    public TimeSpan Position
    {
        get => _audioFile.CurrentTime;
        set => _audioFile.CurrentTime = value;
    }

    public float Volume
    {
        get => _waveOut.Volume;
        set => _waveOut.Volume = Math.Max(0, Math.Min(1, value));
    }

    public event EventHandler<PlaybackStateChangedEventArgs>? StateChanged;
    public event EventHandler<TimeSpan>? PositionChanged;

    public PlaybackSession(string audioId, string filePath)
    {
        AudioId = audioId;
        _audioFile = new AudioFileReader(filePath);
        Duration = _audioFile.TotalTime;
        
        _waveOut = new WaveOutEvent
        {
            DesiredLatency = 150
        };
        _waveOut.Init(_audioFile);
        
        _waveOut.PlaybackStopped += OnPlaybackStopped;
        
        // Position update timer
        _positionTimer = new Timer(_ =>
        {
            if (_state == PlaybackState.Playing)
            {
                PositionChanged?.Invoke(this, Position);
            }
        }, null, Timeout.Infinite, Timeout.Infinite);
        
        // Auto-play on creation
        Resume();
    }

    public void Pause()
    {
        if (_state != PlaybackState.Playing) return;
        
        _waveOut.Pause();
        _positionTimer.Change(Timeout.Infinite, Timeout.Infinite);
        ChangeState(PlaybackState.Paused);
    }

    public void Resume()
    {
        if (_state == PlaybackState.Playing) return;
        
        _waveOut.Play();
        _positionTimer.Change(100, 100); // Update every 100ms
        ChangeState(PlaybackState.Playing);
    }

    public void Stop()
    {
        _waveOut.Stop();
        _audioFile.Position = 0;
        _positionTimer.Change(Timeout.Infinite, Timeout.Infinite);
        ChangeState(PlaybackState.Stopped);
        PositionChanged?.Invoke(this, TimeSpan.Zero);
    }

    private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
    {
        if (_audioFile.Position >= _audioFile.Length)
        {
            // Reached end
            Stop();
        }
    }

    private void ChangeState(PlaybackState newState)
    {
        if (_state == newState) return;
        var oldState = _state;
        _state = newState;
        StateChanged?.Invoke(this, new PlaybackStateChangedEventArgs(oldState, newState));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        
        _positionTimer?.Dispose();
        _waveOut?.Stop();
        _waveOut?.Dispose();
        _audioFile?.Dispose();
    }
}