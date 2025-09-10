using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Lazarus.Shared;
using Lazarus.Shared.Contracts.Audio;
using Microsoft.Extensions.Logging;
using NAudio.Wave;

namespace Lazarus.Backend.Services.Audio;

/// <summary>
/// Audio service implementation with NAudio
/// </summary>
public sealed class AudioService : IAudioService
{
    private readonly ILogger<AudioService> _logger;
    private readonly string _audioDirectory;
    private readonly Dictionary<string, IPlaybackSession> _activeSessions = new();
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public AudioService(ILogger<AudioService> logger)
    {
        _logger = logger;
        _audioDirectory = Path.Combine(LazarusPaths.SharedResources.ImportExport, "Audio");
        Directory.CreateDirectory(_audioDirectory);
    }

    public async Task<IReadOnlyList<AudioItem>> ListAsync(CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            var items = new List<AudioItem>();
            if (!Directory.Exists(_audioDirectory)) return items;

            var extensions = new[] { ".mp3", ".wav", ".flac", ".m4a", ".ogg", ".wma", ".aac" };
            var files = Directory.GetFiles(_audioDirectory)
                .Where(f => extensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .ToList();

            foreach (var file in files)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    var info = new FileInfo(file);
                    var item = new AudioItem
                    {
                        Id = Path.GetFileNameWithoutExtension(file),
                        FileName = Path.GetFileName(file),
                        FilePath = file,
                        FileSize = info.Length,
                        CreatedUtc = info.CreationTimeUtc,
                        ModifiedUtc = info.LastWriteTimeUtc,
                        Format = Path.GetExtension(file).TrimStart('.').ToUpperInvariant()
                    };

                    // Check for cached analysis
                    var analysisPath = GetAnalysisPath(file);
                    if (File.Exists(analysisPath))
                    {
                        try
                        {
                            var analysisJson = File.ReadAllText(analysisPath);
                            var analysis = JsonSerializer.Deserialize<AudioAnalysis>(analysisJson, _jsonOptions);
                            if (analysis != null)
                            {
                                item.Duration = analysis.Duration;
                                item.SampleRate = analysis.SampleRate;
                                item.Channels = analysis.Channels;
                                item.Bitrate = analysis.Bitrate;
                                item.HasAnalysis = true;
                                item.AnalyzedUtc = analysis.AnalyzedUtc;
                                item.FileHash = analysis.FileHash;
                            }
                        }
                        catch { }
                    }
                    
                    // Quick metadata if no analysis
                    if (!item.HasAnalysis)
                    {
                        try
                        {
                            using var reader = new AudioFileReader(file);
                            item.Duration = reader.TotalTime;
                            item.SampleRate = reader.WaveFormat.SampleRate;
                            item.Channels = reader.WaveFormat.Channels;
                            item.Bitrate = reader.WaveFormat.AverageBytesPerSecond * 8;
                        }
                        catch { }
                    }

                    items.Add(item);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to process audio file: {File}", file);
                }
            }

            return items.OrderBy(i => i.FileName).ToList();
        }, ct);
    }

    public async Task<AudioItem?> ImportAsync(string sourcePath, CancellationToken ct = default)
    {
        if (!File.Exists(sourcePath))
        {
            _logger.LogWarning("Source file not found: {Path}", sourcePath);
            return null;
        }

        return await Task.Run(async () =>
        {
            try
            {
                // Calculate hash for deduplication
                var hash = await ComputeFileHashAsync(sourcePath, ct);
                
                // Check for existing file with same hash
                var existing = await ListAsync(ct);
                var duplicate = existing.FirstOrDefault(i => i.FileHash == hash);
                if (duplicate != null)
                {
                    _logger.LogInformation("File already exists with hash {Hash}: {File}", hash, duplicate.FileName);
                    return duplicate;
                }

                // Copy to import directory
                var fileName = Path.GetFileName(sourcePath);
                var destPath = Path.Combine(_audioDirectory, fileName);
                
                // Handle name conflicts
                if (File.Exists(destPath))
                {
                    var nameOnly = Path.GetFileNameWithoutExtension(fileName);
                    var ext = Path.GetExtension(fileName);
                    var counter = 1;
                    do
                    {
                        fileName = $"{nameOnly} ({counter++}){ext}";
                        destPath = Path.Combine(_audioDirectory, fileName);
                    } while (File.Exists(destPath));
                }

                File.Copy(sourcePath, destPath, false);
                _logger.LogInformation("Imported audio file: {Source} -> {Dest}", sourcePath, destPath);

                // Create item
                var info = new FileInfo(destPath);
                var item = new AudioItem
                {
                    Id = Path.GetFileNameWithoutExtension(destPath),
                    FileName = fileName,
                    FilePath = destPath,
                    FileSize = info.Length,
                    FileHash = hash,
                    CreatedUtc = info.CreationTimeUtc,
                    ModifiedUtc = info.LastWriteTimeUtc,
                    Format = Path.GetExtension(destPath).TrimStart('.').ToUpperInvariant()
                };

                // Quick metadata
                try
                {
                    using var reader = new AudioFileReader(destPath);
                    item.Duration = reader.TotalTime;
                    item.SampleRate = reader.WaveFormat.SampleRate;
                    item.Channels = reader.WaveFormat.Channels;
                    item.Bitrate = reader.WaveFormat.AverageBytesPerSecond * 8;
                }
                catch { }

                return item;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to import audio file: {Path}", sourcePath);
                return null;
            }
        }, ct);
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        // Stop playback if active
        if (_activeSessions.TryGetValue(id, out var session))
        {
            session.Dispose();
            _activeSessions.Remove(id);
        }

        await Task.Run(() =>
        {
            var files = Directory.GetFiles(_audioDirectory, $"{id}.*");
            foreach (var file in files)
            {
                try
                {
                    File.Delete(file);
                    _logger.LogInformation("Deleted file: {File}", file);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete file: {File}", file);
                }
            }
        }, ct);
    }

    [SupportedOSPlatform("windows")]
    public async Task<AudioAnalysis> AnalyzeAsync(string id, bool force, CancellationToken ct = default)
    {
        var files = Directory.GetFiles(_audioDirectory, $"{id}.*");
        var audioFile = files.FirstOrDefault(f => !f.EndsWith(".json", StringComparison.OrdinalIgnoreCase));
        
        if (string.IsNullOrEmpty(audioFile) || !File.Exists(audioFile))
            throw new FileNotFoundException($"Audio file not found for ID: {id}");

        var analysisPath = GetAnalysisPath(audioFile);
        
        // Load cached if exists and not forced
        if (!force && File.Exists(analysisPath))
        {
            try
            {
                var json = await File.ReadAllTextAsync(analysisPath, ct);
                var cached = JsonSerializer.Deserialize<AudioAnalysis>(json, _jsonOptions);
                if (cached != null)
                {
                    _logger.LogInformation("Loaded cached analysis for {Id}", id);
                    return cached;
                }
            }
            catch { }
        }

        return await Task.Run(async () =>
        {
            _logger.LogInformation("Analyzing audio file: {File}", audioFile);
            
            var analysis = new AudioAnalysis
            {
                Id = id,
                AnalyzedUtc = DateTime.UtcNow
            };

            // Compute file hash
            analysis.FileHash = await ComputeFileHashAsync(audioFile, ct);

            // Read audio data and metadata
            using (var reader = new AudioFileReader(audioFile))
            {
                analysis.Duration = reader.TotalTime;
                analysis.SampleRate = reader.WaveFormat.SampleRate;
                analysis.Channels = reader.WaveFormat.Channels;
                analysis.Bitrate = reader.WaveFormat.AverageBytesPerSecond * 8;
                analysis.TotalSamples = reader.Length / (reader.WaveFormat.BitsPerSample / 8);
                
                // Read samples for waveform
                var sampleProvider = reader.ToSampleProvider();
                var samples = new List<float>();
                var buffer = new float[1024];
                int read;
                
                while ((read = sampleProvider.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ct.ThrowIfCancellationRequested();
                    for (int i = 0; i < read; i++)
                        samples.Add(buffer[i]);
                }
                
                var sampleArray = samples.ToArray();
                
                // Generate waveforms
                analysis.WaveformSmall = WaveformGenerator.GenerateSmall(sampleArray);
                analysis.WaveformLarge = WaveformGenerator.GenerateLarge(sampleArray);
            }

            // Save analysis
            var analysisJson = JsonSerializer.Serialize(analysis, _jsonOptions);
            await File.WriteAllTextAsync(analysisPath, analysisJson, ct);
            
            _logger.LogInformation("Analysis complete for {Id}", id);
            return analysis;
        }, ct);
    }

    public IPlaybackSession Play(string id)
    {
        // Stop any existing session for this ID
        if (_activeSessions.TryGetValue(id, out var existing))
        {
            existing.Dispose();
            _activeSessions.Remove(id);
        }

        var files = Directory.GetFiles(_audioDirectory, $"{id}.*");
        var audioFile = files.FirstOrDefault(f => !f.EndsWith(".json", StringComparison.OrdinalIgnoreCase));
        
        if (string.IsNullOrEmpty(audioFile) || !File.Exists(audioFile))
            throw new FileNotFoundException($"Audio file not found for ID: {id}");

        var session = new PlaybackSession(id, audioFile);
        _activeSessions[id] = session;
        
        // Auto-remove when stopped
        session.StateChanged += (s, e) =>
        {
            if (e.NewState == PlaybackState.Stopped)
            {
                _activeSessions.Remove(id);
            }
        };
        
        return session;
    }

    private string GetAnalysisPath(string audioFile)
    {
        return Path.ChangeExtension(audioFile, ".analysis.json");
    }

    private async Task<string> ComputeFileHashAsync(string filePath, CancellationToken ct)
    {
        using var sha256 = SHA256.Create();
        using var stream = File.OpenRead(filePath);
        var hash = await sha256.ComputeHashAsync(stream, ct);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }
}