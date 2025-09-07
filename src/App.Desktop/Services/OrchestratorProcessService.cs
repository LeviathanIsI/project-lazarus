using Lazarus.Desktop.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net.Http;

namespace Lazarus.Desktop.Services;

#if DEBUG
internal sealed class OrchestratorProcessService : IHostedService
{
    private readonly ILogger<OrchestratorProcessService> _logger;
    private readonly IServiceProvider _services;
    private Process? _process;

    public OrchestratorProcessService(ILogger<OrchestratorProcessService> logger, IServiceProvider services)
    {
        _logger = logger;
        _services = services;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (await IsOrchestratorRespondingAsync(cancellationToken).ConfigureAwait(false))
            {
                _logger.LogInformation("Orchestrator already responding; skipping local start");
                return;
            }

            var projectPath = TryResolveOrchestratorProjectPath();
            if (projectPath is null)
            {
                _logger.LogWarning("Could not resolve App.Orchestrator.Host project path; skipping auto-start");
                return;
            }

            var psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"run --project \"{projectPath}\" -c Debug",
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            _process = Process.Start(psi);
            if (_process is null)
            {
                _logger.LogWarning("Failed to start orchestrator host process");
                return;
            }

            // Wait briefly for it to come up
            var deadline = DateTimeOffset.UtcNow.AddSeconds(10);
            while (DateTimeOffset.UtcNow < deadline && !cancellationToken.IsCancellationRequested)
            {
                if (await IsOrchestratorRespondingAsync(cancellationToken).ConfigureAwait(false))
                {
                    _logger.LogInformation("Orchestrator host started by Desktop");
                    return;
                }
                await Task.Delay(300, cancellationToken).ConfigureAwait(false);
            }

            _logger.LogWarning("Timed out waiting for orchestrator host to respond");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error attempting to start orchestrator host");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (_process is { HasExited: false })
            {
                _logger.LogInformation("Stopping orchestrator host process started by Desktop");
                try { _process.CloseMainWindow(); } catch { }
                try { if (!_process.HasExited) _process.Kill(true); } catch { }
            }
        }
        finally
        {
            _process?.Dispose();
            _process = null;
        }

        return Task.CompletedTask;
    }

    private static async Task<bool> IsOrchestratorRespondingAsync(CancellationToken ct)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
            var resp = await http.GetAsync("http://127.0.0.1:11711/health", ct).ConfigureAwait(false);
            return resp.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private static string? TryResolveOrchestratorProjectPath()
    {
        // Priority: env LAZARUS_REPO_ROOT -> walk parents to find Lazarus.sln -> relative src/App.Orchestrator.Host
        var env = Environment.GetEnvironmentVariable("LAZARUS_REPO_ROOT");
        if (!string.IsNullOrWhiteSpace(env))
        {
            var p = Path.Combine(env, "src", "App.Orchestrator.Host", "App.Orchestrator.Host.csproj");
            if (File.Exists(p)) return p;
        }

        var dir = AppContext.BaseDirectory;
        for (int i = 0; i < 8 && dir is not null; i++)
        {
            var sln = Path.Combine(dir, "Lazarus.sln");
            var proj = Path.Combine(dir, "src", "App.Orchestrator.Host", "App.Orchestrator.Host.csproj");
            if (File.Exists(sln) && File.Exists(proj))
                return proj;
            dir = Directory.GetParent(dir)?.FullName ?? null;
        }

        return null;
    }
}
#endif

