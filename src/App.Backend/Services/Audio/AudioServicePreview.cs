using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Lazarus.Shared.Contracts.Audio;
using Lazarus.Shared.Utilities.Imaging;
using Timer = System.Timers.Timer;

namespace Lazarus.Backend.Services.Audio;

[SupportedOSPlatform("windows")]
public sealed class AudioServicePreview : IAudioService
{
    private readonly List<AudioItem> _previewItems = new();
    private readonly Dictionary<string, AudioAnalysis> _analyses = new();
    private readonly Dictionary<string, float[]> _waveformCache = new();
    
    public AudioServicePreview()
    {
        GeneratePreviewItems();
    }
    
    private void GeneratePreviewItems()
    {
        var random = new Random();
        var fileNames = new[]
        {
            "Morning Coffee Jazz.mp3",
            "Podcast Episode 42 - Tech Trends.wav",
            "Meeting Recording 2024-01-15.m4a",
            "Symphony No.9 in D Minor.flac",
            "Guitar Practice Session.mp3",
            "Ambient Rain Sounds.wav",
            "Voice Memo - Project Ideas.m4a",
            "Electronic Mix Vol.3.mp3"
        };
        
        for (int i = 0; i < fileNames.Length; i++)
        {
            var id = Guid.NewGuid().ToString();
            var duration = TimeSpan.FromSeconds(random.Next(30, 600));
            var sizeBytes = random.Next(1_000_000, 50_000_000);
            
            var item = new AudioItem
            {
                Id = id,
                FileName = fileNames[i],
                FilePath = $@"C:\Audio\Preview\{fileNames[i]}",
                FileSize = sizeBytes,
                FileHash = GenerateHash(),
                CreatedUtc = DateTime.UtcNow.AddDays(-random.Next(1, 30)),
                ModifiedUtc = DateTime.UtcNow.AddDays(-random.Next(0, 7)),
                Duration = duration,
                SampleRate = random.Next(0, 2) == 0 ? 44100 : 48000,
                Channels = random.Next(1, 3),
                Bitrate = random.Next(128000, 320000),
                Format = System.IO.Path.GetExtension(fileNames[i]).TrimStart('.').ToUpperInvariant(),
                HasAnalysis = true,
                AnalyzedUtc = DateTime.UtcNow
            };
            
            _previewItems.Add(item);
            
            // Generate waveform for this item
            var samples = GeneratePreviewWaveform(i);
            _waveformCache[id] = samples;
            
            // Create analysis
            var analysis = new AudioAnalysis
            {
                Id = id,
                FileHash = item.FileHash,
                Duration = duration,
                Bitrate = item.Bitrate,
                SampleRate = item.SampleRate,
                Channels = item.Channels,
                WaveformSmall = WaveformPng.GenerateSmallWaveform(samples),
                WaveformLarge = WaveformPng.GenerateLargeWaveform(samples),
                HasEmbedding = false,
                AnalyzedUtc = DateTime.UtcNow,
                CodecName = item.Format,
                TotalSamples = (long)(duration.TotalSeconds * item.SampleRate)
            };
            
            _analyses[id] = analysis;
        }
    }
    
    private float[] GeneratePreviewWaveform(int seed)
    {
        var random = new Random(seed);
        int sampleCount = 44100 * 5; // 5 seconds worth
        
        // Mix different frequencies
        var freq1 = 200f + random.Next(0, 500);
        var freq2 = 400f + random.Next(0, 800);
        var freq3 = 800f + random.Next(0, 1200);
        
        var sine1 = WaveformPng.GenerateSineWave(sampleCount, freq1, 0.3f);
        var sine2 = WaveformPng.GenerateSineWave(sampleCount, freq2, 0.2f);
        var sine3 = WaveformPng.GenerateSineWave(sampleCount, freq3, 0.1f);
        var noise = WaveformPng.GenerateNoise(sampleCount, 0.05f);
        
        var mixed = WaveformPng.MixSignals(sine1, sine2, sine3, noise);
        
        // Apply random envelope
        float attack = 0.05f + (float)random.NextDouble() * 0.15f;
        float decay = 0.05f + (float)random.NextDouble() * 0.1f;
        float sustain = 0.6f + (float)random.NextDouble() * 0.3f;
        float release = 0.1f + (float)random.NextDouble() * 0.2f;
        
        return WaveformPng.ApplyEnvelope(mixed, attack, decay, sustain, release);
    }
    
    private string GenerateHash()
    {
        var bytes = new byte[32];
        new Random().NextBytes(bytes);
        return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
    }
    
    public Task<IReadOnlyList<AudioItem>> ListAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<AudioItem>>(_previewItems.AsReadOnly());
    }
    
    public Task<AudioItem?> ImportAsync(string sourcePath, CancellationToken cancellationToken = default)
    {
        // In preview mode, create a fake import
        var fileName = System.IO.Path.GetFileName(sourcePath);
        var id = Guid.NewGuid().ToString();
        
        var item = new AudioItem
        {
            Id = id,
            FileName = fileName,
            FilePath = sourcePath,
            FileSize = new Random().Next(1_000_000, 10_000_000),
            FileHash = GenerateHash(),
            CreatedUtc = DateTime.UtcNow,
            ModifiedUtc = DateTime.UtcNow,
            Duration = TimeSpan.FromSeconds(new Random().Next(60, 300)),
            SampleRate = 44100,
            Channels = 2,
            Bitrate = 192000,
            Format = System.IO.Path.GetExtension(fileName).TrimStart('.').ToUpperInvariant(),
            HasAnalysis = false
        };
        
        _previewItems.Add(item);
        return Task.FromResult<AudioItem?>(item);
    }
    
    public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        _previewItems.RemoveAll(x => x.Id == id);
        _analyses.Remove(id);
        _waveformCache.Remove(id);
        return Task.CompletedTask;
    }
    
    public Task<AudioAnalysis> AnalyzeAsync(string id, bool force, CancellationToken cancellationToken = default)
    {
        if (!force && _analyses.TryGetValue(id, out var existing))
            return Task.FromResult(existing);
        
        var item = _previewItems.FirstOrDefault(x => x.Id == id);
        if (item == null)
            throw new InvalidOperationException($"Audio item {id} not found");
        
        // Generate new analysis
        var samples = GeneratePreviewWaveform(id.GetHashCode());
        _waveformCache[id] = samples;
        
        var analysis = new AudioAnalysis
        {
            Id = id,
            FileHash = item.FileHash,
            Duration = item.Duration,
            Bitrate = item.Bitrate,
            SampleRate = item.SampleRate,
            Channels = item.Channels,
            WaveformSmall = WaveformPng.GenerateSmallWaveform(samples),
            WaveformLarge = WaveformPng.GenerateLargeWaveform(samples),
            HasEmbedding = false,
            AnalyzedUtc = DateTime.UtcNow,
            CodecName = item.Format,
            TotalSamples = (long)(item.Duration.TotalSeconds * item.SampleRate)
        };
        
        _analyses[id] = analysis;
        item.HasAnalysis = true;
        item.AnalyzedUtc = DateTime.UtcNow;
        
        return Task.FromResult(analysis);
    }
    
    public IPlaybackSession Play(string id)
    {
        var item = _previewItems.FirstOrDefault(x => x.Id == id);
        if (item == null)
            throw new InvalidOperationException($"Audio item {id} not found");
        
        return new PreviewPlaybackSession(item);
    }
    
    private sealed class PreviewPlaybackSession : IPlaybackSession, INotifyPropertyChanged
    {
        private readonly AudioItem _item;
        private readonly Timer _timer;
        private TimeSpan _position;
        private PlaybackState _state;
        private float _volume = 1.0f;
        private bool _disposed;
        
        public PreviewPlaybackSession(AudioItem item)
        {
            _item = item;
            AudioId = item.Id;
            _timer = new Timer(100); // Update every 100ms
            _timer.Elapsed += OnTimerElapsed;
            _state = PlaybackState.Playing;
            _timer.Start();
        }
        
        public string AudioId { get; }
        
        public TimeSpan Position
        {
            get => _position;
            set
            {
                if (_position != value)
                {
                    _position = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Position)));
                    PositionChanged?.Invoke(this, _position);
                }
            }
        }
        
        public TimeSpan Duration => _item.Duration;
        
        public PlaybackState State
        {
            get => _state;
            private set
            {
                if (_state != value)
                {
                    var oldState = _state;
                    _state = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(State)));
                    StateChanged?.Invoke(this, new PlaybackStateChangedEventArgs(oldState, _state));
                }
            }
        }
        
        public float Volume
        {
            get => _volume;
            set
            {
                _volume = Math.Max(0, Math.Min(1, value));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Volume)));
            }
        }
        
        public event EventHandler<PlaybackStateChangedEventArgs>? StateChanged;
        public event EventHandler<TimeSpan>? PositionChanged;
        public event PropertyChangedEventHandler? PropertyChanged;
        
        private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            if (_state == PlaybackState.Playing)
            {
                Position = Position.Add(TimeSpan.FromMilliseconds(100));
                if (Position >= Duration)
                {
                    Position = Duration;
                    Stop();
                }
            }
        }
        
        public void Pause()
        {
            State = PlaybackState.Paused;
            _timer.Stop();
        }
        
        public void Resume()
        {
            State = PlaybackState.Playing;
            _timer.Start();
        }
        
        public void Stop()
        {
            State = PlaybackState.Stopped;
            _timer.Stop();
            Position = TimeSpan.Zero;
        }
        
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _timer?.Stop();
                _timer?.Dispose();
            }
        }
    }
}