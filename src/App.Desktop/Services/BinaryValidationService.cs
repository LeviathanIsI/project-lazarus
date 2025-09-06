using Lazarus.Desktop.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Collections.ObjectModel;

namespace Lazarus.Desktop.Services;

/// <summary>
/// Lightweight binary validation service that checks file system availability
/// and driver compatibility during application startup without spawning processes.
/// </summary>
internal sealed class BinaryValidationService : IBinaryValidationService
{
    private readonly BinaryValidationOptions _options;
    private readonly ILogger<BinaryValidationService> _logger;
    private BinaryValidationStatus _status;

    // P/Invoke for CUDA driver detection (lightweight check only)
    [DllImport("nvcuda.dll", EntryPoint = "cuInit")]
    private static extern int CuInit(uint flags);

    public BinaryValidationService(
        IOptions<BinaryValidationOptions> options,
        ILogger<BinaryValidationService> logger)
    {
        _options = options.Value;
        _logger = logger;
        _status = new BinaryValidationStatus();
    }

    public BinaryValidationStatus Status => _status;

    public event EventHandler<BinaryValidationStatusChangedEventArgs>? StatusChanged;

    public async Task ValidateAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting binary validation");

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(_options.ValidationTimeout);

            var issues = new List<string>();
            bool binariesAvailable = false;
            bool cudaDriverAvailable = false;
            string? llamaServerPath = null;

            // Validate binaries directory structure
            var runnersPath = Path.Combine(_options.BinariesPath, _options.RunnersPath);
            if (!Directory.Exists(runnersPath))
            {
                issues.Add($"Runners directory not found: {runnersPath}");
                _logger.LogWarning("Runners directory not found at {RunnersPath}", runnersPath);
            }
            else
            {
                // Check for llama runner directory
                var llamaRunnerPath = Path.Combine(runnersPath, _options.LlamaRunnerDirectory);
                if (!Directory.Exists(llamaRunnerPath))
                {
                    issues.Add($"Llama runner directory not found: {llamaRunnerPath}");
                    _logger.LogWarning("Llama runner directory not found at {LlamaRunnerPath}", llamaRunnerPath);
                }
                else
                {
                    // Check for llama-server.exe
                    llamaServerPath = Path.Combine(llamaRunnerPath, _options.LlamaServerExecutable);
                    if (!File.Exists(llamaServerPath))
                    {
                        issues.Add($"Llama server executable not found: {llamaServerPath}");
                        _logger.LogWarning("Llama server executable not found at {LlamaServerPath}", llamaServerPath);
                    }
                    else
                    {
                        binariesAvailable = true;
                        _logger.LogInformation("Llama server executable found at {LlamaServerPath}", llamaServerPath);
                    }
                }
            }

            // Check CUDA driver availability (lightweight check only)
            if (_options.CheckCudaDriver)
            {
                cudaDriverAvailable = await CheckCudaDriverAvailabilityAsync(cts.Token).ConfigureAwait(false);
                if (!cudaDriverAvailable)
                {
                    issues.Add("CUDA driver not available or accessible");
                    _logger.LogWarning("CUDA driver check failed - GPU acceleration may not be available");
                }
                else
                {
                    _logger.LogInformation("CUDA driver is available");
                }
            }
            else
            {
                _logger.LogInformation("CUDA driver check disabled in configuration");
            }

            // Create new status
            var newStatus = new BinaryValidationStatus
            {
                BinariesAvailable = binariesAvailable,
                CudaDriverAvailable = cudaDriverAvailable,
                LlamaServerPath = llamaServerPath,
                Issues = issues.AsReadOnly(),
                RequiresCuda = _options.CheckCudaDriver,
                LastValidated = DateTimeOffset.Now
            };

            // Update status and notify listeners
            _status = newStatus;
            StatusChanged?.Invoke(this, new BinaryValidationStatusChangedEventArgs(newStatus));

            var statusMessage = newStatus.IsSystemReady ? "System ready" : "System not ready";
            _logger.LogInformation("Binary validation completed: {StatusMessage}. Issues: {IssueCount}",
                statusMessage, issues.Count);

            if (issues.Count > 0)
            {
                foreach (var issue in issues)
                {
                    _logger.LogWarning("Validation issue: {Issue}", issue);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Binary validation was cancelled");
            throw;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Binary validation timed out after {Timeout}", _options.ValidationTimeout);
            var timeoutStatus = new BinaryValidationStatus
            {
                Issues = new[] { $"Validation timed out after {_options.ValidationTimeout}" }.AsReadOnly(),
                LastValidated = DateTimeOffset.Now
            };
            _status = timeoutStatus;
            StatusChanged?.Invoke(this, new BinaryValidationStatusChangedEventArgs(timeoutStatus));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during binary validation");
            var errorStatus = new BinaryValidationStatus
            {
                Issues = new[] { $"Validation error: {ex.Message}" }.AsReadOnly(),
                LastValidated = DateTimeOffset.Now
            };
            _status = errorStatus;
            StatusChanged?.Invoke(this, new BinaryValidationStatusChangedEventArgs(errorStatus));
        }
    }

    /// <summary>
    /// Performs lightweight CUDA driver availability check.
    /// Does NOT allocate VRAM or initialize GPU contexts.
    /// </summary>
    private async Task<bool> CheckCudaDriverAvailabilityAsync(CancellationToken cancellationToken)
    {
        return await Task.Run(() =>
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Try to call cuInit(0) which initializes the CUDA driver API
                // This is a lightweight operation that doesn't allocate GPU memory
                var result = CuInit(0);

                // CUDA_SUCCESS is 0, any other value indicates error
                return result == 0;
            }
            catch (DllNotFoundException)
            {
                // nvcuda.dll not found - CUDA driver not installed
                _logger.LogDebug("CUDA driver DLL not found");
                return false;
            }
            catch (Win32Exception)
            {
                // Other Windows API error
                _logger.LogDebug("Win32 error accessing CUDA driver");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Unexpected error checking CUDA driver");
                return false;
            }
        }, cancellationToken).ConfigureAwait(false);
    }
}