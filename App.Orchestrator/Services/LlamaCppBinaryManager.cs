using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace Lazarus.Orchestrator.Services;

public static class LlamaCppBinaryManager
{
    private static readonly ILogger Logger = LoggerFactory.Create(builder =>
        builder.AddConsole().SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Debug)
    ).CreateLogger("BinaryManager");

    public static async Task<bool> EnsureBinariesAsync()
    {
        var binariesPath = GetBinariesPath();
        var executable = GetExpectedExecutableName();
        var executablePath = Path.Combine(binariesPath, executable);

        Logger.LogInformation($"Checking for llama.cpp binary at: {executablePath}");

        if (File.Exists(executablePath))
        {
            Logger.LogInformation("llama.cpp binary found");
            return await ValidateBinaryAsync(executablePath);
        }

        Logger.LogWarning("llama.cpp binary not found - attempting to locate or download");
        return await ProvisionBinaryAsync(binariesPath, executable);
    }

    public static string GetBinariesPath()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var binariesPath = Path.Combine(baseDir, "binaries");

        if (!Directory.Exists(binariesPath))
        {
            Directory.CreateDirectory(binariesPath);
            Logger.LogInformation($"Created binaries directory: {binariesPath}");
        }

        return binariesPath;
    }

    public static string GetExpectedExecutableName()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "llama-server.exe" : "llama-server";
    }

    private static async Task<bool> ValidateBinaryAsync(string executablePath)
    {
        try
        {
            Logger.LogInformation("Validating llama.cpp binary");

            var startInfo = new ProcessStartInfo
            {
                FileName = executablePath,
                Arguments = "--help",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null) return false;

            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            var isValid = output.Contains("llama.cpp") || output.Contains("usage:");
            Logger.LogInformation($"Binary validation: {(isValid ? "PASSED" : "FAILED")}");

            return isValid;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Binary validation failed");
            return false;
        }
    }

    private static async Task<bool> ProvisionBinaryAsync(string binariesPath, string executable)
    {
        // Strategy 1: Look for system-installed llama.cpp
        var systemBinary = await FindSystemLlamaCppAsync();
        if (!string.IsNullOrEmpty(systemBinary))
        {
            var targetPath = Path.Combine(binariesPath, executable);
            File.Copy(systemBinary, targetPath, overwrite: true);
            Logger.LogInformation($"Copied system binary from: {systemBinary}");
            return true;
        }

        // Strategy 2: Download from releases (implement if needed)
        Logger.LogWarning("Auto-download not implemented yet");

        // Strategy 3: User guidance
        Logger.LogError($"""
            llama.cpp binary not found. Please:
            1. Download llama.cpp from: https://github.com/ggerganov/llama.cpp/releases
            2. Place the 'llama-server' executable in: {binariesPath}
            3. Restart the application
            """);

        return false;
    }

    private static async Task<string?> FindSystemLlamaCppAsync()
    {
        var candidates = new[]
        {
            "llama-server",
            "llama-server.exe",
            "/usr/local/bin/llama-server",
            "/opt/homebrew/bin/llama-server",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local/bin/llama-server")
        };

        foreach (var candidate in candidates)
        {
            try
            {
                var fullPath = await Task.Run(() =>
                {
                    if (Path.IsPathFullyQualified(candidate))
                    {
                        return File.Exists(candidate) ? candidate : null;
                    }

                    // Search PATH
                    var pathVar = Environment.GetEnvironmentVariable("PATH") ?? "";
                    var paths = pathVar.Split(Path.PathSeparator);

                    foreach (var path in paths)
                    {
                        var testPath = Path.Combine(path, candidate);
                        if (File.Exists(testPath)) return testPath;
                    }

                    return null;
                });

                if (fullPath != null && await ValidateBinaryAsync(fullPath))
                {
                    Logger.LogInformation($"Found system llama.cpp at: {fullPath}");
                    return fullPath;
                }
            }
            catch (Exception ex)
            {
                Logger.LogDebug(ex, $"Failed to check candidate: {candidate}");
            }
        }

        return null;
    }
}