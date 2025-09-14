using System;
using System.Collections.Concurrent;
using System.IO;
using System.Diagnostics;
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

            // Create a Jobs/<id> working directory with manifests
            Directory.CreateDirectory(LazarusPaths.SystemData.Training.JobsRoot);

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

            // Materialize working directory and simple manifests under System-Data/Training/Jobs
            var jobDir = Path.Combine(LazarusPaths.SystemData.Training.JobsRoot, job.Id.ToString());
            Directory.CreateDirectory(jobDir);
            var artifactsDir = Path.Combine(jobDir, "artifacts");
            Directory.CreateDirectory(artifactsDir);
            // Write profile.json for visibility
            try
            {
                var profilePath = Path.Combine(jobDir, "profile.json");
                var json = JsonSerializer.Serialize(profile, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(profilePath, json);
            }
            catch { /* non-fatal */ }

            var uiJob = new Lazarus.Shared.Contracts.TrainingJob
            {
                Id = job.Id.ToString(),
                Name = $"Conversations: {profile.BaseModel} {profile.Task}",
                Modality = Lazarus.Shared.Contracts.TrainingModality.Conversations,
                Status = Lazarus.Shared.Contracts.TrainingStatus.Draft,
                OutputPath = jobDir,
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

            // Prepare dataset workspace under Jobs/<id>
            try
            {
                var jobDir = Path.Combine(LazarusPaths.SystemData.Training.JobsRoot, job.Id.ToString());
                Directory.CreateDirectory(jobDir);
                var profilePath = Path.Combine(jobDir, "profile.json");
                if (File.Exists(profilePath))
                {
                    var profile = JsonSerializer.Deserialize<Lazarus.Shared.Training.TrainingProfile>(await File.ReadAllTextAsync(profilePath, ct));
                    if (profile != null)
                    {
                        // Convert to sharegpt JSONL if needed
                        var outFile = Path.Combine(jobDir, "train_converted.jsonl");
                        await ConvertDatasetsToShareGptAsync(profile.TrainFiles, outFile, ct);
                        // dataset_info.json (no BOM, UTF-8)
                        var datasetInfo = new
                        {
                            sharegpt = new { file_name = "train_converted.jsonl", formatting = "sharegpt" }
                        };
                        var dsInfoPath = Path.Combine(jobDir, "dataset_info.json");
                        var dsJson = JsonSerializer.Serialize(datasetInfo, new JsonSerializerOptions { WriteIndented = true });
                        await File.WriteAllTextAsync(dsInfoPath, dsJson, new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false), ct);

                        // Ensure wrapper exists under Jobs/<id>/bin/direct_train.py
                        var binDir = Path.Combine(jobDir, "bin");
                        Directory.CreateDirectory(binDir);
                        var wrapper = Path.Combine(binDir, "direct_train.py");
                        if (!File.Exists(wrapper))
                        {
                            var content = string.Join('\n', new[]
                            {
                                "import sys",
                                "for key in list(sys.modules.keys()):",
                                "    if 'llamafactory' in key:",
                                "        del sys.modules[key]",
                                "from llamafactory.train.tuner import run_exp",
                                "",
                                "if __name__ == '__main__':",
                                "    run_exp()"
                            });
                            await File.WriteAllTextAsync(wrapper, content, new System.Text.UTF8Encoding(false), ct);
                        }

                        if (profile.Trainer == Lazarus.Shared.Training.TrainerBackend.LLaMAFactory)
                        {
                            // Spawn LLaMAFactory training using direct_train.py with hydra overrides
                            _ = Task.Run(async () =>
                            {
                                try
                                {
                                    var lfRoot = Path.Combine(LazarusPaths.Root, "Trainers", "LLaMA-Factory");
                                    var (exe, baseArgs) = DetectPython();
                                    var overrides = BuildLfOverrides(profile, jobDir);
                                    var argList = new List<string>();
                                    if (!string.IsNullOrWhiteSpace(baseArgs)) argList.Add(baseArgs);
                                    argList.Add('"' + wrapper.Replace("\\", "/") + '"');
                                    argList.AddRange(overrides);

                                    var psi = new ProcessStartInfo(exe, string.Join(" ", argList))
                                    {
                                        WorkingDirectory = jobDir,
                                        CreateNoWindow = true,
                                        UseShellExecute = false,
                                        RedirectStandardOutput = true,
                                        RedirectStandardError = true
                                    };
                                    // Ensure Python can import llamafactory if it's a local repo under Trainers
                                    if (Directory.Exists(lfRoot))
                                    {
                                        var existing = Environment.GetEnvironmentVariable("PYTHONPATH") ?? string.Empty;
                                        psi.Environment["PYTHONPATH"] = string.IsNullOrEmpty(existing) ? lfRoot : existing + Path.PathSeparator + lfRoot;
                                    }
                                    // Favor forward slashes in env paths if consumed later
                                    psi.Environment["HF_HOME"] = LazarusPaths.Models.RootDir.Replace('\\', '/');

                                    using var proc = Process.Start(psi);
                                    if (proc != null)
                                    {
                                        await Task.WhenAll(
                                            PumpAsync(proc.StandardOutput, logPath, job.Cts.Token),
                                            PumpAsync(proc.StandardError, logPath, job.Cts.Token)
                                        );
                                        proc.WaitForExit();
                                        job.Status = proc.ExitCode == 0 ? TrainingStatus.Completed : TrainingStatus.Failed;
                                        job.ModifiedUtc = DateTime.UtcNow;
                                        StateChanged?.Invoke(this, new TrainingStateChangedEventArgs { JobId = job.Id, Status = job.Status.ToString() });
                                    }
                                }
                                catch (Exception runEx)
                                {
                                    await File.AppendAllTextAsync(logPath, $"{DateTime.UtcNow:o} launch error: {runEx.Message}\n");
                                    job.Status = TrainingStatus.Failed;
                                    job.ModifiedUtc = DateTime.UtcNow;
                                    StateChanged?.Invoke(this, new TrainingStateChangedEventArgs { JobId = job.Id, Status = job.Status.ToString() });
                                }
                            }, job.Cts.Token);

                            // Return early — process runs asynchronously; progress will be updated via logs (future enhancement)
                            return;
                        }
                    }
                }
            }
            catch (Exception prepEx)
            {
                await File.AppendAllTextAsync(logPath, $"{DateTime.UtcNow:o} dataset prep warning: {prepEx.Message}\n");
            }

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

        private static async Task ConvertDatasetsToShareGptAsync(string[] trainFiles, string outputJsonl, CancellationToken ct)
        {
            await using var fs = new FileStream(outputJsonl, FileMode.Create, FileAccess.Write, FileShare.Read);
            await using var writer = new StreamWriter(fs, new UTF8Encoding(false));
            foreach (var src in trainFiles ?? Array.Empty<string>())
            {
                if (string.IsNullOrWhiteSpace(src) || !File.Exists(src)) continue;
                await foreach (var line in ReadLinesAsync(src, ct))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    try
                    {
                        using var doc = JsonDocument.Parse(line);
                        if (doc.RootElement.TryGetProperty("messages", out var messages) && messages.ValueKind == JsonValueKind.Array)
                        {
                            var list = new List<Dictionary<string, string>>();
                            foreach (var m in messages.EnumerateArray())
                            {
                                var role = m.TryGetProperty("role", out var r) ? r.GetString() ?? "" : "";
                                var content = m.TryGetProperty("content", out var c) ? c.GetString() ?? "" : "";
                                var from = role == "system" ? "system" : role == "user" ? "human" : "gpt";
                                list.Add(new Dictionary<string, string> { ["from"] = from, ["value"] = content });
                            }
                            var obj = new Dictionary<string, object> { ["conversations"] = list };
                            var outLine = JsonSerializer.Serialize(obj);
                            await writer.WriteLineAsync(outLine);
                        }
                    }
                    catch
                    {
                        // skip malformed lines
                    }
                }
            }
            await writer.FlushAsync();
        }

        private static (string exe, string baseArgs) DetectPython()
        {
            try
            {
                // Prefer py -3.12 if available
                var ok = TryRun("py", "-3.12 --version", out var output);
                if (ok && output.Contains("3.12")) return ("py", "-3.12");
            }
            catch { }
            return ("python", "");
        }

        private static bool TryRun(string exe, string args, out string output)
        {
            try
            {
                var psi = new ProcessStartInfo(exe, args)
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                using var p = Process.Start(psi);
                if (p == null) { output = string.Empty; return false; }
                var stdout = p.StandardOutput.ReadToEnd();
                var stderr = p.StandardError.ReadToEnd();
                p.WaitForExit(1500);
                output = string.IsNullOrWhiteSpace(stdout) ? stderr : stdout;
                return p.ExitCode == 0 || !string.IsNullOrWhiteSpace(output);
            }
            catch { output = string.Empty; return false; }
        }

        private static IEnumerable<string> BuildLfOverrides(Lazarus.Shared.Training.TrainingProfile profile, string jobDir)
        {
            // Favor forward slashes for trainer compatibility
            string Fwd(string p) => p.Replace("\\", "/");

            var list = new List<string>
            {
                $"model_name_or_path={Fwd(profile.BaseModel)}",
                "datasets=[sharegpt]",
                "dataset_dir=.",
                $"template={profile.ChatTemplate.ToLowerInvariant()}",
                $"learning_rate={profile.LearningRate}",
                profile.Epochs.HasValue ? $"num_train_epochs={profile.Epochs.Value}" : $"max_steps={profile.MaxSteps ?? 0}",
                $"per_device_train_batch_size={profile.PerDeviceBatch}",
                $"gradient_accumulation_steps={profile.GradAccum}",
                $"cutoff_len={profile.MaxSeqLen}",
                $"lr_scheduler_type={profile.LrScheduler}",
                $"optim={profile.Optimizer}",
                $"eval_steps={profile.EvalEverySteps}",
                $"save_steps={profile.SaveEverySteps}",
                "evaluation_strategy=steps",
                $"output_dir={Fwd(Path.Combine(jobDir, "artifacts"))}"
            };

            var ft = profile.UseLoRA ? "lora" : "full";
            list.Add($"finetuning_type={ft}");
            if (profile.UseLoRA)
            {
                list.Add($"lora_rank={profile.LoRARank}");
                list.Add($"lora_alpha={profile.LoRAAlpha}");
                list.Add($"lora_dropout={profile.LoRADropout}");
            }
            list.Add("gradient_checkpointing=False");
            list.Add("use_flash_attn=False");
            list.Add("packing=False");
            return list;
        }

        private static async Task PumpAsync(StreamReader reader, string logPath, CancellationToken ct)
        {
            try
            {
                while (!reader.EndOfStream && !ct.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync();
                    if (line != null)
                    {
                        await File.AppendAllTextAsync(logPath, line + Environment.NewLine);
                    }
                }
            }
            catch { }
        }
    }

    public sealed class TrainingStateChangedEventArgs : EventArgs
    {
        public Guid JobId { get; init; }
        public string Status { get; init; } = string.Empty;
    }
}
