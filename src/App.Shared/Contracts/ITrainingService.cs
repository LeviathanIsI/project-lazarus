using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Lazarus.Shared.Contracts
{
    public interface ITrainingService
    {
        // Job lifecycle
        Task<TrainingJob> CreateJobAsync(string name, TrainingModality modality, CancellationToken cancellationToken = default);
        Task<TrainingJob> GetJobAsync(string jobId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<TrainingJob>> GetJobsAsync(CancellationToken cancellationToken = default);
        Task UpdateJobAsync(TrainingJob job, CancellationToken cancellationToken = default);
        Task DeleteJobAsync(string jobId, CancellationToken cancellationToken = default);
        Task<TrainingJob> DuplicateJobAsync(string jobId, CancellationToken cancellationToken = default);

        // Job operations
        Task QueueJobAsync(string jobId, CancellationToken cancellationToken = default);
        Task StartJobAsync(string jobId, CancellationToken cancellationToken = default);
        Task PauseJobAsync(string jobId, CancellationToken cancellationToken = default);
        Task ResumeJobAsync(string jobId, CancellationToken cancellationToken = default);
        Task StopJobAsync(string jobId, CancellationToken cancellationToken = default);
        Task<string> ExportJobAsync(string jobId, string exportPath, CancellationToken cancellationToken = default);

        // Batch operations
        Task StartMultipleAsync(IEnumerable<string> jobIds, CancellationToken cancellationToken = default);
        Task PauseMultipleAsync(IEnumerable<string> jobIds, CancellationToken cancellationToken = default);
        Task StopMultipleAsync(IEnumerable<string> jobIds, CancellationToken cancellationToken = default);
        Task DeleteMultipleAsync(IEnumerable<string> jobIds, CancellationToken cancellationToken = default);

        // Data management
        Task<TrainingDatasetRef> ImportDatasetAsync(string path, TrainingModality modality, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<TrainingDatasetRef>> GetDatasetsAsync(TrainingModality? modality = null, CancellationToken cancellationToken = default);
        Task<TrainingConfig> GetConfigAsync(string configId, CancellationToken cancellationToken = default);
        Task<TrainingResources> GetResourcesAsync(string resourcesId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<GpuInfo>> GetAvailableGpusAsync(CancellationToken cancellationToken = default);

        // Monitoring streams
        IObservable<TrainingMetricsSnapshot> GetMetricsStream(string jobId);
        IObservable<TrainingLogEvent> GetLogsStream(string jobId);
        IObservable<TrainingJob> GetJobStatusStream(string jobId);

        // Validation
        Task<ValidationResult> ValidateJobAsync(string jobId, CancellationToken cancellationToken = default);
        Task<ResourceEstimate> EstimateResourcesAsync(TrainingConfig config, TrainingResources resources, CancellationToken cancellationToken = default);
    }

    public sealed class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
    }

    public sealed class ResourceEstimate
    {
        public long EstimatedVramBytes { get; set; }
        public TimeSpan EstimatedTimePerEpoch { get; set; }
        public double EstimatedTotalHours { get; set; }
        public List<string> Recommendations { get; set; } = new();
    }
}
