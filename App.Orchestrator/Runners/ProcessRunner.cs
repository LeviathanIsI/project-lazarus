using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Lazarus.Orchestrator.Runners;

public class ProcessRunner : IDisposable
{
    private readonly ILogger _logger;
    private Process? _process;
    private readonly StringBuilder _outputBuffer = new();
    private readonly StringBuilder _errorBuffer = new();

    public string ProcessName { get; }
    public bool IsRunning => _process is { HasExited: false };
    public int? ProcessId => _process?.Id;

    public event EventHandler<string>? OutputReceived;
    public event EventHandler<string>? ErrorReceived;
    public event EventHandler<int>? ProcessExited;

    public ProcessRunner(string processName, ILogger logger)
    {
        ProcessName = processName;
        _logger = logger;
    }

    public async Task<bool> StartAsync(string executable, string arguments, string? workingDirectory = null)
    {
        if (IsRunning)
        {
            _logger.LogWarning($"{ProcessName} is already running (PID: {ProcessId})");
            return true;
        }

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = executable,
                Arguments = arguments,
                WorkingDirectory = workingDirectory ?? Path.GetDirectoryName(executable),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            _logger.LogInformation($"Starting {ProcessName}: {executable} {arguments}");

            _process = new Process { StartInfo = startInfo };
            _process.OutputDataReceived += OnOutputReceived;
            _process.ErrorDataReceived += OnErrorReceived;
            _process.Exited += OnProcessExited;
            _process.EnableRaisingEvents = true;

            if (!_process.Start())
            {
                _logger.LogError($"Failed to start {ProcessName}");
                return false;
            }

            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();

            _logger.LogInformation($"{ProcessName} started successfully (PID: {_process.Id})");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to start {ProcessName}");
            return false;
        }
    }

    public async Task StopAsync(TimeSpan timeout = default)
    {
        if (!IsRunning) return;

        timeout = timeout == default ? TimeSpan.FromSeconds(10) : timeout;

        try
        {
            _logger.LogInformation($"Stopping {ProcessName} (PID: {ProcessId})");

            _process!.CloseMainWindow();

            if (await WaitForExitAsync(timeout))
            {
                _logger.LogInformation($"{ProcessName} stopped gracefully");
                return;
            }

            _logger.LogWarning($"{ProcessName} didn't stop gracefully, forcing termination");
            _process.Kill(entireProcessTree: true);

            if (await WaitForExitAsync(TimeSpan.FromSeconds(5)))
            {
                _logger.LogInformation($"{ProcessName} terminated forcefully");
            }
            else
            {
                _logger.LogError($"Failed to terminate {ProcessName}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error stopping {ProcessName}");
        }
    }

    private async Task<bool> WaitForExitAsync(TimeSpan timeout)
    {
        if (_process?.HasExited == true) return true;

        return await Task.Run(() => _process?.WaitForExit((int)timeout.TotalMilliseconds) == true);
    }

    private void OnOutputReceived(object sender, DataReceivedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.Data)) return;

        _outputBuffer.AppendLine(e.Data);
        _logger.LogDebug($"[{ProcessName}] {e.Data}");
        OutputReceived?.Invoke(this, e.Data);
    }

    private void OnErrorReceived(object sender, DataReceivedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.Data)) return;

        _errorBuffer.AppendLine(e.Data);
        _logger.LogWarning($"[{ProcessName}] ERROR: {e.Data}");
        ErrorReceived?.Invoke(this, e.Data);
    }

    private void OnProcessExited(object? sender, EventArgs e)
    {
        var exitCode = _process?.ExitCode ?? -1;
        _logger.LogInformation($"{ProcessName} exited with code {exitCode}");
        ProcessExited?.Invoke(this, exitCode);
    }

    public void Dispose()
    {
        if (IsRunning)
        {
            Task.Run(() => StopAsync()).Wait(TimeSpan.FromSeconds(5));
        }

        _process?.Dispose();
    }
}