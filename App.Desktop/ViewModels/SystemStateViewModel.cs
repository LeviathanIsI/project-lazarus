using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Lazarus.Desktop.Services;
using Microsoft.Extensions.DependencyInjection;
using Timer = System.Timers.Timer;

namespace Lazarus.Desktop.ViewModels;

/// <summary>
/// System brain: Global state that's always visible across all views
/// Shows what model/runner is loaded, GPU/VRAM, context length, API status
/// This eliminates user blindness - they always know system state
/// </summary>
public class SystemStateViewModel : INotifyPropertyChanged
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Timer _statusUpdateTimer;
    
    private string _currentModel = "No model loaded";
    private string _currentRunner = "No runner";
    private string _apiStatus = "Unknown";
    private string _gpuInfo = "Unknown";
    private string _vramUsage = "0 MB / 0 MB";
    private double _vramPercentage = 0;
    private string _contextLength = "0";
    private string _tokensPerSecond = "0.0";
    private bool _isOnline = false;
    private int _queuedJobs = 0;
    private string _serverPort = "11711";
    
    public SystemStateViewModel(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        
        // Commands for quick actions in context bar
        SwitchModelCommand = new SystemRelayCommand(async () => await SwitchModelAsync());
        UnloadModelCommand = new SystemRelayCommand(async () => await UnloadModelAsync());
        RestartRunnerCommand = new SystemRelayCommand(async () => await RestartRunnerAsync());
        
        // Update system state every 2 seconds
        _statusUpdateTimer = new Timer(2000);
        _statusUpdateTimer.Elapsed += async (_, _) => await UpdateSystemStateAsync();
        _statusUpdateTimer.Start();
        
        // Initial update
        _ = UpdateSystemStateAsync();
        
        Console.WriteLine("[SystemState] 🧠 System brain initialized - users will never be blind again");
    }

    #region Properties - System Consciousness

    /// <summary>
    /// Currently loaded model name (displayed everywhere)
    /// </summary>
    public string CurrentModel
    {
        get => _currentModel;
        private set
        {
            _currentModel = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Active runner (llama.cpp, vLLM, etc.)
    /// </summary>
    public string CurrentRunner
    {
        get => _currentRunner;
        private set
        {
            _currentRunner = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// API connection status
    /// </summary>
    public string ApiStatus
    {
        get => _apiStatus;
        private set
        {
            _apiStatus = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ApiStatusColor));
        }
    }

    /// <summary>
    /// Color for API status indicator
    /// </summary>
    public string ApiStatusColor => _isOnline ? "#4ade80" : "#f87171"; // Green or Red

    /// <summary>
    /// GPU information
    /// </summary>
    public string GpuInfo
    {
        get => _gpuInfo;
        private set
        {
            _gpuInfo = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// VRAM usage display
    /// </summary>
    public string VramUsage
    {
        get => _vramUsage;
        private set
        {
            _vramUsage = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// VRAM percentage for progress bar
    /// </summary>
    public double VramPercentage
    {
        get => _vramPercentage;
        private set
        {
            _vramPercentage = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Model context length
    /// </summary>
    public string ContextLength
    {
        get => _contextLength;
        private set
        {
            _contextLength = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Inference speed
    /// </summary>
    public string TokensPerSecond
    {
        get => _tokensPerSecond;
        private set
        {
            _tokensPerSecond = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Whether API is online
    /// </summary>
    public bool IsOnline
    {
        get => _isOnline;
        private set
        {
            _isOnline = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ApiStatusColor));
        }
    }

    /// <summary>
    /// Number of queued jobs
    /// </summary>
    public int QueuedJobs
    {
        get => _queuedJobs;
        private set
        {
            _queuedJobs = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasQueuedJobs));
        }
    }

    /// <summary>
    /// Whether there are queued jobs (for visibility)
    /// </summary>
    public bool HasQueuedJobs => _queuedJobs > 0;

    /// <summary>
    /// Server port
    /// </summary>
    public string ServerPort
    {
        get => _serverPort;
        private set
        {
            _serverPort = value;
            OnPropertyChanged();
        }
    }

    #endregion

    #region Commands - Quick Actions

    public ICommand SwitchModelCommand { get; }
    public ICommand UnloadModelCommand { get; }
    public ICommand RestartRunnerCommand { get; }

    private async Task SwitchModelAsync()
    {
        try
        {
            Console.WriteLine("[SystemState] 🔄 User initiated model switch");
            // Navigate to model selection - will implement with INavigationService
            // For now, placeholder
            await Task.Delay(100);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SystemState] Error switching model: {ex.Message}");
        }
    }

    private async Task UnloadModelAsync()
    {
        try
        {
            Console.WriteLine("[SystemState] 🔄 User initiated model unload");
            
            // Call orchestrator to unload model
            var result = await ApiClient.UnloadModelAsync();
            
            if (result)
            {
                CurrentModel = "No model loaded";
                ContextLength = "0";
                TokensPerSecond = "0.0";
                VramUsage = "0 MB / 0 MB";
                VramPercentage = 0;
                
                Console.WriteLine("[SystemState] ✅ Model unloaded successfully");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SystemState] Error unloading model: {ex.Message}");
        }
    }

    private async Task RestartRunnerAsync()
    {
        try
        {
            Console.WriteLine("[SystemState] 🔄 User initiated runner restart");
            
            // Call orchestrator to restart runner
            var result = await ApiClient.RestartRunnerAsync();
            
            if (result)
            {
                Console.WriteLine("[SystemState] ✅ Runner restarted successfully");
                await UpdateSystemStateAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SystemState] Error restarting runner: {ex.Message}");
        }
    }

    #endregion

    #region System State Updates

    /// <summary>
    /// Update all system state from orchestrator
    /// This runs every 2 seconds to keep users informed
    /// </summary>
    private async Task UpdateSystemStateAsync()
    {
        try
        {
            // Get system status
            var systemStatus = await ApiClient.GetSystemStatusAsync();
            if (systemStatus != null)
            {
                IsOnline = true;
                ApiStatus = "Online";
                
                CurrentModel = systemStatus.LoadedModel ?? "No model loaded";
                CurrentRunner = systemStatus.ActiveRunner ?? "No runner";
                GpuInfo = systemStatus.GpuName ?? "CPU";
                ContextLength = systemStatus.ContextLength?.ToString() ?? "0";
                TokensPerSecond = systemStatus.TokensPerSecond?.ToString("F1") ?? "0.0";
                ServerPort = systemStatus.ServerPort?.ToString() ?? "11711";
                
                // Update VRAM info
                if (systemStatus.VramUsedMB.HasValue && systemStatus.VramTotalMB.HasValue)
                {
                    var usedMB = systemStatus.VramUsedMB.Value;
                    var totalMB = systemStatus.VramTotalMB.Value;
                    VramUsage = $"{usedMB:N0} MB / {totalMB:N0} MB";
                    VramPercentage = totalMB > 0 ? (double)usedMB / totalMB * 100 : 0;
                }
                else
                {
                    VramUsage = "Unknown";
                    VramPercentage = 0;
                }
                
                // Update job queue count
                QueuedJobs = systemStatus.QueuedJobs ?? 0;
            }
            else
            {
                // Offline state
                IsOnline = false;
                ApiStatus = "Offline";
                CurrentModel = "No connection";
                CurrentRunner = "Offline";
                TokensPerSecond = "0.0";
            }
        }
        catch (Exception ex)
        {
            // Error state
            IsOnline = false;
            ApiStatus = "Error";
            Console.WriteLine($"[SystemState] Error updating system state: {ex.Message}");
        }
    }

    #endregion

    #region INotifyPropertyChanged

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion

    #region Dispose

    public void Dispose()
    {
        _statusUpdateTimer?.Stop();
        _statusUpdateTimer?.Dispose();
    }

    #endregion
}

/// <summary>
/// Command implementation for SystemStateViewModel
/// </summary>
public class SystemRelayCommand : ICommand
{
    private readonly Func<Task> _execute;
    private readonly Func<bool>? _canExecute;
    private bool _isExecuting;

    public SystemRelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) => !_isExecuting && (_canExecute?.Invoke() ?? true);

    public async void Execute(object? parameter)
    {
        if (!CanExecute(parameter)) return;

        _isExecuting = true;
        CommandManager.InvalidateRequerySuggested();

        try
        {
            await _execute();
        }
        finally
        {
            _isExecuting = false;
            CommandManager.InvalidateRequerySuggested();
        }
    }
}