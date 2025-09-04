using Lazarus.App.Shared.Contracts;
using Lazarus.App.Shared.Models;
using System.Collections.Concurrent;

namespace Lazarus.App.Orchestrator.Services;

/// <summary>
/// Service for managing runner instances
/// </summary>
public class RunnerService : IRunnerService
{
    private readonly ILogger<RunnerService> _logger;
    private readonly ConcurrentDictionary<string, RunnerStatus> _runners = new();
    private readonly Random _random = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="RunnerService"/> class
    /// </summary>
    /// <param name="logger">The logger</param>
    public RunnerService(ILogger<RunnerService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Initialize with some demo runners for development
        InitializeDemoRunners();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<RunnerStatus>> GetAllRunnersAsync(CancellationToken cancellationToken = default)
    {
        await Task.Delay(50, cancellationToken); // Simulate async operation
        
        _logger.LogDebug("Retrieved {Count} runners", _runners.Count);
        return _runners.Values.ToList();
    }

    /// <inheritdoc />
    public async Task<RunnerStatus?> GetRunnerAsync(string runnerId, CancellationToken cancellationToken = default)
    {
        await Task.Delay(25, cancellationToken); // Simulate async operation
        
        _runners.TryGetValue(runnerId, out var runner);
        
        if (runner != null)
        {
            _logger.LogDebug("Retrieved runner {RunnerId} with status {Status}", runnerId, runner.Status);
        }
        else
        {
            _logger.LogDebug("Runner {RunnerId} not found", runnerId);
        }
        
        return runner;
    }

    /// <inheritdoc />
    public async Task<bool> StartRunnerAsync(string runnerId, CancellationToken cancellationToken = default)
    {
        await Task.Delay(100, cancellationToken); // Simulate async operation
        
        if (_runners.TryGetValue(runnerId, out var runner))
        {
            if (runner.Status == "stopped" || runner.Status == "idle")
            {
                runner.Status = "active";
                runner.LastActivity = DateTime.UtcNow;
                runner.UpTimeSeconds = 0; // Reset uptime when starting
                runner.VramUsageMb = 2048 + (_random.Next(0, 2048)); // Simulate VRAM usage
                
                _logger.LogInformation("Started runner {RunnerId}", runnerId);
                return true;
            }
            else
            {
                _logger.LogWarning("Runner {RunnerId} is already {Status}", runnerId, runner.Status);
                return false;
            }
        }
        
        _logger.LogWarning("Runner {RunnerId} not found", runnerId);
        return false;
    }

    /// <inheritdoc />
    public async Task<bool> StopRunnerAsync(string runnerId, CancellationToken cancellationToken = default)
    {
        await Task.Delay(100, cancellationToken); // Simulate async operation
        
        if (_runners.TryGetValue(runnerId, out var runner))
        {
            runner.Status = "stopped";
            runner.VramUsageMb = 0; // Clear VRAM usage when stopped
            
            _logger.LogInformation("Stopped runner {RunnerId}", runnerId);
            return true;
        }
        
        _logger.LogWarning("Runner {RunnerId} not found", runnerId);
        return false;
    }

    /// <inheritdoc />
    public async Task<bool> RegisterRunnerAsync(string runnerId, string name, string modelName, CancellationToken cancellationToken = default)
    {
        await Task.Delay(50, cancellationToken); // Simulate async operation
        
        var runner = new RunnerStatus
        {
            Id = runnerId,
            Name = name,
            Status = "idle",
            ModelName = modelName,
            VramUsageMb = 0,
            UpTimeSeconds = 0,
            LastActivity = DateTime.UtcNow,
            EndpointUrl = $"http://localhost:{8080 + _runners.Count}",
            ProcessId = null
        };

        var added = _runners.TryAdd(runnerId, runner);
        if (added)
        {
            _logger.LogInformation("Registered new runner {RunnerId} ({Name}) for model {ModelName}", runnerId, name, modelName);
        }
        else
        {
            _logger.LogWarning("Runner {RunnerId} already exists", runnerId);
        }
        
        return added;
    }

    /// <inheritdoc />
    public async Task<bool> UnregisterRunnerAsync(string runnerId, CancellationToken cancellationToken = default)
    {
        await Task.Delay(50, cancellationToken); // Simulate async operation
        
        var removed = _runners.TryRemove(runnerId, out var runner);
        if (removed && runner != null)
        {
            _logger.LogInformation("Unregistered runner {RunnerId} ({Name})", runnerId, runner.Name);
        }
        else
        {
            _logger.LogWarning("Runner {RunnerId} not found for unregistration", runnerId);
        }
        
        return removed;
    }

    /// <summary>
    /// Initialize demo runners for development and testing
    /// </summary>
    private void InitializeDemoRunners()
    {
        var demoRunners = new[]
        {
            new RunnerStatus
            {
                Id = "runner_1",
                Name = "llama-runner-1",
                Status = "active",
                ModelName = "llama-3.1-8b",
                VramUsageMb = 2048 + _random.Next(0, 512),
                UpTimeSeconds = 3600 + _random.Next(0, 7200),
                LastActivity = DateTime.UtcNow.AddSeconds(-_random.Next(0, 30)),
                EndpointUrl = "http://localhost:8080",
                ProcessId = 1234
            },
            new RunnerStatus
            {
                Id = "runner_2",
                Name = "mistral-runner-1",
                Status = "active",
                ModelName = "mistral-7b",
                VramUsageMb = 1536 + _random.Next(0, 512),
                UpTimeSeconds = 1800 + _random.Next(0, 3600),
                LastActivity = DateTime.UtcNow.AddSeconds(-_random.Next(0, 60)),
                EndpointUrl = "http://localhost:8081",
                ProcessId = 1235
            },
            new RunnerStatus
            {
                Id = "runner_3",
                Name = "llama-runner-2",
                Status = "idle",
                ModelName = "llama-3.1-8b",
                VramUsageMb = 0,
                UpTimeSeconds = 900 + _random.Next(0, 1800),
                LastActivity = DateTime.UtcNow.AddMinutes(-_random.Next(5, 30)),
                EndpointUrl = "http://localhost:8082",
                ProcessId = null
            }
        };

        foreach (var runner in demoRunners)
        {
            _runners.TryAdd(runner.Id, runner);
        }

        _logger.LogInformation("Initialized {Count} demo runners", demoRunners.Length);

        // Start a background task to periodically update runner metrics
        _ = Task.Run(UpdateRunnerMetricsLoop);
    }

    /// <summary>
    /// Background task that updates runner metrics periodically
    /// </summary>
    private async Task UpdateRunnerMetricsLoop()
    {
        while (true)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(5)); // Update every 5 seconds

                var now = DateTime.UtcNow;
                foreach (var runner in _runners.Values.Where(r => r.Status == "active"))
                {
                    // Update uptime
                    runner.UpTimeSeconds += 5;
                    
                    // Simulate activity
                    if (_random.NextDouble() > 0.3) // 70% chance of activity
                    {
                        runner.LastActivity = now;
                        
                        // Simulate VRAM fluctuation
                        var baseVram = runner.ModelName.Contains("llama") ? 2048 : 1536;
                        runner.VramUsageMb = baseVram + _random.Next(-256, 512);
                        runner.VramUsageMb = Math.Max(0, runner.VramUsageMb);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating runner metrics");
            }
        }
    }
}