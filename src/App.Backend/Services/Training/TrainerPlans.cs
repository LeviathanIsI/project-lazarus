using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Lazarus.Shared;
using Lazarus.Shared.Models.Training;

namespace Lazarus.Backend.Services.Training;

public static class TrainerPlanner
{
    public static TrainerPlan PlanForLlamaFactory(TrainingProfile p, Guid jobId)
    {
        var jobDir = Path.Combine(LazarusPaths.SystemData.Training.JobsRoot, jobId.ToString("N"));
        var trainerDir = Path.Combine("trainer", "llama-factory");
        var argsPath = Path.Combine(trainerDir, "lmf-args.json");

        var map = new Dictionary<string, object?>
        {
            ["stage"] = p.Task,
            ["template"] = p.ChatTemplate,
            ["preprocessing_num_workers"] = 2,
            ["max_seq_length"] = p.Batching.MaxSeqLen,
            ["per_device_train_batch_size"] = p.Batching.PerDeviceBatch,
            ["gradient_accumulation_steps"] = p.Batching.GradAccum,
            ["learning_rate"] = p.Schedule.LearningRate,
            ["num_train_epochs"] = p.Schedule.Epochs,
            ["max_steps"] = p.Schedule.MaxSteps,
            ["lr_scheduler_type"] = p.Schedule.LrScheduler,
            ["warmup_ratio"] = p.Schedule.WarmupRatio,
            ["weight_decay"] = p.Schedule.WeightDecay,
            ["eval_steps"] = p.Eval.PerSteps,
            ["save_steps"] = p.Eval.SavePerSteps,
            ["logging_steps"] = p.Eval.LoggingPerSteps,
            ["bf16"] = p.Hardware.Bf16,
            ["fp16"] = p.Hardware.Fp16,
            // Paths resolved at runtime using LazarusPaths
            ["model_name_or_path"] = Path.Combine(LazarusPaths.Models.BaseModels, p.BaseModel),
            ["dataset_list"] = p.Dataset.Train,
            ["val_dataset_list"] = p.Dataset.Eval,
        };

        if (p.Optimization.LoRA.Enabled)
        {
            map["finetuning_type"] = "lora";
            map["lora_rank"] = p.Optimization.LoRA.R;
            map["lora_alpha"] = p.Optimization.LoRA.Alpha;
            map["lora_dropout"] = p.Optimization.LoRA.Dropout;
            map["lora_target"] = p.Optimization.LoRA.Target;
        }
        if (p.Optimization.QLoRA.Enabled)
        {
            map["quantization_bit"] = p.Optimization.QLoRA.Bits;
            map["bnb_4bit_compute_dtype"] = p.Hardware.Bf16 ? "bf16" : (p.Hardware.Fp16 ? "fp16" : "fp32");
        }

        var json = JsonSerializer.Serialize(map, new JsonSerializerOptions { WriteIndented = true });
        return new TrainerPlan
        {
            Trainer = "llama-factory",
            WorkingDirectory = jobDir,
            Files = new List<PlannedFile>
            {
                new PlannedFile { RelativePath = argsPath, Contents = json }
            },
            LaunchCommand = "python -m llamafactory.cli train @trainer/llama-factory/lmf-args.json"
        };
    }

    public static TrainerPlan PlanForAxolotl(TrainingProfile p, Guid jobId)
    {
        var jobDir = Path.Combine(LazarusPaths.SystemData.Training.JobsRoot, jobId.ToString("N"));
        var trainerDir = Path.Combine("trainer", "axolotl");
        var yamlPath = Path.Combine(trainerDir, "axolotl.yaml");

        // Minimal YAML using string builder (keep simple; adapter can refine later)
        var sb = new StringBuilder();
        sb.AppendLine("# Auto-generated from TrainingProfile");
        sb.AppendLine($"base_model: \"{Path.Combine(LazarusPaths.Models.BaseModels, p.BaseModel).Replace("\\", "/")}\"");
        sb.AppendLine($"template: {p.ChatTemplate}");
        sb.AppendLine($"task: {p.Task}");
        sb.AppendLine("datasets:");
        foreach (var d in p.Dataset.Train) sb.AppendLine($"  - {d.Replace("\\", "/")}");
        sb.AppendLine("eval_datasets:");
        foreach (var d in p.Dataset.Eval) sb.AppendLine($"  - {d.Replace("\\", "/")}");
        if (p.Optimization.LoRA.Enabled)
        {
            sb.AppendLine("lora:");
            sb.AppendLine($"  r: {p.Optimization.LoRA.R}");
            sb.AppendLine($"  alpha: {p.Optimization.LoRA.Alpha}");
            sb.AppendLine($"  dropout: {p.Optimization.LoRA.Dropout}");
            sb.AppendLine($"  target: \"{p.Optimization.LoRA.Target}\"");
        }
        sb.AppendLine("training:");
        sb.AppendLine($"  max_seq_len: {p.Batching.MaxSeqLen}");
        sb.AppendLine($"  per_device_batch: {p.Batching.PerDeviceBatch}");
        sb.AppendLine($"  grad_accum: {p.Batching.GradAccum}");
        if (p.Schedule.Epochs.HasValue) sb.AppendLine($"  epochs: {p.Schedule.Epochs}");
        if (p.Schedule.MaxSteps.HasValue) sb.AppendLine($"  max_steps: {p.Schedule.MaxSteps}");
        sb.AppendLine($"  learning_rate: {p.Schedule.LearningRate}");
        sb.AppendLine($"  lr_scheduler: {p.Schedule.LrScheduler}");
        sb.AppendLine($"  warmup_ratio: {p.Schedule.WarmupRatio}");
        sb.AppendLine($"  weight_decay: {p.Schedule.WeightDecay}");

        return new TrainerPlan
        {
            Trainer = "axolotl",
            WorkingDirectory = jobDir,
            Files = new List<PlannedFile> { new PlannedFile { RelativePath = yamlPath, Contents = sb.ToString() } },
            LaunchCommand = "axolotl train trainer/axolotl/axolotl.yaml"
        };
    }

    public static TrainerPlan PlanForUnsloth(TrainingProfile p, Guid jobId)
    {
        var jobDir = Path.Combine(LazarusPaths.SystemData.Training.JobsRoot, jobId.ToString("N"));
        var trainerDir = Path.Combine("trainer", "unsloth");
        var jsonPath = Path.Combine(trainerDir, "unsloth.json");

        var map = new Dictionary<string, object?>
        {
            ["task"] = p.Task,
            ["chat_template"] = p.ChatTemplate,
            ["base_model"] = Path.Combine(LazarusPaths.Models.BaseModels, p.BaseModel),
            ["train"] = p.Dataset.Train,
            ["eval"] = p.Dataset.Eval,
            ["lora_enabled"] = p.Optimization.LoRA.Enabled,
            ["lora_r"] = p.Optimization.LoRA.R,
            ["lora_alpha"] = p.Optimization.LoRA.Alpha,
            ["lora_dropout"] = p.Optimization.LoRA.Dropout,
            ["lora_target"] = p.Optimization.LoRA.Target,
            ["qlora_enabled"] = p.Optimization.QLoRA.Enabled,
            ["qlora_bits"] = p.Optimization.QLoRA.Bits,
            ["bnb_dtype"] = p.Optimization.QLoRA.BnbDtype,
            ["per_device_batch"] = p.Batching.PerDeviceBatch,
            ["grad_accum"] = p.Batching.GradAccum,
            ["max_seq_len"] = p.Batching.MaxSeqLen,
            ["lr"] = p.Schedule.LearningRate,
            ["epochs"] = p.Schedule.Epochs,
            ["max_steps"] = p.Schedule.MaxSteps,
            ["warmup_ratio"] = p.Schedule.WarmupRatio,
            ["scheduler"] = p.Schedule.LrScheduler,
            ["weight_decay"] = p.Schedule.WeightDecay,
        };
        var json = JsonSerializer.Serialize(map, new JsonSerializerOptions { WriteIndented = true });

        return new TrainerPlan
        {
            Trainer = "unsloth",
            WorkingDirectory = jobDir,
            Files = new List<PlannedFile> { new PlannedFile { RelativePath = jsonPath, Contents = json } },
            LaunchCommand = "python -m unsloth.train --config trainer/unsloth/unsloth.json"
        };
    }
}

