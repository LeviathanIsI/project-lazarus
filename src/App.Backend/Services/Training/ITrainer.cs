using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Lazarus.Shared.Models.Training;

namespace Lazarus.Backend.Services.Training;

public interface ITrainer
{
    string Name { get; }

    Task<TrainerPlan> PlanAsync(TrainingProfile profile, CancellationToken ct = default);
    Task<TrainerRunResult> RunAsync(TrainerPlan plan, Guid jobId, CancellationToken ct = default);
}

public sealed class TrainerPlan
{
    public string Trainer { get; init; } = string.Empty; // llama-factory | axolotl | unsloth
    public string WorkingDirectory { get; init; } = string.Empty; // Jobs/<id>
    public List<PlannedFile> Files { get; init; } = new(); // to write under trainer/
    public string LaunchCommand { get; init; } = string.Empty; // e.g., python -m llamafactory.cli train @trainer/lmf-args.json
}

public sealed class PlannedFile
{
    public string RelativePath { get; init; } = string.Empty; // relative to job working dir
    public string Contents { get; init; } = string.Empty;
}

public sealed class TrainerRunResult
{
    public bool Started { get; init; }
    public int? ProcessId { get; init; }
}

