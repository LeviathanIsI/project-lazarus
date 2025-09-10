using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Lazarus.Desktop.ViewModels;
using Lazarus.Shared;
using Microsoft.Extensions.Logging;
using NAudio.Wave;

namespace Lazarus.Desktop.Services;

/// <summary>
/// Implementation of audio service using NAudio
/// </summary>
public sealed class AudioService : IAudioService
{
    private readonly ILogger<AudioService> _logger;
    private readonly string _audioDirectory;
    private WaveOutEvent? _waveOut;
    private AudioFileReader? _audioFile;
    private WaveInEvent? _waveIn;
    private WaveFileWriter? _recordingWriter;
#pragma warning disable CS0414
    private string? _recordingPath = null;
#pragma warning restore CS0414

    public AudioService(ILogger<AudioService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Use LazarusPaths to get the audio directory
        _audioDirectory = Path.Combine(
            LazarusPaths.SharedResources.ImportExport,
            "Audio"
        );
        
        // Ensure directory exists
        Directory.CreateDirectory(_audioDirectory);
        
        _logger.LogInformation("AudioService initialized with directory: {Directory}", _audioDirectory);
    }

    public async Task<IReadOnlyList<AudioItem>> GetAudioFilesAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                var audioFiles = new List<AudioItem>();
                
                if (!Directory.Exists(_audioDirectory))
                {
                    Directory.CreateDirectory(_audioDirectory);
                    return audioFiles;
                }
                
                var supportedExtensions = new[] { ".mp3", ".wav", ".flac", ".ogg", ".m4a", ".wma", ".aac" };
                var files = Directory.GetFiles(_audioDirectory)
                    .Where(f => supportedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                    .ToList();
                
                foreach (var file in files)
                {
                    try
                    {
                        var fileInfo = new FileInfo(file);
                        var item = new AudioItem
                        {
                            Id = Guid.NewGuid(),
                            FileName = Path.GetFileName(file),
                            FilePath = file,
                            FileSize = fileInfo.Length,
                            Format = Path.GetExtension(file).TrimStart('.').ToUpperInvariant(),
                            LastModified = fileInfo.LastWriteTime
                        };
                        
                        // Try to get audio metadata
                        try
                        {
                            using var reader = new AudioFileReader(file);
                            item.Duration = reader.TotalTime;
                            item.SampleRate = reader.WaveFormat.SampleRate;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Could not read audio metadata for {File}", file);
                            item.Duration = TimeSpan.Zero;
                            item.SampleRate = 44100; // Default
                        }
                        
                        audioFiles.Add(item);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to process audio file: {File}", file);
                    }
                }
                
                return audioFiles.OrderBy(f => f.FileName).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get audio files");
                return new List<AudioItem>();
            }
        });
    }

    public async Task ImportFileAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be empty", nameof(filePath));
        
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Source file not found", filePath);
        
        await Task.Run(() =>
        {
            try
            {
                var fileName = Path.GetFileName(filePath);
                var destPath = Path.Combine(_audioDirectory, fileName);
                
                // Handle duplicate names
                if (File.Exists(destPath))
                {
                    var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                    var ext = Path.GetExtension(fileName);
                    var counter = 1;
                    
                    do
                    {
                        fileName = $"{nameWithoutExt} ({counter}){ext}";
                        destPath = Path.Combine(_audioDirectory, fileName);
                        counter++;
                    } while (File.Exists(destPath));
                }
                
                File.Copy(filePath, destPath, false);
                _logger.LogInformation("Imported audio file: {Source} -> {Dest}", filePath, destPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to import audio file: {File}", filePath);
                throw;
            }
        });
    }

    public Task<string> StartRecordingAsync()
    {
        // Recording stub - would need microphone permissions and UI
        throw new NotImplementedException("Audio recording is not yet implemented");
        
        // Full implementation would be:
        /*
        if (_waveIn != null)
            throw new InvalidOperationException("Recording already in progress");
        
        _recordingPath = Path.Combine(_audioDirectory, $"Recording_{DateTime.Now:yyyyMMdd_HHmmss}.wav");
        
        _waveIn = new WaveInEvent
        {
            WaveFormat = new WaveFormat(44100, 16, 2) // 44.1kHz, 16-bit, stereo
        };
        
        _recordingWriter = new WaveFileWriter(_recordingPath, _waveIn.WaveFormat);
        
        _waveIn.DataAvailable += (s, e) =>
        {
            _recordingWriter?.Write(e.Buffer, 0, e.BytesRecorded);
        };
        
        _waveIn.StartRecording();
        _logger.LogInformation("Started recording to: {Path}", _recordingPath);
        
        return Task.FromResult(_recordingPath);
        */
    }

    public Task StopRecordingAsync()
    {
        // Recording stub
        throw new NotImplementedException("Audio recording is not yet implemented");
        
        // Full implementation would be:
        /*
        if (_waveIn == null)
            throw new InvalidOperationException("No recording in progress");
        
        _waveIn.StopRecording();
        _waveIn.Dispose();
        _waveIn = null;
        
        _recordingWriter?.Dispose();
        _recordingWriter = null;
        
        var path = _recordingPath;
        _recordingPath = null;
        
        _logger.LogInformation("Stopped recording: {Path}", path);
        return Task.CompletedTask;
        */
    }

    public Task PlayAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be empty", nameof(filePath));
        
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Audio file not found", filePath);
        
        return Task.Run(() =>
        {
            try
            {
                // Clean up any existing playback
                Stop();
                
                _audioFile = new AudioFileReader(filePath);
                _waveOut = new WaveOutEvent();
                _waveOut.Init(_audioFile);
                _waveOut.Play();
                
                _logger.LogInformation("Playing audio file: {File}", filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to play audio file: {File}", filePath);
                throw;
            }
        });
    }

    public Task PauseAsync()
    {
        return Task.Run(() =>
        {
            try
            {
                if (_waveOut?.PlaybackState == PlaybackState.Playing)
                {
                    _waveOut.Pause();
                    _logger.LogInformation("Playback paused");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to pause playback");
                throw;
            }
        });
    }

    public void Stop()
    {
        try
        {
            if (_waveOut != null)
            {
                _waveOut.Stop();
                _waveOut.Dispose();
                _waveOut = null;
            }
            
            if (_audioFile != null)
            {
                _audioFile.Dispose();
                _audioFile = null;
            }
            
            _logger.LogInformation("Playback stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop playback");
        }
    }

    public TimeSpan? GetPlaybackPosition()
    {
        try
        {
            return _audioFile?.CurrentTime;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get playback position");
            return null;
        }
    }

    public async Task DeleteFileAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be empty", nameof(filePath));
        
        await Task.Run(() =>
        {
            try
            {
                // Stop playback if this file is playing
                if (_audioFile?.FileName == filePath)
                {
                    Stop();
                }
                
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    _logger.LogInformation("Deleted audio file: {File}", filePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete audio file: {File}", filePath);
                throw;
            }
        });
    }

    public void Dispose()
    {
        try
        {
            Stop();
            
            _waveIn?.StopRecording();
            _waveIn?.Dispose();
            _waveIn = null;
            
            _recordingWriter?.Dispose();
            _recordingWriter = null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during AudioService disposal");
        }
    }
}