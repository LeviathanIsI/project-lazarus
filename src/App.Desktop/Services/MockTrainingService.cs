using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lazarus.Shared.Contracts;

namespace Lazarus.Desktop.Services
{
    // TODO(training): Replace with actual ITrainingService implementation
    public sealed class MockTrainingService : ITrainingService
    {
        public Task<TrainingJob> CreateJobAsync(string name, TrainingModality modality, CancellationToken cancellationToken = default)
        {
            var job = new TrainingJob
            {
                Id = Guid.NewGuid().ToString(),
                Name = name,
                Modality = modality,
                Status = TrainingStatus.Draft,
                ConfigId = Guid.NewGuid().ToString(),
                ResourcesId = Guid.NewGuid().ToString(),
                OutputPath = $"./outputs/{name}"
            };
            return Task.FromResult(job);
        }

        public Task DeleteJobAsync(string jobId, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task DeleteMultipleAsync(IEnumerable<string> jobIds, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<TrainingJob> DuplicateJobAsync(string jobId, CancellationToken cancellationToken = default)
        {
            var job = new TrainingJob
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Duplicated Job",
                Modality = TrainingModality.Conversations,
                Status = TrainingStatus.Draft,
                ConfigId = Guid.NewGuid().ToString(),
                ResourcesId = Guid.NewGuid().ToString(),
                OutputPath = "./outputs/duplicated"
            };
            return Task.FromResult(job);
        }

        public Task<ResourceEstimate> EstimateResourcesAsync(TrainingConfig config, TrainingResources resources, CancellationToken cancellationToken = default)
        {
            var estimate = new ResourceEstimate
            {
                EstimatedVramBytes = 8L * 1024 * 1024 * 1024, // 8GB
                EstimatedTimePerEpoch = TimeSpan.FromHours(2),
                EstimatedTotalHours = 6.0
            };
            return Task.FromResult(estimate);
        }

        public Task<string> ExportJobAsync(string jobId, string exportPath, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(exportPath);
        }

        public Task<IReadOnlyList<GpuInfo>> GetAvailableGpusAsync(CancellationToken cancellationToken = default)
        {
            var list = new List<GpuInfo>
            {
                new GpuInfo { Id = "gpu0", Name = "NVIDIA RTX 4090", TotalMemoryBytes = 24L * 1024 * 1024 * 1024, IsAvailable = true },
                new GpuInfo { Id = "gpu1", Name = "NVIDIA RTX 3080", TotalMemoryBytes = 12L * 1024 * 1024 * 1024, IsAvailable = true }
            }.AsReadOnly();
            
            return Task.FromResult<IReadOnlyList<GpuInfo>>(list);
        }

        public Task<TrainingConfig> GetConfigAsync(string configId, CancellationToken cancellationToken = default)
        {
            var config = new TrainingConfig
            {
                Id = configId,
                ModelId = "llama-7b-chat",
                Recipe = "LoRA",
                Modality = TrainingModality.Conversations,
                OutputPath = "./outputs/model"
            };
            return Task.FromResult(config);
        }

        public Task<IReadOnlyList<TrainingDatasetRef>> GetDatasetsAsync(TrainingModality? modality = null, CancellationToken cancellationToken = default)
        {
            var datasets = new List<TrainingDatasetRef>
            {
                new() { Id = Guid.NewGuid().ToString(), Name = "Sample Conversations", Type = DatasetType.ConversationJsonl, Modality = TrainingModality.Conversations, Path = "./datasets/conversations.jsonl", Stats = new DatasetStats { TotalItems = 1000, TotalTokens = 50000 } },
                new() { Id = Guid.NewGuid().ToString(), Name = "Sample Images", Type = DatasetType.ImageDirectory, Modality = TrainingModality.Images, Path = "./datasets/images/", Stats = new DatasetStats { TotalItems = 500 } }
            };
            
            if (modality.HasValue)
            {
                datasets = datasets.Where(d => d.Modality == modality.Value).ToList();
            }
            
            return Task.FromResult<IReadOnlyList<TrainingDatasetRef>>(datasets);
        }

        public Task<TrainingJob> GetJobAsync(string jobId, CancellationToken cancellationToken = default)
        {
            var job = new TrainingJob
            {
                Id = jobId,
                Name = "Sample Job",
                Modality = TrainingModality.Conversations,
                Status = TrainingStatus.Running,
                ConfigId = Guid.NewGuid().ToString(),
                ResourcesId = Guid.NewGuid().ToString(),
                OutputPath = "./outputs/sample",
                CurrentEpoch = 2,
                CurrentStep = 1234,
                Progress = 0.45,
                EstimatedTimeRemaining = TimeSpan.FromHours(1.5)
            };
            return Task.FromResult(job);
        }

        public Task<IReadOnlyList<TrainingJob>> GetJobsAsync(CancellationToken cancellationToken = default)
        {
            var jobs = new List<TrainingJob>
            {
                new() { Id = Guid.NewGuid().ToString(), Name = "Conversation Model Training", Modality = TrainingModality.Conversations, Status = TrainingStatus.Running, ConfigId = "", ResourcesId = "", OutputPath = "", CurrentEpoch = 2, CurrentStep = 1234, Progress = 0.45 },
                new() { Id = Guid.NewGuid().ToString(), Name = "Image Generation Fine-tune", Modality = TrainingModality.Images, Status = TrainingStatus.Queued, ConfigId = "", ResourcesId = "", OutputPath = "", CurrentEpoch = 0, CurrentStep = 0, Progress = 0.0 },
                new() { Id = Guid.NewGuid().ToString(), Name = "Voice Synthesis Training", Modality = TrainingModality.Voice, Status = TrainingStatus.Completed, ConfigId = "", ResourcesId = "", OutputPath = "", CurrentEpoch = 3, CurrentStep = 2000, Progress = 1.0 }
            };
            return Task.FromResult<IReadOnlyList<TrainingJob>>(jobs);
        }

        public IObservable<TrainingJob> GetJobStatusStream(string jobId)
        {
            // TODO(training): Return actual observable stream
            throw new NotImplementedException("Mock service - observable streams not implemented");
        }

        public IObservable<TrainingLogEvent> GetLogsStream(string jobId)
        {
            // TODO(training): Return actual observable stream  
            throw new NotImplementedException("Mock service - observable streams not implemented");
        }

        public IObservable<TrainingMetricsSnapshot> GetMetricsStream(string jobId)
        {
            // TODO(training): Return actual observable stream
            throw new NotImplementedException("Mock service - observable streams not implemented");
        }

        public Task<TrainingResources> GetResourcesAsync(string resourcesId, CancellationToken cancellationToken = default)
        {
            var resources = new TrainingResources
            {
                Id = resourcesId,
                GpuIds = new List<string> { "gpu0" },
                BatchSize = 4,
                Precision = PrecisionType.FP16,
                EstimatedVRAMBytes = 8L * 1024 * 1024 * 1024
            };
            return Task.FromResult(resources);
        }

        public Task<TrainingDatasetRef> ImportDatasetAsync(string path, TrainingModality modality, CancellationToken cancellationToken = default)
        {
            var dataset = new TrainingDatasetRef
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Imported Dataset",
                Type = DatasetType.ConversationJsonl,
                Modality = modality,
                Path = path,
                Stats = new DatasetStats { TotalItems = 500 }
            };
            return Task.FromResult(dataset);
        }

        public Task PauseJobAsync(string jobId, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task PauseMultipleAsync(IEnumerable<string> jobIds, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task QueueJobAsync(string jobId, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task ResumeJobAsync(string jobId, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task StartJobAsync(string jobId, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task StartMultipleAsync(IEnumerable<string> jobIds, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task StopJobAsync(string jobId, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task StopMultipleAsync(IEnumerable<string> jobIds, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task UpdateJobAsync(TrainingJob job, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<ValidationResult> ValidateJobAsync(string jobId, CancellationToken cancellationToken = default)
        {
            var result = new ValidationResult
            {
                IsValid = true,
                Errors = new List<string>(),
                Warnings = new List<string> { "This is a mock validation result" }
            };
            return Task.FromResult(result);
        }
    }
}
