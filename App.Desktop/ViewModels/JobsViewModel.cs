using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Lazarus.Desktop.Helpers;
using Lazarus.Desktop.Services;
using Lazarus.Shared.Utilities;
using Timer = System.Timers.Timer;

namespace Lazarus.Desktop.ViewModels;

/// <summary>
/// Jobs and Queue management ViewModel for downloads, training, embeddings, and background tasks
/// Essential for any serious LLM platform - users need visibility into long-running operations
/// </summary>
public class JobsViewModel : INotifyPropertyChanged
{
    private readonly INavigationService _navigationService;
    private readonly Timer _jobUpdateTimer;
    
    public ObservableCollection<JobViewModel> ActiveJobs { get; } = new();
    public ObservableCollection<JobViewModel> CompletedJobs { get; } = new();
    public ObservableCollection<JobViewModel> QueuedJobs { get; } = new();
    
    private bool _hasActiveJobs;
    private bool _hasQueuedJobs;
    private string _overallStatus = "Ready";
    private int _totalJobsCount;
    private JobViewModel? _selectedJob;
    
    public bool HasActiveJobs
    {
        get => _hasActiveJobs;
        private set
        {
            if (_hasActiveJobs != value)
            {
                _hasActiveJobs = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ShowEmptyState));
            }
        }
    }
    
    public bool HasQueuedJobs
    {
        get => _hasQueuedJobs;
        private set
        {
            if (_hasQueuedJobs != value)
            {
                _hasQueuedJobs = value;
                OnPropertyChanged();
            }
        }
    }
    
    public string OverallStatus
    {
        get => _overallStatus;
        private set
        {
            if (_overallStatus != value)
            {
                _overallStatus = value;
                OnPropertyChanged();
            }
        }
    }
    
    public int TotalJobsCount
    {
        get => _totalJobsCount;
        private set
        {
            if (_totalJobsCount != value)
            {
                _totalJobsCount = value;
                OnPropertyChanged();
            }
        }
    }

    public JobViewModel? SelectedJob
    {
        get => _selectedJob;
        set
        {
            if (_selectedJob != value)
            {
                _selectedJob = value;
                OnPropertyChanged();
            }
        }
    }
    
    public bool ShowEmptyState => !HasActiveJobs && !HasQueuedJobs && CompletedJobs.Count == 0;
    
    // Commands
    public ICommand StartModelDownloadCommand { get; }
    public ICommand StartLoRATrainingCommand { get; }
    public ICommand StartEmbeddingProcessCommand { get; }
    public ICommand ClearCompletedCommand { get; }
    public ICommand PauseAllJobsCommand { get; }
    public ICommand ResumeAllJobsCommand { get; }
    public ICommand PauseJobCommand { get; }
    public ICommand ResumeJobCommand { get; }
    public ICommand CancelJobCommand { get; }
    
    public JobsViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
        
        // Initialize commands
        StartModelDownloadCommand = new RelayCommand(_ => StartModelDownload());
        StartLoRATrainingCommand = new RelayCommand(_ => StartLoRATraining());
        StartEmbeddingProcessCommand = new RelayCommand(_ => StartEmbeddingProcess());
        ClearCompletedCommand = new RelayCommand(_ => ClearCompletedJobs(), _ => CompletedJobs.Count > 0);
        PauseAllJobsCommand = new RelayCommand(_ => PauseAllJobs(), _ => HasActiveJobs);
        ResumeAllJobsCommand = new RelayCommand(_ => ResumeAllJobs(), _ => HasActiveJobs);
        
        // Per-job commands
        PauseJobCommand = new SimpleRelayCommand<JobViewModel>(PauseJob, j => j != null && j.CanPause);
        ResumeJobCommand = new SimpleRelayCommand<JobViewModel>(ResumeJob, j => j != null && j.CanResume);
        CancelJobCommand = new SimpleRelayCommand<JobViewModel>(CancelJob, j => j != null && j.CanCancel);

        // Start job monitoring timer
        _jobUpdateTimer = new Timer(1000); // Update every second for smooth progress
        _jobUpdateTimer.Elapsed += async (s, e) => await RefreshJobsAsync();
        _jobUpdateTimer.Start();
        
        // Initialize with some sample data for testing
        InitializeSampleJobs();
        
        Console.WriteLine("JobsViewModel: Initialized job management system");
    }
    
    private void InitializeSampleJobs()
    {
        // Add some sample jobs to demonstrate the interface
        ActiveJobs.Add(new JobViewModel
        {
            Id = Guid.NewGuid().ToString(),
            Type = JobType.ModelDownload,
            Name = "Downloading Llama-3.2-3B-Instruct-Q4_K_M.gguf",
            Progress = 45.7,
            Status = JobStatus.Running,
            StartTime = DateTime.Now.AddMinutes(-12),
            EstimatedTimeRemaining = TimeSpan.FromMinutes(8),
            Logs = new ObservableCollection<string>
            {
                "[00:00] Job started",
                "[00:12] Connecting to mirror...",
                "[00:45] Downloading chunks..."
            }
        });
        
        QueuedJobs.Add(new JobViewModel
        {
            Id = Guid.NewGuid().ToString(),
            Type = JobType.LoRATraining,
            Name = "Fine-tuning Code Assistant LoRA",
            Progress = 0,
            Status = JobStatus.Queued,
            QueuePosition = 1
        });
        
        CompletedJobs.Add(new JobViewModel
        {
            Id = Guid.NewGuid().ToString(),
            Type = JobType.EmbeddingGeneration,
            Name = "Generated embeddings for knowledge base",
            Progress = 100,
            Status = JobStatus.Completed,
            StartTime = DateTime.Now.AddHours(-2),
            CompletionTime = DateTime.Now.AddMinutes(-5)
        });
        
        UpdateJobCounts();
        if (ActiveJobs.Count > 0)
        {
            SelectedJob = ActiveJobs[0];
        }
    }
    
    private async Task RefreshJobsAsync()
    {
        try
        {
            // In real implementation, this would call the orchestrator API
            // await RefreshJobsFromApi();
            
            // For now, simulate job progress updates
            foreach (var job in ActiveJobs)
            {
                if (job.Status == JobStatus.Running)
                {
                    job.Progress += 0.5; // Simulate progress
                    if (job.Progress >= 100)
                    {
                        job.Status = JobStatus.Completed;
                        job.CompletionTime = DateTime.Now;
                        // Move to completed jobs
                        Application.Current?.Dispatcher.Invoke(() =>
                        {
                            CompletedJobs.Insert(0, job);
                            ActiveJobs.Remove(job);
                        });
                    }
                }
            }
            
            UpdateJobCounts();
            UpdateOverallStatus();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"JobsViewModel: Error refreshing jobs - {ex.Message}");
        }
    }
    
    private void UpdateJobCounts()
    {
        HasActiveJobs = ActiveJobs.Count > 0;
        HasQueuedJobs = QueuedJobs.Count > 0;
        TotalJobsCount = ActiveJobs.Count + QueuedJobs.Count + CompletedJobs.Count;
        OnPropertyChanged(nameof(ShowEmptyState));
    }
    
    private void UpdateOverallStatus()
    {
        if (HasActiveJobs)
        {
            var runningJobs = 0;
            foreach (var job in ActiveJobs)
            {
                if (job.Status == JobStatus.Running) runningJobs++;
            }
            OverallStatus = $"Running {runningJobs} job{(runningJobs == 1 ? "" : "s")}";
        }
        else if (HasQueuedJobs)
        {
            OverallStatus = $"{QueuedJobs.Count} job{(QueuedJobs.Count == 1 ? "" : "s")} queued";
        }
        else
        {
            OverallStatus = "Ready";
        }
    }
    
    private void StartModelDownload()
    {
        var job = new JobViewModel
        {
            Id = Guid.NewGuid().ToString(),
            Type = JobType.ModelDownload,
            Name = "New Model Download",
            Progress = 0,
            Status = JobStatus.Running,
            StartTime = DateTime.Now
        };
        
        ActiveJobs.Add(job);
        UpdateJobCounts();
        Console.WriteLine($"JobsViewModel: Started model download job - {job.Name}");
    }
    
    private void StartLoRATraining()
    {
        var job = new JobViewModel
        {
            Id = Guid.NewGuid().ToString(),
            Type = JobType.LoRATraining,
            Name = "Custom LoRA Training",
            Progress = 0,
            Status = JobStatus.Queued,
            QueuePosition = QueuedJobs.Count + 1
        };
        
        QueuedJobs.Add(job);
        UpdateJobCounts();
        Console.WriteLine($"JobsViewModel: Queued LoRA training job - {job.Name}");
    }
    
    private void StartEmbeddingProcess()
    {
        var job = new JobViewModel
        {
            Id = Guid.NewGuid().ToString(),
            Type = JobType.EmbeddingGeneration,
            Name = "Document Embedding Generation",
            Progress = 0,
            Status = JobStatus.Running,
            StartTime = DateTime.Now
        };
        
        ActiveJobs.Add(job);
        UpdateJobCounts();
        Console.WriteLine($"JobsViewModel: Started embedding generation job - {job.Name}");
    }
    
    private void ClearCompletedJobs()
    {
        CompletedJobs.Clear();
        UpdateJobCounts();
        Console.WriteLine("JobsViewModel: Cleared completed jobs");
    }
    
    private void PauseAllJobs()
    {
        foreach (var job in ActiveJobs)
        {
            if (job.Status == JobStatus.Running)
            {
                job.Status = JobStatus.Paused;
            }
        }
        Console.WriteLine("JobsViewModel: Paused all active jobs");
    }
    
    private void ResumeAllJobs()
    {
        foreach (var job in ActiveJobs)
        {
            if (job.Status == JobStatus.Paused)
            {
                job.Status = JobStatus.Running;
            }
        }
        Console.WriteLine("JobsViewModel: Resumed all paused jobs");
    }

    private void PauseJob(JobViewModel? job)
    {
        if (job == null) return;
        if (job.Status == JobStatus.Running)
        {
            job.Status = JobStatus.Paused;
        }
    }

    private void ResumeJob(JobViewModel? job)
    {
        if (job == null) return;
        if (job.Status == JobStatus.Paused)
        {
            job.Status = JobStatus.Running;
        }
    }

    private void CancelJob(JobViewModel? job)
    {
        if (job == null) return;
        job.Status = JobStatus.Cancelled;
        job.CompletionTime = DateTime.Now;

        Application.Current?.Dispatcher.Invoke(() =>
        {
            if (ActiveJobs.Contains(job)) ActiveJobs.Remove(job);
            if (QueuedJobs.Contains(job)) QueuedJobs.Remove(job);
            if (!CompletedJobs.Contains(job)) CompletedJobs.Insert(0, job);
        });

        UpdateJobCounts();
    }
    
    #region INotifyPropertyChanged
    
    public event PropertyChangedEventHandler? PropertyChanged;
    
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    
    #endregion
}

/// <summary>
/// Individual job item ViewModel
/// </summary>
public class JobViewModel : INotifyPropertyChanged
{
    private double _progress;
    private JobStatus _status;
    private string _statusMessage = string.Empty;
    private DateTime? _completionTime;
    private TimeSpan? _estimatedTimeRemaining;
    private ObservableCollection<string> _logs = new();
    
    public string Id { get; set; } = string.Empty;
    public JobType Type { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime? StartTime { get; set; }
    public int QueuePosition { get; set; }
    
    public double Progress
    {
        get => _progress;
        set
        {
            if (Math.Abs(_progress - value) > 0.01)
            {
                _progress = Math.Max(0, Math.Min(100, value));
                OnPropertyChanged();
                OnPropertyChanged(nameof(ProgressText));
                OnPropertyChanged(nameof(IsCompleted));
            }
        }
    }
    
    public JobStatus Status
    {
        get => _status;
        set
        {
            if (_status != value)
            {
                _status = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StatusText));
                OnPropertyChanged(nameof(CanPause));
                OnPropertyChanged(nameof(CanResume));
                OnPropertyChanged(nameof(CanCancel));
            }
        }
    }
    
    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            if (_statusMessage != value)
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }
    }
    
    public DateTime? CompletionTime
    {
        get => _completionTime;
        set
        {
            if (_completionTime != value)
            {
                _completionTime = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Duration));
            }
        }
    }
    
    public TimeSpan? EstimatedTimeRemaining
    {
        get => _estimatedTimeRemaining;
        set
        {
            if (_estimatedTimeRemaining != value)
            {
                _estimatedTimeRemaining = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ETAText));
            }
        }
    }
    
    // Computed properties
    public string ProgressText => $"{Progress:F1}%";
    public bool IsCompleted => Status == JobStatus.Completed;
    public bool CanPause => Status == JobStatus.Running;
    public bool CanResume => Status == JobStatus.Paused;
    public bool CanCancel => Status is JobStatus.Running or JobStatus.Paused or JobStatus.Queued;
    
    public string StatusText => Status switch
    {
        JobStatus.Queued => $"Queued (#{QueuePosition})",
        JobStatus.Running => "Running",
        JobStatus.Paused => "Paused",
        JobStatus.Completed => "Completed",
        JobStatus.Failed => "Failed",
        JobStatus.Cancelled => "Cancelled",
        _ => "Unknown"
    };
    
    public string TypeIcon => Type switch
    {
        JobType.ModelDownload => "📥",
        JobType.LoRATraining => "🔬",
        JobType.EmbeddingGeneration => "🧠",
        JobType.DatasetProcessing => "📊",
        JobType.ModelConversion => "🔄",
        _ => "⚙️"
    };
    
    public string Duration
    {
        get
        {
            if (StartTime.HasValue)
            {
                var end = CompletionTime ?? DateTime.Now;
                var duration = end - StartTime.Value;
                return $"{duration.TotalMinutes:F1}m";
            }
            return "-";
        }
    }
    
    public string ETAText => EstimatedTimeRemaining?.ToString(@"mm\:ss") ?? "-";
    public ObservableCollection<string> Logs
    {
        get => _logs;
        set
        {
            if (!ReferenceEquals(_logs, value))
            {
                _logs = value ?? new ObservableCollection<string>();
                OnPropertyChanged();
            }
        }
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;
    
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public enum JobType
{
    ModelDownload,
    LoRATraining,
    EmbeddingGeneration,
    DatasetProcessing,
    ModelConversion
}

public enum JobStatus
{
    Queued,
    Running,
    Paused,
    Completed,
    Failed,
    Cancelled
}