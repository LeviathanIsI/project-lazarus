using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Lazarus.Shared;
using Lazarus.Shared.Models.Training;

namespace Lazarus.Backend.Services.Training;

public interface ITrainingJobOrchestrator
{
    Guid CreateJobWorkspace(TrainingProfile profile, string trainer, out string jobDir);
    Task WriteManifestAsync(Guid jobId, TrainingProfile profile, string trainer, IEnumerable<string> trainFiles, IEnumerable<string> evalFiles, CancellationToken ct = default);
    Task WritePlanFilesAsync(Guid jobId, TrainerPlan plan, CancellationToken ct = default);
}

public sealed class TrainingJobOrchestrator : ITrainingJobOrchestrator
{
    public Guid CreateJobWorkspace(TrainingProfile profile, string trainer, out string jobDir)
    {
        var jobId = Guid.NewGuid();
        jobDir = Path.Combine(LazarusPaths.SystemData.Training.JobsRoot, jobId.ToString("N"));
        var trainerDir = Path.Combine(jobDir, "trainer");
        var subDirs = new[]
        {
            jobDir,
            trainerDir,
            Path.Combine(jobDir, "logs"),
            Path.Combine(jobDir, "checkpoints"),
            Path.Combine(jobDir, "artifacts"),
            Path.Combine(jobDir, "trainer", "llama-factory"),
            Path.Combine(jobDir, "trainer", "axolotl"),
            Path.Combine(jobDir, "trainer", "unsloth")
        };
        foreach (var d in subDirs) Directory.CreateDirectory(d);
        return jobId;
    }

    public async Task WriteManifestAsync(Guid jobId, TrainingProfile profile, string trainer, IEnumerable<string> trainFiles, IEnumerable<string> evalFiles, CancellationToken ct = default)
    {
        var jobDir = Path.Combine(LazarusPaths.SystemData.Training.JobsRoot, jobId.ToString("N"));
        Directory.CreateDirectory(jobDir);
        var manifestPath = Path.Combine(jobDir, "manifest.json");
        var manifest = new
        {
            profile,
            trainer,
            resolved = new
            {
                baseModelPath = Path.Combine(LazarusPaths.Models.BaseModels, profile.BaseModel),
                tokenizerPath = Path.Combine(LazarusPaths.Models.Tokenizers, profile.BaseModel),
                trainFiles = trainFiles,
                evalFiles = evalFiles
            }
        };
        var json = JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(manifestPath, json, ct).ConfigureAwait(false);
    }

    public async Task WritePlanFilesAsync(Guid jobId, TrainerPlan plan, CancellationToken ct = default)
    {
        var jobDir = Path.Combine(LazarusPaths.SystemData.Training.JobsRoot, jobId.ToString("N"));
        foreach (var f in plan.Files)
        {
            var full = Path.Combine(jobDir, f.RelativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(full)!);
            await File.WriteAllTextAsync(full, f.Contents, ct).ConfigureAwait(false);
        }
    }
}

