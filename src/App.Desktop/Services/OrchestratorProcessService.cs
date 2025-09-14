using Lazarus.Desktop.Services;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net.Http;
using System.IO;

namespace Lazarus.Desktop.Services;

internal interface IOrchestratorProcessService
{
    Task StartIfNeededAsync(CancellationToken cancellationToken);
    Task StopIfOwnedAsync(CancellationToken cancellationToken);
}

internal sealed class OrchestratorProcessService : IOrchestratorProcessService
{
    private readonly ILogger<OrchestratorProcessService> _logger;
    private readonly IServiceProvider _services;
    private Process? _process;

    public OrchestratorProcessService(ILogger<OrchestratorProcessService> logger, IServiceProvider services)
    {
        _logger = logger;
        _services = services;
    }

    public async Task StartIfNeededAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (await IsOrchestratorRespondingAsync(cancellationToken).ConfigureAwait(false))
            {
                _logger.LogInformation("Orchestrator already responding; skipping local start");
                return;
            }

            ProcessStartInfo? psi = null;

#if DEBUG
            var projectPath = TryResolveOrchestratorProjectPath();
            if (projectPath is null)
            {
                _logger.LogWarning("Could not resolve App.Orchestrator.Host project path; skipping auto-start");
                return;
            }

            // To avoid locking the project bin during builds, shadow-copy the built output to a cache folder
            // and run the DLL from there via 'dotnet exec'.
            var srcOutDir = Path.Combine(Path.GetDirectoryName(projectPath)!, "bin", "Debug", "net8.0");
            var srcDll = Path.Combine(srcOutDir, "Lazarus.Orchestrator.Host.dll");
            if (!File.Exists(srcDll))
            {
                _logger.LogInformation("Orchestrator output not found at {Path}; will attempt to run via 'dotnet run' as fallback", srcDll);
                psi = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"run --project \"{projectPath}\" -c Debug",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
            }
            else
            {
                var shadowDir = Path.Combine(Lazarus.Shared.LazarusPaths.SystemData.Cache, "OrchestratorHost");
                Directory.CreateDirectory(shadowDir);
                try
                {
                    MirrorDirectory(srcOutDir, shadowDir);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to mirror orchestrator output; falling back to 'dotnet run'");
                    psi = new ProcessStartInfo
                    {
                        FileName = "dotnet",
                        Arguments = $"run --project \"{projectPath}\" -c Debug",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                    };
                }

                if (psi is null)
                {
                    var shadowDll = Path.Combine(shadowDir, "Lazarus.Orchestrator.Host.dll");
                    if (!File.Exists(shadowDll))
                    {
                        _logger.LogWarning("Shadow DLL not found at {Path}; using 'dotnet run' fallback", shadowDll);
                        psi = new ProcessStartInfo
                        {
                            FileName = "dotnet",
                            Arguments = $"run --project \"{projectPath}\" -c Debug",
                            UseShellExecute = false,
                            CreateNoWindow = true,
                        };
                    }
                    else
                    {
                        psi = new ProcessStartInfo
                        {
                            FileName = "dotnet",
                            Arguments = $"\"{shadowDll}\"",
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            WorkingDirectory = shadowDir,
                        };
                    }
                }
            }
#else
            // RELEASE: try App.Orchestrator.Host.exe next to the app, then in \App.Orchestrator.Host\
            var exe = TryResolveOrchestratorExePath();
            if (exe is null)
            {
                _logger.LogInformation("Orchestrator executable not found near app; skipping auto-start");
                return;
            }
            psi = new ProcessStartInfo
            {
                FileName = exe,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(exe) ?? AppContext.BaseDirectory,
            };
#endif

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

    public Task StopIfOwnedAsync(CancellationToken cancellationToken)
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

    private static void MirrorDirectory(string sourceDir, string destDir)
    {
        foreach (var dir in Directory.EnumerateDirectories(sourceDir, "*", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(sourceDir, dir);
            var target = Path.Combine(destDir, rel);
            Directory.CreateDirectory(target);
        }

        foreach (var file in Directory.EnumerateFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(sourceDir, file);
            var target = Path.Combine(destDir, rel);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            try
            {
                File.Copy(file, target, overwrite: true);
            }
            catch
            {
                // Best-effort; if a file copy fails, continue. Shadow copy is only to avoid locks.
            }
        }
    }
#if !DEBUG
    private static string? TryResolveOrchestratorExePath()
    {
        var baseDir = AppContext.BaseDirectory;
        var c1 = Path.Combine(baseDir, "App.Orchestrator.Host.exe");
        if (File.Exists(c1)) return c1;

        var c2 = Path.Combine(baseDir, "App.Orchestrator.Host", "App.Orchestrator.Host.exe");
        if (File.Exists(c2)) return c2;

        return null;
    }
#endif
}
