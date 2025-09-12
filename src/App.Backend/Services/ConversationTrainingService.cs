using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Lazarus.Shared;
using Lazarus.Shared.Models.Training;

namespace Lazarus.Backend.Services
{
    public interface IConversationTrainingService
    {
        event EventHandler<TrainingProgressEventArgs>? ProgressChanged;

        Task<ConversationTrainingJob> CreateJobAsync(TrainingConfiguration config, CancellationToken ct = default);
        Task StartTrainingAsync(Guid jobId, CancellationToken ct = default);
        Task PauseTrainingAsync(Guid jobId, CancellationToken ct = default);
        Task StopTrainingAsync(Guid jobId, CancellationToken ct = default);
        Task<string> ExportToJsonlAsync(Guid jobId, CancellationToken ct = default);
        Task ImportFromJsonlAsync(string filePath, CancellationToken ct = default);
        ConversationTrainingJob? GetJob(Guid jobId);
    }

    public sealed class TrainingProgressEventArgs : EventArgs
    {
        public Guid JobId { get; init; }
        public double Progress { get; init; }
        public int? Epoch { get; init; }
        public long? Step { get; init; }
        public string? Message { get; init; }
    }

    public sealed class ConversationTrainingJob
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public TrainingConfiguration Config { get; init; } = new();
        public TrainingStatus Status { get; set; } = TrainingStatus.Created;
        public DateTime CreatedUtc { get; init; } = DateTime.UtcNow;
        public DateTime ModifiedUtc { get; set; } = DateTime.UtcNow;
        public string OutputDir { get; set; } = string.Empty;
        internal CancellationTokenSource? Cts { get; set; }
        internal bool IsPaused { get; set; }
    }

    /// <summary>
    /// Minimal in-process trainer stub that simulates progress and reads/writes JSONL.
    /// Uses LazarusPaths for file locations. Replace with real trainer when available.
    /// </summary>
    public sealed class ConversationTrainingService : IConversationTrainingService
    {
        private readonly ConcurrentDictionary<Guid, ConversationTrainingJob> _jobs = new();

        public event EventHandler<TrainingProgressEventArgs>? ProgressChanged;

        public ConversationTrainingJob? GetJob(Guid jobId) => _jobs.TryGetValue(jobId, out var j) ? j : null;

        public Task<ConversationTrainingJob> CreateJobAsync(TrainingConfiguration config, CancellationToken ct = default)
        {
            if (config is null) throw new ArgumentNullException(nameof(config));

            var outputsRoot = config.Type == TrainingType.LoRA || config.Type == TrainingType.QLoRA
                ? LazarusPaths.Models.LoRAAdapters
                : LazarusPaths.Models.BaseModels;
            Directory.CreateDirectory(outputsRoot);

            var job = new ConversationTrainingJob
            {
                Config = config,
                OutputDir = Path.Combine(outputsRoot, SanitizePathSegment(config.BaseModel), DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"))
            };

            _jobs[job.Id] = job;
            return Task.FromResult(job);
        }

        public async Task StartTrainingAsync(Guid jobId, CancellationToken ct = default)
        {
            var job = RequireJob(jobId);
            if (job.Status == TrainingStatus.Running) return;
            job.Cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            job.Status = TrainingStatus.Running;
            job.ModifiedUtc = DateTime.UtcNow;

            Directory.CreateDirectory(job.OutputDir);
            var logsDir = Path.Combine(LazarusPaths.SystemData.Logs, "training");
            Directory.CreateDirectory(logsDir);
            var logPath = Path.Combine(logsDir, $"{job.Id}.log");

            // Simulate a training loop
            var steps = job.Config.Duration == TrainingDuration.Steps ? Math.Max(1, job.Config.Steps) : Math.Max(1, job.Config.Epochs * 100);
            for (var i = 1; i <= steps; i++)
            {
                job.Cts.Token.ThrowIfCancellationRequested();
                while (job.IsPaused)
                {
                    await Task.Delay(200, job.Cts.Token);
                }

                await File.AppendAllTextAsync(logPath, $"{DateTime.UtcNow:o} step={i}/{steps} lr={job.Config.LearningRate}\n");
                var progress = (double)i / steps;
                ProgressChanged?.Invoke(this, new TrainingProgressEventArgs
                {
                    JobId = job.Id,
                    Progress = progress,
                    Step = i,
                    Message = "training"
                });

                await Task.Delay(100, job.Cts.Token);
            }

            job.Status = TrainingStatus.Completed;
            job.ModifiedUtc = DateTime.UtcNow;
        }

        public Task PauseTrainingAsync(Guid jobId, CancellationToken ct = default)
        {
            var job = RequireJob(jobId);
            if (job.Status != TrainingStatus.Running) return Task.CompletedTask;
            job.IsPaused = true;
            job.Status = TrainingStatus.Paused;
            job.ModifiedUtc = DateTime.UtcNow;
            return Task.CompletedTask;
        }

        public Task StopTrainingAsync(Guid jobId, CancellationToken ct = default)
        {
            var job = RequireJob(jobId);
            job.Cts?.Cancel();
            job.Status = TrainingStatus.Stopped;
            job.ModifiedUtc = DateTime.UtcNow;
            return Task.CompletedTask;
        }

        public async Task<string> ExportToJsonlAsync(Guid jobId, CancellationToken ct = default)
        {
            var job = RequireJob(jobId);
            var exportRoot = Path.Combine(LazarusPaths.SharedResources.ImportExport, "training-exports");
            Directory.CreateDirectory(exportRoot);
            var target = Path.Combine(exportRoot, $"{job.Id}.jsonl");

            await using var fs = new FileStream(target, FileMode.Create, FileAccess.Write, FileShare.Read);
            await using var writer = new StreamWriter(fs, Encoding.UTF8);

            // Minimal placeholder JSONL entry
            var obj = new
            {
                messages = new object[]
                {
                    new { role = "system", content = "You are a helpful assistant." },
                    new { role = "user", content = "Hello" },
                    new { role = "assistant", content = "Hi!" }
                }
            };
            var line = JsonSerializer.Serialize(obj);
            await writer.WriteLineAsync(line);
            await writer.FlushAsync();
            return target;
        }

        public async Task ImportFromJsonlAsync(string filePath, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("Invalid path", nameof(filePath));
            if (!File.Exists(filePath)) throw new FileNotFoundException("JSONL file not found", filePath);

            // Basic validation: ensure each line parses with a messages array
            await foreach (var line in ReadLinesAsync(filePath, ct))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                try
                {
                    using var doc = JsonDocument.Parse(line);
                    _ = doc.RootElement.GetProperty("messages");
                }
                catch (Exception ex)
                {
                    throw new InvalidDataException($"Invalid JSONL entry: {ex.Message}");
                }
            }
        }

        private ConversationTrainingJob RequireJob(Guid id)
        {
            if (!_jobs.TryGetValue(id, out var job)) throw new KeyNotFoundException($"Job {id} not found");
            return job;
        }

        private static string SanitizePathSegment(string value)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                value = value.Replace(c, '_');
            }
            return value;
        }

        private static async IAsyncEnumerable<string> ReadLinesAsync(string path, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(fs, Encoding.UTF8);
            while (!reader.EndOfStream)
            {
                ct.ThrowIfCancellationRequested();
                var line = await reader.ReadLineAsync();
                if (line is not null) yield return line;
            }
        }
    }
}


