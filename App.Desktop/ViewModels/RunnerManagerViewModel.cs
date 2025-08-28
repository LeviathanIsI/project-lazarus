using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Lazarus.Desktop.Helpers;

namespace Lazarus.Desktop.ViewModels;

/// <summary>
/// Runner Manager ViewModel - Control and manage LLM runners
/// Provides UI for switching between llama.cpp, vLLM, and other backends
/// </summary>
public class RunnerManagerViewModel : INotifyPropertyChanged
{
    private bool _isLoading = false;
    private string _activeRunnerId = "";
    private string _activeRunnerName = "Loading...";
    private string _activeRunnerStatus = "Checking...";
    private string _statusMessage = "Initializing runner manager...";
    private bool _isHealthy = false;
    
    public RunnerManagerViewModel()
    {
        // Initialize commands
        RefreshRunnersCommand = new RelayCommand(_ => _ = RefreshRunnersAsync());
        SwitchRunnerCommand = new RelayCommand(SwitchRunner);
        StartRunnerCommand = new RelayCommand(StartRunner);
        StopRunnerCommand = new RelayCommand(StopRunner);
        RestartRunnerCommand = new RelayCommand(RestartRunner);
        
        // Initialize collections
        AvailableRunners = new ObservableCollection<RunnerViewModel>();
        
        // Load initial data
        _ = InitializeAsync();
        
        Console.WriteLine("[RunnerManagerViewModel] Initialized - Runner control ready");
    }

    #region Properties

    /// <summary>
    /// Whether the view is loading data
    /// </summary>
    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            _isLoading = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Active runner ID
    /// </summary>
    public string ActiveRunnerId
    {
        get => _activeRunnerId;
        set
        {
            _activeRunnerId = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Active runner display name
    /// </summary>
    public string ActiveRunnerName
    {
        get => _activeRunnerName;
        set
        {
            _activeRunnerName = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Active runner status
    /// </summary>
    public string ActiveRunnerStatus
    {
        get => _activeRunnerStatus;
        set
        {
            _activeRunnerStatus = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Status message for user feedback
    /// </summary>
    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            _statusMessage = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Whether the runner system is healthy
    /// </summary>
    public bool IsHealthy
    {
        get => _isHealthy;
        set
        {
            _isHealthy = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Collection of available runners
    /// </summary>
    public ObservableCollection<RunnerViewModel> AvailableRunners { get; }

    /// <summary>
    /// Selected runner for operations
    /// </summary>
    public RunnerViewModel? SelectedRunner { get; set; }

    #endregion

    #region Commands

    public ICommand RefreshRunnersCommand { get; }
    public ICommand SwitchRunnerCommand { get; }
    public ICommand StartRunnerCommand { get; }
    public ICommand StopRunnerCommand { get; }
    public ICommand RestartRunnerCommand { get; }

    #endregion

    #region Methods

    private async Task InitializeAsync()
    {
        StatusMessage = "Loading runner information...";
        await RefreshRunnersAsync();
    }

    private async Task RefreshRunnersAsync()
    {
        IsLoading = true;
        StatusMessage = "Refreshing runner information...";

        try
        {
            // Get runner information from API
            var runnerInfo = await ApiClient.GetRunnersAsync();
            if (runnerInfo == null)
            {
                StatusMessage = "❌ Failed to connect to orchestrator";
                IsHealthy = false;
                return;
            }

            // Update active runner info
            ActiveRunnerId = runnerInfo.Active ?? "None";
            ActiveRunnerName = runnerInfo.Available.FirstOrDefault(r => r.Id == runnerInfo.Active)?.Name ?? "Unknown";
            ActiveRunnerStatus = runnerInfo.Active != null ? "🟢 Running" : "🔴 Stopped";
            IsHealthy = runnerInfo.Active != null;

            // Update available runners collection
            AvailableRunners.Clear();
            foreach (var runner in runnerInfo.Available)
            {
                AvailableRunners.Add(new RunnerViewModel
                {
                    Id = runner.Id,
                    Name = runner.Name,
                    Description = runner.Description,
                    IsActive = runner.Id == runnerInfo.Active,
                    Status = runner.Id == runnerInfo.Active ? "Active" : "Available",
                    Type = GetRunnerType(runner.Id),
                    Icon = GetRunnerIcon(runner.Id)
                });
            }

            StatusMessage = $"✅ Found {AvailableRunners.Count} runners - Active: {ActiveRunnerName}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Error loading runners: {ex.Message}";
            IsHealthy = false;
            Console.WriteLine($"[RunnerManagerViewModel] RefreshRunners failed: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async void SwitchRunner(object? parameter)
    {
        if (parameter is not RunnerViewModel runner)
        {
            if (SelectedRunner == null)
            {
                StatusMessage = "⚠️ Please select a runner to switch to";
                return;
            }
            runner = SelectedRunner;
        }

        if (runner.IsActive)
        {
            StatusMessage = $"ℹ️ {runner.Name} is already active";
            return;
        }

        StatusMessage = $"🔄 Switching to {runner.Name}...";
        IsLoading = true;

        try
        {
            // Determine base URL based on runner type
            var baseUrl = GetRunnerBaseUrl(runner.Id);
            var success = await ApiClient.SwitchRunnerAsync(runner.Type, baseUrl);

            if (success)
            {
                StatusMessage = $"✅ Successfully switched to {runner.Name}";
                await RefreshRunnersAsync();
            }
            else
            {
                StatusMessage = $"❌ Failed to switch to {runner.Name}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Error switching runner: {ex.Message}";
            Console.WriteLine($"[RunnerManagerViewModel] SwitchRunner failed: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async void StartRunner(object? parameter)
    {
        StatusMessage = "🚀 Starting runner...";
        // TODO: Implement runner start functionality
        await Task.Delay(1000);
        StatusMessage = "ℹ️ Runner start functionality coming soon";
    }

    private async void StopRunner(object? parameter)
    {
        StatusMessage = "🛑 Stopping runner...";
        // TODO: Implement runner stop functionality
        await Task.Delay(1000);
        StatusMessage = "ℹ️ Runner stop functionality coming soon";
    }

    private async void RestartRunner(object? parameter)
    {
        StatusMessage = "🔄 Restarting active runner...";
        IsLoading = true;

        try
        {
            var success = await ApiClient.RestartRunnerAsync();
            if (success)
            {
                StatusMessage = "✅ Runner restarted successfully";
                await RefreshRunnersAsync();
            }
            else
            {
                StatusMessage = "❌ Failed to restart runner";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Error restarting runner: {ex.Message}";
            Console.WriteLine($"[RunnerManagerViewModel] RestartRunner failed: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static string GetRunnerType(string runnerId)
    {
        return runnerId.ToLower() switch
        {
            var id when id.Contains("llama-server") => "llama-server",
            var id when id.Contains("llama-cpp") => "llama-cpp", 
            var id when id.Contains("vllm") => "vllm",
            var id when id.Contains("exllama") => "exllama",
            _ => "unknown"
        };
    }

    private static string GetRunnerIcon(string runnerId)
    {
        return runnerId.ToLower() switch
        {
            var id when id.Contains("llama-server") => "🦙",
            var id when id.Contains("llama-cpp") => "⚡",
            var id when id.Contains("vllm") => "🚀",
            var id when id.Contains("exllama") => "🔥",
            _ => "🔧"
        };
    }

    private static string GetRunnerBaseUrl(string runnerId)
    {
        // This should be configured or detected, using defaults for now
        return runnerId.ToLower() switch
        {
            var id when id.Contains("llama-server") => "http://127.0.0.1:8080/v1",
            var id when id.Contains("vllm") => "http://127.0.0.1:8000/v1",
            _ => "http://127.0.0.1:8080/v1"
        };
    }

    #endregion

    #region INotifyPropertyChanged

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion
}

/// <summary>
/// View model for individual runner display
/// </summary>
public class RunnerViewModel : INotifyPropertyChanged
{
    private bool _isActive = false;
    private string _status = "Available";

    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Type { get; set; } = "";
    public string Icon { get; set; } = "🔧";

    public bool IsActive
    {
        get => _isActive;
        set
        {
            _isActive = value;
            OnPropertyChanged();
        }
    }

    public string Status
    {
        get => _status;
        set
        {
            _status = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}