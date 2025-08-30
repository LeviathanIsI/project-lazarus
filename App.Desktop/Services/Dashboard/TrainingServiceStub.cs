using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

namespace Lazarus.Desktop.Services.Dashboard
{
    /// <summary>
    /// Stub implementation of ITrainingService for Dashboard widgets
    /// TODO: Replace with actual training service integration
    /// </summary>
    public class TrainingServiceStub : ITrainingService, IDisposable
    {
        private readonly ObservableCollection<TrainingJob> _activeJobs = new();
        private readonly ObservableCollection<TrainingJob> _queuedJobs = new();
        private readonly ObservableCollection<TrainingJob> _completedJobs = new();
        private readonly System.Timers.Timer _updateTimer;
        private readonly Random _random = new();

        public TrainingServiceStub()
        {
            // Initialize with some sample data
            InitializeSampleData();
            
            // Setup update timer to simulate job progress
            _updateTimer = new System.Timers.Timer(2000); // Update every 2 seconds
            _updateTimer.Elapsed += OnUpdateTimerElapsed;
            _updateTimer.AutoReset = true;
            _updateTimer.Start();
        }

        #region ITrainingService Implementation

        public ObservableCollection<TrainingJob> GetActiveJobs()
        {
            return new ObservableCollection<TrainingJob>(_activeJobs);
        }

        public ObservableCollection<TrainingJob> GetQueuedJobs()
        {
            return new ObservableCollection<TrainingJob>(_queuedJobs);
        }

        public ObservableCollection<TrainingJob> GetRecentlyCompleted()
        {
            return new ObservableCollection<TrainingJob>(_completedJobs.OrderByDescending(j => j.EndTime).Take(5));
        }

        public TrainingJob GetJobById(string jobId)
        {
            return _activeJobs.FirstOrDefault(j => j.Id == jobId) ??
                   _queuedJobs.FirstOrDefault(j => j.Id == jobId) ??
                   _completedJobs.FirstOrDefault(j => j.Id == jobId) ??
                   new TrainingJob { Id = jobId, Name = "Job not found", Status = TrainingJobStatus.Failed };
        }

        public async Task<string> StartTrainingAsync(TrainingConfiguration config)
        {
            await Task.Delay(100); // Simulate async operation
            
            var jobId = Guid.NewGuid().ToString("N")[..8];
            var job = new TrainingJob
            {
                Id = jobId,
                Name = config.Name,
                Status = TrainingJobStatus.Queued,
                ProgressPercent = 0,
                StartTime = DateTime.Now,
                ModelType = config.ModelType,
                DatasetPath = config.DatasetPath,
                Configuration = config.Parameters,
                EstimatedTimeRemaining = TimeSpan.FromHours(2) // Default estimate
            };
            
            _queuedJobs.Add(job);
            
            JobStatusChanged?.Invoke(this, new TrainingJobStatusChangedEventArgs
            {
                Job = job,
                OldStatus = TrainingJobStatus.Queued,
                NewStatus = TrainingJobStatus.Queued
            });
            
            return jobId;
        }

        public event EventHandler<TrainingJobStatusChangedEventArgs>? JobStatusChanged;

        #endregion

        #region Private Methods

        private void InitializeSampleData()
        {
            // Add some sample active jobs
            _activeJobs.Add(new TrainingJob
            {
                Id = "job001",
                Name = "LoRA Fine-tuning - Character Portraits",
                Status = TrainingJobStatus.Running,
                ProgressPercent = 67,
                StartTime = DateTime.Now.AddMinutes(-45),
                ModelType = "SDXL",
                DatasetPath = @"C:\Datasets\character_portraits",
                EstimatedTimeRemaining = TimeSpan.FromMinutes(15),
                Configuration = new() { { "learning_rate", 0.0001 }, { "batch_size", 4 } }
            });

            // Add some sample queued jobs
            _queuedJobs.Add(new TrainingJob
            {
                Id = "job002",
                Name = "Text Model Fine-tuning - Code Assistant",
                Status = TrainingJobStatus.Queued,
                ProgressPercent = 0,
                StartTime = DateTime.Now,
                ModelType = "Llama-3-8B",
                DatasetPath = @"C:\Datasets\code_samples",
                EstimatedTimeRemaining = TimeSpan.FromHours(3),
                Configuration = new() { { "epochs", 3 }, { "learning_rate", 0.00005 } }
            });

            // Add some completed jobs
            _completedJobs.Add(new TrainingJob
            {
                Id = "job003",
                Name = "Style Transfer Model",
                Status = TrainingJobStatus.Completed,
                ProgressPercent = 100,
                StartTime = DateTime.Now.AddHours(-2),
                EndTime = DateTime.Now.AddMinutes(-10),
                ModelType = "SDXL",
                DatasetPath = @"C:\Datasets\art_styles",
                EstimatedTimeRemaining = TimeSpan.Zero,
                Configuration = new() { { "style", "renaissance" }, { "steps", 1000 } }
            });
        }

        private void OnUpdateTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            // Simulate job progress
            foreach (var job in _activeJobs.ToList())
            {
                if (job.Status == TrainingJobStatus.Running)
                {
                    var oldProgress = job.ProgressPercent;
                    job.ProgressPercent = Math.Min(100, job.ProgressPercent + _random.Next(1, 5));
                    
                    // Update estimated time remaining
                    if (job.ProgressPercent > 0)
                    {
                        var elapsed = DateTime.Now - job.StartTime;
                        var totalEstimated = elapsed.TotalMinutes * 100 / job.ProgressPercent;
                        job.EstimatedTimeRemaining = TimeSpan.FromMinutes(Math.Max(0, totalEstimated - elapsed.TotalMinutes));
                    }
                    
                    // Complete job if it reaches 100%
                    if (job.ProgressPercent >= 100)
                    {
                        job.Status = TrainingJobStatus.Completed;
                        job.EndTime = DateTime.Now;
                        job.EstimatedTimeRemaining = TimeSpan.Zero;
                        
                        _activeJobs.Remove(job);
                        _completedJobs.Add(job);
                        
                        JobStatusChanged?.Invoke(this, new TrainingJobStatusChangedEventArgs
                        {
                            Job = job,
                            OldStatus = TrainingJobStatus.Running,
                            NewStatus = TrainingJobStatus.Completed
                        });
                    }
                }
            }
            
            // Move queued jobs to active if there's capacity
            if (_activeJobs.Count < 2 && _queuedJobs.Any())
            {
                var nextJob = _queuedJobs.First();
                nextJob.Status = TrainingJobStatus.Starting;
                
                _queuedJobs.Remove(nextJob);
                
                // Simulate startup delay
                Task.Delay(3000).ContinueWith(_ =>
                {
                    nextJob.Status = TrainingJobStatus.Running;
                    nextJob.StartTime = DateTime.Now;
                    _activeJobs.Add(nextJob);
                    
                    JobStatusChanged?.Invoke(this, new TrainingJobStatusChangedEventArgs
                    {
                        Job = nextJob,
                        OldStatus = TrainingJobStatus.Starting,
                        NewStatus = TrainingJobStatus.Running
                    });
                });
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            _updateTimer?.Stop();
            _updateTimer?.Dispose();
        }

        #endregion
    }
}
