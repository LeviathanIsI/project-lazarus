using Lazarus.Shared.Contracts.Training;

namespace Lazarus.Backend.Services.Training
{
    public interface ILogsStream : IAsyncDisposable
    {
        IAsyncEnumerable<TrainingLogEvent> ReadAllAsync(CancellationToken ct = default);
    }

    public interface IMetricsStream : IAsyncDisposable
    {
        IAsyncEnumerable<TrainingMetricsSnapshot> ReadAllAsync(CancellationToken ct = default);
    }

    public interface ITrainingService
    {
        Task QueueAsync(TrainingJob job, TrainingConfig config, TrainingResources resources, CancellationToken ct = default);
        Task StartAsync(Guid jobId, CancellationToken ct = default);
        Task PauseAsync(Guid jobId, CancellationToken ct = default);
        Task ResumeAsync(Guid jobId, CancellationToken ct = default);
        Task StopAsync(Guid jobId, CancellationToken ct = default);
        Task ExportAsync(Guid jobId, string targetPath, CancellationToken ct = default);

        Task<IMetricsStream> OpenMetricsAsync(Guid jobId, CancellationToken ct = default);
        Task<ILogsStream> OpenLogsAsync(Guid jobId, CancellationToken ct = default);
    }
}

