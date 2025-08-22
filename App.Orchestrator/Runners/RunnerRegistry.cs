namespace Lazarus.Orchestrator.Runners;

public static class RunnerRegistry
{
    public static IChatRunner? Active { get; private set; }
    private static string? _currentModel;

    public static void InitializeFromEnv()
    {
        var url = Environment.GetEnvironmentVariable("LAZARUS_RUNNER_URL");
        if (string.IsNullOrWhiteSpace(url)) return;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return;

        // Optional controls
        var kind = (Environment.GetEnvironmentVariable("LAZARUS_RUNNER_KIND") ?? "llama-server").ToLowerInvariant();
        var name = Environment.GetEnvironmentVariable("LAZARUS_RUNNER_NAME") ?? "llama-server";
        var model = Environment.GetEnvironmentVariable("LAZARUS_RUNNER_MODEL"); // optional

        // Choose runner explicitly (default to llama-server since you already run it)
        Active = kind switch
        {
            "llama-server" => new LlamaServerRunner(name, uri, model),
            // keep your existing process-managed runner available if you want it:
            "llama-cpp" => new LlamaCppRunner(uri), // your existing class
            _ => new LlamaServerRunner(name, uri, model),
        };

        _currentModel = model;
    }

    public static void SwitchTo(string runnerType, string baseUrl, string? model = null)
    {
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri))
            throw new ArgumentException($"Invalid base URL: {baseUrl}");

        Active = runnerType.ToLowerInvariant() switch
        {
            "llama-server" => new LlamaServerRunner(runnerType, uri, model),
            "llama-cpp" => new LlamaCppRunner(uri),
            _ => throw new ArgumentException($"Unknown runner type: {runnerType}")
        };

        _currentModel = model;
    }

    public static bool LoadModel(string modelPath)
    {
        try
        {
            // Extract model name from path
            var modelName = Path.GetFileNameWithoutExtension(modelPath);

            // For now, just update the model reference
            // In a real implementation, you'd restart the runner with the new model
            _currentModel = modelName;

            // If using LlamaServerRunner, you might need to restart the external process
            // with the new model path. This would require additional coordination.

            return true;
        }
        catch
        {
            return false;
        }
    }

    public static string? GetCurrentModel() => _currentModel;

    public static async Task<bool> IsHealthyAsync()
    {
        return Active != null && await Active.HealthAsync();
    }

    public static RunnerStatus GetStatus()
    {
        return new RunnerStatus
        {
            HasActiveRunner = Active != null,
            RunnerType = Active?.Name,
            BaseUrl = Active?.BaseAddress.ToString(),
            CurrentModel = _currentModel,
            IsHealthy = Active != null ? Task.Run(() => Active.HealthAsync()).Result : false
        };
    }
}

public record RunnerStatus
{
    public bool HasActiveRunner { get; init; }
    public string? RunnerType { get; init; }
    public string? BaseUrl { get; init; }
    public string? CurrentModel { get; init; }
    public bool IsHealthy { get; init; }
}