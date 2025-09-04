using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using Lazarus.App.Desktop.Collections;

namespace Lazarus.App.Desktop.ViewModels;

/// <summary>
/// View model for the Runner Manager section
/// </summary>
public partial class RunnerManagerViewModel : BaseViewModel
{
    private readonly ILogger<RunnerManagerViewModel> _logger;
    private RunnerItem? _selectedRunner;

    /// <summary>
    /// Initializes a new instance of the <see cref="RunnerManagerViewModel"/> class
    /// </summary>
    /// <param name="logger">The logger instance</param>
    public RunnerManagerViewModel(ILogger<RunnerManagerViewModel> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        Title = "Runner Manager";
        StatusMessage = "Runner management tools coming soon";
        
        // Initialize collections
        Runners = new ThreadSafeObservableCollection<RunnerItem>();
        
        // Initialize commands
        StartRunnerCommand = new RelayCommand<RunnerItem>(ExecuteStartRunner);
        StopRunnerCommand = new RelayCommand<RunnerItem>(ExecuteStopRunner);
        RefreshRunnersCommand = new AsyncRelayCommand(RefreshRunnersAsync);
        
        // Load sample data
        LoadSampleRunners();
        
        _logger.LogInformation("Runner Manager view model initialized");
    }

    /// <summary>
    /// Gets the title of the view
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// Gets or sets the selected runner
    /// </summary>
    public RunnerItem? SelectedRunner
    {
        get => _selectedRunner;
        set => SetProperty(ref _selectedRunner, value);
    }

    /// <summary>
    /// Gets the thread-safe collection of runners
    /// </summary>
    public ThreadSafeObservableCollection<RunnerItem> Runners { get; }

    /// <summary>
    /// Gets the start runner command
    /// </summary>
    public IRelayCommand<RunnerItem> StartRunnerCommand { get; }

    /// <summary>
    /// Gets the stop runner command
    /// </summary>
    public IRelayCommand<RunnerItem> StopRunnerCommand { get; }

    /// <summary>
    /// Gets the refresh runners command
    /// </summary>
    public IAsyncRelayCommand RefreshRunnersCommand { get; }

    /// <summary>
    /// Executes the start runner command
    /// </summary>
    /// <param name="runner">The runner to start</param>
    private void ExecuteStartRunner(RunnerItem? runner)
    {
        if (runner == null) return;
        
        _logger.LogInformation("Starting runner: {RunnerName}", runner.Name);
        StatusMessage = $"Starting runner '{runner.Name}'...";
    }

    /// <summary>
    /// Executes the stop runner command
    /// </summary>
    /// <param name="runner">The runner to stop</param>
    private void ExecuteStopRunner(RunnerItem? runner)
    {
        if (runner == null) return;
        
        _logger.LogInformation("Stopping runner: {RunnerName}", runner.Name);
        StatusMessage = $"Stopping runner '{runner.Name}'...";
    }

    /// <summary>
    /// Refreshes the runners list
    /// </summary>
    /// <returns>A task that represents the asynchronous operation</returns>
    private async Task RefreshRunnersAsync()
    {
        try
        {
            SetBusyState(true, "Refreshing runners...");
            _logger.LogInformation("Refreshing runners list");

            // Simulate refresh delay
            await Task.Delay(1000);

            LoadSampleRunners();
            
            SetBusyState(false, "Runners refreshed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing runners");
            SetBusyState(false, "Failed to refresh runners");
        }
    }

    /// <summary>
    /// Loads sample runner data
    /// </summary>
    private void LoadSampleRunners()
    {
        Runners.Clear();
        
        Runners.Add(new RunnerItem("GPU Runner 1", "NVIDIA RTX 4090", "Running", 85.2, DateTime.Now.AddHours(-2)));
        Runners.Add(new RunnerItem("GPU Runner 2", "NVIDIA RTX 4080", "Idle", 12.5, DateTime.Now.AddMinutes(-30)));
        Runners.Add(new RunnerItem("CPU Runner 1", "Intel Core i9-13900K", "Running", 67.8, DateTime.Now.AddHours(-1)));
        Runners.Add(new RunnerItem("Cloud Runner 1", "AWS g4dn.xlarge", "Stopped", 0.0, DateTime.Now.AddHours(-6)));
        Runners.Add(new RunnerItem("Cloud Runner 2", "Azure NC6s_v3", "Running", 92.1, DateTime.Now.AddMinutes(-45)));
    }

    /// <summary>
    /// Disposes of resources used by the RunnerManagerViewModel
    /// </summary>
    protected override void DisposeResources()
    {
        // Clear collections to break reference chains
        Runners.Clear();
        SelectedRunner = null;

        base.DisposeResources();
    }
}

/// <summary>
/// Represents a runner item
/// </summary>
public class RunnerItem
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RunnerItem"/> class
    /// </summary>
    /// <param name="name">The runner name</param>
    /// <param name="hardware">The hardware description</param>
    /// <param name="status">The current status</param>
    /// <param name="utilization">The current utilization percentage</param>
    /// <param name="lastActivity">When the last activity occurred</param>
    public RunnerItem(string name, string hardware, string status, double utilization, DateTime lastActivity)
    {
        Name = name;
        Hardware = hardware;
        Status = status;
        Utilization = utilization;
        LastActivity = lastActivity;
    }

    /// <summary>
    /// Gets the runner name
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the hardware description
    /// </summary>
    public string Hardware { get; }

    /// <summary>
    /// Gets the current status
    /// </summary>
    public string Status { get; }

    /// <summary>
    /// Gets the current utilization percentage
    /// </summary>
    public double Utilization { get; }

    /// <summary>
    /// Gets when the last activity occurred
    /// </summary>
    public DateTime LastActivity { get; }
}