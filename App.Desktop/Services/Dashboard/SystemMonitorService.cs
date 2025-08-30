using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Timers;
using Lazarus.Desktop.ViewModels;

namespace Lazarus.Desktop.Services.Dashboard
{
    /// <summary>
    /// Implementation of ISystemMonitor using existing SystemStateViewModel
    /// Provides system monitoring data for Dashboard widgets
    /// </summary>
    public class SystemMonitorService : ISystemMonitor, IDisposable
    {
        private readonly SystemStateViewModel _systemState;
        private readonly System.Timers.Timer _updateTimer;
        private readonly object _lockObject = new object();
        
        private SystemStatus _currentStatus = new();
        private MemoryUsage _currentMemory = new();
        private ObservableCollection<ProcessInfo> _runningProcesses = new();
        private ObservableCollection<DataPoint> _cpuHistory = new();
        private ObservableCollection<DataPoint> _memoryHistory = new();
        
        private const int MAX_HISTORY_POINTS = 60; // Keep 1 minute of data at 1-second intervals

        public SystemMonitorService(SystemStateViewModel systemState)
        {
            _systemState = systemState ?? throw new ArgumentNullException(nameof(systemState));
            
            // Subscribe to SystemState changes
            _systemState.PropertyChanged += OnSystemStateChanged;
            
            // Setup update timer for history tracking
            _updateTimer = new System.Timers.Timer(1000); // Update every second
            _updateTimer.Elapsed += OnUpdateTimerElapsed;
            _updateTimer.AutoReset = true;
            _updateTimer.Start();
            
            // Initial data load
            UpdateCurrentData();
        }

        #region ISystemMonitor Implementation

        public SystemStatus GetCurrentStatus()
        {
            lock (_lockObject)
            {
                return new SystemStatus
                {
                    IsOnline = _systemState.IsOnline,
                    CurrentRunner = _systemState.CurrentRunner ?? "None",
                    RunnerStates = GetRunnerStates(),
                    ApiResponseTime = TimeSpan.FromMilliseconds(100), // TODO: Get from actual API metrics
                    CpuUsagePercent = _systemState.CpuUsagePercent,
                    GpuUtilizationPercent = _systemState.GpuUtilizationPercent,
                    ApiStatus = _systemState.ApiStatus ?? "Unknown",
                    ApiStatusColor = _systemState.ApiStatusColor ?? "#6b7280"
                };
            }
        }

        public MemoryUsage GetMemoryUsage()
        {
            lock (_lockObject)
            {
                // Parse VRAM usage from SystemState with bounds checking
                var vramParts = _systemState.VramUsage?.Split('/') ?? new[] { "0 MB", "0 MB" };
                var usedVramMB = vramParts.Length > 0 ? ParseMemoryValue(vramParts[0]) : 0;
                var totalVramMB = vramParts.Length > 1 ? ParseMemoryValue(vramParts[1]) : 0;
                
                // Parse System RAM usage with bounds checking
                var ramParts = _systemState.SystemRamUsage?.Split('/') ?? new[] { "0.0 GB", "0.0 GB" };
                var usedRamGB = ramParts.Length > 0 ? ParseMemoryValue(ramParts[0]) : 0;
                var totalRamGB = ramParts.Length > 1 ? ParseMemoryValue(ramParts[1]) : 0;

                return new MemoryUsage
                {
                    TotalSystemRam = (long)(totalRamGB * 1024 * 1024 * 1024),
                    UsedSystemRam = (long)(usedRamGB * 1024 * 1024 * 1024),
                    TotalVram = (long)(totalVramMB * 1024 * 1024),
                    UsedVram = (long)(usedVramMB * 1024 * 1024)
                };
            }
        }

        public ObservableCollection<ProcessInfo> GetRunningProcesses()
        {
            lock (_lockObject)
            {
                try
                {
                    var processes = Process.GetProcesses()
                        .Where(p => !p.HasExited && !string.IsNullOrEmpty(p.ProcessName))
                        .Take(20) // Limit to top 20 processes
                        .Select(p => new ProcessInfo
                        {
                            ProcessId = p.Id,
                            ProcessName = p.ProcessName,
                            Status = p.Responding ? "Running" : "Not Responding",
                            CpuUsage = 0, // TODO: Calculate actual CPU usage
                            MemoryUsage = p.WorkingSet64,
                            StartTime = p.StartTime,
                            CommandLine = GetProcessCommandLine(p)
                        })
                        .OrderByDescending(p => p.MemoryUsage)
                        .ToList();

                    _runningProcesses.Clear();
                    foreach (var process in processes)
                    {
                        _runningProcesses.Add(process);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error getting processes: {ex.Message}");
                }

                return _runningProcesses;
            }
        }

        public ObservableCollection<DataPoint> GetCpuHistory(TimeSpan timeRange)
        {
            lock (_lockObject)
            {
                var cutoff = DateTime.Now - timeRange;
                var filteredHistory = _cpuHistory
                    .Where(dp => dp.Timestamp >= cutoff)
                    .ToList();

                return new ObservableCollection<DataPoint>(filteredHistory);
            }
        }

        public ObservableCollection<DataPoint> GetMemoryHistory(TimeSpan timeRange)
        {
            lock (_lockObject)
            {
                var cutoff = DateTime.Now - timeRange;
                var filteredHistory = _memoryHistory
                    .Where(dp => dp.Timestamp >= cutoff)
                    .ToList();

                return new ObservableCollection<DataPoint>(filteredHistory);
            }
        }

        public double GetTokensPerSecond()
        {
            if (double.TryParse(_systemState.TokensPerSecond?.Replace("t/s", "").Trim(), out double tokens))
            {
                return tokens;
            }
            return 0.0;
        }

        public event EventHandler<SystemStatusChangedEventArgs>? StatusChanged;

        #endregion

        #region Private Methods

        private void UpdateCurrentData()
        {
            _currentStatus = GetCurrentStatus();
            _currentMemory = GetMemoryUsage();
        }

        private Dictionary<RunnerType, RunnerStatus> GetRunnerStates()
        {
            var states = new Dictionary<RunnerType, RunnerStatus>();
            
            // Determine runner states based on current runner and online status
            if (_systemState.IsOnline && !string.IsNullOrEmpty(_systemState.CurrentRunner))
            {
                if (Enum.TryParse<RunnerType>(_systemState.CurrentRunner.Replace(".", ""), true, out var activeRunner))
                {
                    states[activeRunner] = RunnerStatus.Online;
                }
            }

            // Add other runners as offline (placeholder logic)
            foreach (RunnerType runner in Enum.GetValues<RunnerType>())
            {
                if (!states.ContainsKey(runner))
                {
                    states[runner] = RunnerStatus.Offline;
                }
            }

            return states;
        }

        private double ParseMemoryValue(string memoryString)
        {
            if (string.IsNullOrEmpty(memoryString)) return 0;
            
            var cleanString = memoryString.Trim();
            var parts = cleanString.Split(' ');
            
            if (parts.Length >= 2 && double.TryParse(parts[0], out double value))
            {
                var unit = parts[1].ToUpperInvariant();
                return unit switch
                {
                    "GB" => value,
                    "MB" => value / 1024.0,
                    "KB" => value / (1024.0 * 1024.0),
                    "B" => value / (1024.0 * 1024.0 * 1024.0),
                    _ => value
                };
            }
            
            return 0;
        }

        private string GetProcessCommandLine(Process process)
        {
            try
            {
                // Simplified - just return process name for now
                return process.ProcessName;
            }
            catch
            {
                return "";
            }
        }

        private void OnSystemStateChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            UpdateCurrentData();
            
            // Fire status changed event
            StatusChanged?.Invoke(this, new SystemStatusChangedEventArgs
            {
                SystemStatus = _currentStatus
            });
        }

        private void OnUpdateTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            lock (_lockObject)
            {
                var now = DateTime.Now;
                
                // Add CPU data point
                _cpuHistory.Add(new DataPoint
                {
                    Timestamp = now,
                    Value = _systemState.CpuUsagePercent
                });

                // Add Memory data point
                var memUsage = GetMemoryUsage();
                _memoryHistory.Add(new DataPoint
                {
                    Timestamp = now,
                    Value = memUsage.SystemRamPercentage
                });

                // Trim history to keep only recent data
                while (_cpuHistory.Count > MAX_HISTORY_POINTS)
                {
                    _cpuHistory.RemoveAt(0);
                }

                while (_memoryHistory.Count > MAX_HISTORY_POINTS)
                {
                    _memoryHistory.RemoveAt(0);
                }
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            _updateTimer?.Stop();
            _updateTimer?.Dispose();
            
            if (_systemState != null)
            {
                _systemState.PropertyChanged -= OnSystemStateChanged;
            }
        }

        #endregion
    }
}
