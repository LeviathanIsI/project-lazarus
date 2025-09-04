using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;

namespace Lazarus.App.Desktop.ViewModels;

/// <summary>
/// View model for the Jobs section
/// </summary>
public partial class JobsViewModel : BaseViewModel
{
    private readonly ILogger<JobsViewModel> _logger;
    private JobItem? _selectedJob;

    /// <summary>
    /// Initializes a new instance of the <see cref="JobsViewModel"/> class
    /// </summary>
    /// <param name="logger">The logger instance</param>
    public JobsViewModel(ILogger<JobsViewModel> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        Title = "Jobs";
        StatusMessage = "Job management coming soon";
        
        // Initialize collections
        Jobs = new ObservableCollection<JobItem>();
        
        // Initialize commands
        CreateJobCommand = new RelayCommand(ExecuteCreateJob);
        CancelJobCommand = new RelayCommand<JobItem>(ExecuteCancelJob);
        
        // Load sample data
        LoadSampleJobs();
        
        _logger.LogInformation("Jobs view model initialized");
    }

    /// <summary>
    /// Gets the title of the view
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// Gets or sets the selected job
    /// </summary>
    public JobItem? SelectedJob
    {
        get => _selectedJob;
        set => SetProperty(ref _selectedJob, value);
    }

    /// <summary>
    /// Gets the collection of jobs
    /// </summary>
    public ObservableCollection<JobItem> Jobs { get; }

    /// <summary>
    /// Gets the create job command
    /// </summary>
    public IRelayCommand CreateJobCommand { get; }

    /// <summary>
    /// Gets the cancel job command
    /// </summary>
    public IRelayCommand<JobItem> CancelJobCommand { get; }

    /// <summary>
    /// Executes the create job command
    /// </summary>
    private void ExecuteCreateJob()
    {
        _logger.LogInformation("Creating new job");
        
        var newJob = new JobItem(
            $"Job {DateTime.Now:HHmmss}",
            "Training",
            "Queued",
            0,
            DateTime.Now,
            null
        );
        
        Jobs.Insert(0, newJob);
        SelectedJob = newJob;
        StatusMessage = $"Job '{newJob.Name}' created";
    }

    /// <summary>
    /// Executes the cancel job command
    /// </summary>
    /// <param name="job">The job to cancel</param>
    private void ExecuteCancelJob(JobItem? job)
    {
        if (job == null) return;
        
        _logger.LogInformation("Canceling job: {JobName}", job.Name);
        StatusMessage = $"Job '{job.Name}' canceled";
    }

    /// <summary>
    /// Loads sample job data
    /// </summary>
    private void LoadSampleJobs()
    {
        Jobs.Clear();
        
        Jobs.Add(new JobItem("GPT Fine-tuning", "Training", "Running", 67.5, DateTime.Now.AddHours(-2), DateTime.Now.AddHours(1)));
        Jobs.Add(new JobItem("Image Classification", "Training", "Queued", 0, DateTime.Now.AddMinutes(-15), null));
        Jobs.Add(new JobItem("Text Generation", "Inference", "Completed", 100, DateTime.Now.AddHours(-4), DateTime.Now.AddHours(-3)));
        Jobs.Add(new JobItem("Model Evaluation", "Testing", "Failed", 45, DateTime.Now.AddHours(-6), DateTime.Now.AddHours(-5)));
    }
}

/// <summary>
/// Represents a job item
/// </summary>
public class JobItem
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JobItem"/> class
    /// </summary>
    /// <param name="name">The job name</param>
    /// <param name="type">The job type</param>
    /// <param name="status">The current status</param>
    /// <param name="progress">The progress percentage</param>
    /// <param name="startTime">When the job started</param>
    /// <param name="endTime">When the job ended (if applicable)</param>
    public JobItem(string name, string type, string status, double progress, DateTime startTime, DateTime? endTime)
    {
        Name = name;
        Type = type;
        Status = status;
        Progress = progress;
        StartTime = startTime;
        EndTime = endTime;
    }

    /// <summary>
    /// Gets the job name
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the job type
    /// </summary>
    public string Type { get; }

    /// <summary>
    /// Gets the current status
    /// </summary>
    public string Status { get; }

    /// <summary>
    /// Gets the progress percentage
    /// </summary>
    public double Progress { get; }

    /// <summary>
    /// Gets when the job started
    /// </summary>
    public DateTime StartTime { get; }

    /// <summary>
    /// Gets when the job ended (if applicable)
    /// </summary>
    public DateTime? EndTime { get; }
}