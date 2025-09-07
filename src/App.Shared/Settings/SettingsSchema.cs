using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Lazarus.Shared.Settings;

/// <summary>
/// Defines the strongly-typed, versioned settings schema for Lazarus.
/// </summary>
public static class SettingsSchema
{
    public const int CurrentVersion = 1;
}

/// <summary>
/// Root application settings object (flat MVP schema).
/// </summary>
public sealed class AppSettings
{
    // Always bump when the shape changes in a breaking way.
    public int SchemaVersion { get; set; } = SettingsSchema.CurrentVersion;

    // ---- General ----
    public string? PreferredTheme { get; set; } = "Dark"; // Dark | Light | System
    public string? Language { get; set; } = "en-US";
    public bool CheckForUpdatesOnStart { get; set; } = false; // future use

    // ---- Paths ----
    public string ModelsDirectory { get; set; } = @"D:\models"; // user can change
    public string CacheDirectory { get; set; } = @"%LOCALAPPDATA%\Lazarus\cache";

    // ---- Orchestrator ----
    public string OrchestratorBaseUrl { get; set; } = "http://127.0.0.1:11711";
    public int OrchestratorStartupTimeoutSec { get; set; } = 45;

    // ---- Runner (active + per-runner opts) ----
    public string ActiveRunner { get; set; } = "llama.cpp"; // llama.cpp | vllm | exllamav2

    public LlamaCppSettings LlamaCpp { get; set; } = new();

    /// <summary>
    /// Creates default settings aligned with the MVP schema.
    /// </summary>
    public static AppSettings CreateDefault() => new();
}

/// <summary>
/// Minimal llama.cpp runner settings for MVP.
/// </summary>
public sealed class LlamaCppSettings
{
    /// <summary>
    /// Optional absolute directory containing llama-server.exe.
    /// </summary>
    public string? BinaryDir { get; set; }

    /// <summary>
    /// Preferred port (orchestrator may adjust to avoid collisions).
    /// </summary>
    public int DefaultPort { get; set; } = 11888;

    /// <summary>
    /// Startup timeout in seconds to wait for health.
    /// </summary>
    public int StartupTimeoutSec { get; set; } = 120;

    /// <summary>
    /// Health endpoint path.
    /// </summary>
    public string HealthCheckEndpoint { get; set; } = "/health";
}
