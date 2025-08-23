using System.Text.Json.Serialization;

namespace Lazarus.Orchestrator.Configuration;

public class RunnerConfiguration
{
    public string Type { get; set; } = "llama-server";
    public string Name { get; set; } = "Default Runner";
    public string BaseUrl { get; set; } = "http://127.0.0.1:8080";
    public string? DefaultModel { get; set; }
    public int TimeoutSeconds { get; set; } = 300;
    public int MaxRetries { get; set; } = 3;
    public int RetryDelayMs { get; set; } = 1000;
    public bool EnableHealthCheck { get; set; } = true;
    public int HealthCheckIntervalSeconds { get; set; } = 30;
}

public class OrchestratorConfiguration
{
    public RunnerConfiguration[] Runners { get; set; } = Array.Empty<RunnerConfiguration>();
    public string DefaultRunner { get; set; } = "";
    public bool AllowFallbackEcho { get; set; } = false;
    public LogLevel LogLevel { get; set; } = LogLevel.Information;
}

public enum LogLevel
{
    Debug,
    Information,
    Warning,
    Error
}