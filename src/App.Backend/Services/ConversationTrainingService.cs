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
using Lazarus.Shared.Training;

namespace Lazarus.Backend.Services
{
    public interface IConversationTrainingService
    {
        // Datasets
        Task<string> ImportFromJsonlAsync(string path, DatasetKind kind = DatasetKind.Conversations, CancellationToken ct = default);

        // Jobs
        Task<Lazarus.Shared.Contracts.TrainingJob> CreateJobAsync(Lazarus.Shared.Training.TrainingProfile profile, CancellationToken ct = default);
        Task StartTrainingAsync(Guid jobId, CancellationToken ct = default);
        Task PauseTrainingAsync(Guid jobId, CancellationToken ct = default);
        Task StopTrainingAsync(Guid jobId, CancellationToken ct = default);
        Task<string> ExportArtifactsAsync(Guid jobId, CancellationToken ct = default);

        event EventHandler<TrainingProgressEventArgs>? ProgressChanged;
        event EventHandler<TrainingStateChangedEventArgs>? StateChanged;

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
        public event EventHandler<TrainingStateChangedEventArgs>? StateChanged;

        public ConversationTrainingJob? GetJob(Guid jobId) => _jobs.TryGetValue(jobId, out var j) ? j : null;

        public Task<Lazarus.Shared.Contracts.TrainingJob> CreateJobAsync(Lazarus.Shared.Training.TrainingProfile profile, CancellationToken ct = default)
        {
            if (profile is null) throw new ArgumentNullException(nameof(profile));

            // Choose outputs root based on adapter usage
            var outputsRoot = profile.UseLoRA
                ? LazarusPaths.SystemData.Training.Outputs_Adapters
                : LazarusPaths.Models.BaseModels;
            Directory.CreateDirectory(outputsRoot);

            var job = new ConversationTrainingJob
            {
                Config = new TrainingConfiguration
                {
                    BaseModel = profile.BaseModel,
                    Type = profile.UseQLoRA ? TrainingType.QLoRA : profile.UseLoRA ? TrainingType.LoRA : TrainingType.FineTuning,
                    LearningRate = profile.LearningRate,
                    BatchSize = profile.PerDeviceBatch,
                    GradientAccumulation = profile.GradAccum,
                    MaxSequenceLength = profile.MaxSeqLen,
                    ChatTemplate = profile.ChatTemplate,
                    Duration = profile.Epochs.HasValue ? TrainingDuration.Epochs : TrainingDuration.Steps,
                    Steps = profile.MaxSteps ?? 0,
                    Epochs = profile.Epochs ?? 0
                },
                OutputDir = Path.Combine(outputsRoot, SanitizePathSegment(profile.BaseModel), DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"))
            };

            _jobs[job.Id] = job;

            var uiJob = new Lazarus.Shared.Contracts.TrainingJob
            {
                Id = job.Id.ToString(),
                Name = $"Conversations: {profile.BaseModel} {profile.Task}",
                Modality = Lazarus.Shared.Contracts.TrainingModality.Conversations,
                Status = Lazarus.Shared.Contracts.TrainingStatus.Draft,
                OutputPath = job.OutputDir,
                ConfigId = "uncommitted",
                ResourcesId = "uncommitted"
            };

            return Task.FromResult(uiJob);
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
                var progressArgs = new TrainingProgressEventArgs
                {
                    JobId = job.Id,
                    Progress = progress,
                    Step = i,
                    Message = "training"
                };
                ProgressChanged?.Invoke(this, progressArgs);
                
                await Task.Delay(100, job.Cts.Token);
            }

            job.Status = TrainingStatus.Completed;
            job.ModifiedUtc = DateTime.UtcNow;
            StateChanged?.Invoke(this, new TrainingStateChangedEventArgs { JobId = job.Id, Status = job.Status.ToString() });
        }

        public Task PauseTrainingAsync(Guid jobId, CancellationToken ct = default)
        {
            var job = RequireJob(jobId);
            if (job.Status != TrainingStatus.Running) return Task.CompletedTask;
            job.IsPaused = true;
            job.Status = TrainingStatus.Paused;
            job.ModifiedUtc = DateTime.UtcNow;
            StateChanged?.Invoke(this, new TrainingStateChangedEventArgs { JobId = job.Id, Status = job.Status.ToString() });
            return Task.CompletedTask;
        }

        public Task StopTrainingAsync(Guid jobId, CancellationToken ct = default)
        {
            var job = RequireJob(jobId);
            job.Cts?.Cancel();
            job.Status = TrainingStatus.Stopped;
            job.ModifiedUtc = DateTime.UtcNow;
            StateChanged?.Invoke(this, new TrainingStateChangedEventArgs { JobId = job.Id, Status = job.Status.ToString() });
            return Task.CompletedTask;
        }

        public Task<string> ExportArtifactsAsync(Guid jobId, CancellationToken ct = default)
        {
            var job = RequireJob(jobId);
            Directory.CreateDirectory(job.OutputDir);
            return Task.FromResult(job.OutputDir);
        }

        public async Task<string> ImportFromJsonlAsync(string path, DatasetKind kind = DatasetKind.Conversations, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Invalid path", nameof(path));
            if (!File.Exists(path)) throw new FileNotFoundException("JSONL file not found", path);

            // Basic validation: ensure each line parses with a messages array (for conversations/preferences)
            await foreach (var line in ReadLinesAsync(path, ct))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                try
                {
                    using var doc = JsonDocument.Parse(line);
                    _ = doc.RootElement.TryGetProperty("messages", out _);
                }
                catch (Exception ex)
                {
                    throw new InvalidDataException($"Invalid JSONL entry: {ex.Message}");
                }
            }

            // Normalize into System-Data/Training/Datasets
            var destRoot = kind switch
            {
                DatasetKind.Conversations => LazarusPaths.SystemData.Training.Datasets_Conversations,
                DatasetKind.Preferences => LazarusPaths.SystemData.Training.Datasets_Preferences,
                DatasetKind.Eval => LazarusPaths.SystemData.Training.Datasets_Eval,
                _ => LazarusPaths.SystemData.Training.Datasets_Conversations
            };
            Directory.CreateDirectory(destRoot);
            var fileName = Path.GetFileName(path);
            var destPath = Path.Combine(destRoot, fileName);
            if (!File.Exists(destPath))
            {
                File.Copy(path, destPath, overwrite: false);
            }
            return destPath;
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

    public sealed class TrainingStateChangedEventArgs : EventArgs
    {
        public Guid JobId { get; init; }
        public string Status { get; init; } = string.Empty;
    }
}

