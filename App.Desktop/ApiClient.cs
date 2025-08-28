using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using Lazarus.Shared.OpenAI;
using Lazarus.Shared.Models;
using System.Diagnostics;

namespace Lazarus.Desktop;

public static class ApiClient
{
    private static readonly HttpClient Http = new();
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private const string BaseUrl = "http://127.0.0.1:11711";

    static ApiClient()
    {
        Http.BaseAddress = new Uri(BaseUrl);
        Http.Timeout = TimeSpan.FromSeconds(30);
    }

    public static async Task<bool> HealthAsync()
    {
        try
        {
            using var resp = await Http.GetAsync("/status").ConfigureAwait(false);
            return resp.IsSuccessStatusCode;
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"[ApiClient] Network error in HealthAsync: {ex.Message}");
            return false;
        }
        catch (TaskCanceledException ex)
        {
            Console.WriteLine($"[ApiClient] Timeout in HealthAsync: {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ApiClient] Unexpected error in HealthAsync: {ex.Message}");
            return false;
        }
    }

    public static async Task<ChatCompletionResponse?> ChatCompletionAsync(ChatCompletionRequest request)
    {
        try
        {
            Console.WriteLine($"[ApiClient] Sending chat request to {BaseUrl}/v1/chat/completions");
            Console.WriteLine($"[ApiClient] Request model: {request.Model}");
            Console.WriteLine($"[ApiClient] Request messages count: {request.Messages?.Count ?? 0}");
            
            using var resp = await Http.PostAsJsonAsync("/v1/chat/completions", request, Json);
            Console.WriteLine($"[ApiClient] Response status: {resp.StatusCode}");
            
            if (!resp.IsSuccessStatusCode)
            {
                var errorContent = await resp.Content.ReadAsStringAsync();
                Console.WriteLine($"[ApiClient] Error response: {errorContent}");
                resp.EnsureSuccessStatusCode();
            }
            
            var response = await resp.Content.ReadFromJsonAsync<ChatCompletionResponse>(Json);
            Console.WriteLine($"[ApiClient] Response received, choices count: {response?.Choices?.Count ?? 0}");
            return response;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ApiClient] ChatCompletionAsync exception: {ex.GetType().Name}: {ex.Message}");
            Console.WriteLine($"[ApiClient] Stack trace: {ex.StackTrace}");
            return null;
        }
    }


    public static async Task<SystemInfo?> GetSystemInfoAsync()
    {
        try
        {
            using var resp = await Http.GetAsync("/v1/system").ConfigureAwait(false);
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadFromJsonAsync<SystemInfo>(Json);
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"[ApiClient] Network error in GetSystemInfoAsync: {ex.Message}");
            return null;
        }
        catch (TaskCanceledException ex)
        {
            Console.WriteLine($"[ApiClient] Timeout in GetSystemInfoAsync: {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ApiClient] Unexpected error in GetSystemInfoAsync: {ex.Message}");
            return null;
        }
    }

    public static async Task<ModelInventoryResponse?> GetAvailableModelsAsync()
    {
        try
        {
            using var resp = await Http.GetAsync("/v1/models/available").ConfigureAwait(false);
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadFromJsonAsync<ModelInventoryResponse>(Json);
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"[ApiClient] Network error in GetAvailableModelsAsync: {ex.Message}");
            return null;
        }
        catch (TaskCanceledException ex)
        {
            Console.WriteLine($"[ApiClient] Timeout in GetAvailableModelsAsync: {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ApiClient] Unexpected error in GetAvailableModelsAsync: {ex.Message}");
            return null;
        }
    }

    public static async Task<RunnerInfo?> GetRunnersAsync()
    {
        try
        {
            using var resp = await Http.GetAsync("/v1/runners").ConfigureAwait(false);
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadFromJsonAsync<RunnerInfo>(Json);
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"[ApiClient] Network error in GetRunnersAsync: {ex.Message}");
            return null;
        }
        catch (TaskCanceledException ex)
        {
            Console.WriteLine($"[ApiClient] Timeout in GetRunnersAsync: {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ApiClient] Unexpected error in GetRunnersAsync: {ex.Message}");
            return null;
        }
    }

    public static async Task<bool> SwitchRunnerAsync(string type, string baseUrl, string? model = null)
    {
        try
        {
            var request = new { Type = type, BaseUrl = baseUrl, Model = model };
            using var resp = await Http.PostAsJsonAsync("/v1/runners/switch", request, Json).ConfigureAwait(false);
            return resp.IsSuccessStatusCode;
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"[ApiClient] Network error in SwitchRunnerAsync: {ex.Message}");
            return false;
        }
        catch (TaskCanceledException ex)
        {
            Console.WriteLine($"[ApiClient] Timeout in SwitchRunnerAsync: {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ApiClient] Unexpected error in SwitchRunnerAsync: {ex.Message}");
            return false;
        }
    }

    public static async Task<bool> LoadModelAsync(string modelPath, string? name = null)
    {
        try
        {
            var request = new { ModelPath = modelPath, Name = name };
            using var resp = await Http.PostAsJsonAsync("/v1/models/load", request, Json).ConfigureAwait(false);
            return resp.IsSuccessStatusCode;
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"[ApiClient] Network error in LoadModelAsync: {ex.Message}");
            return false;
        }
        catch (TaskCanceledException ex)
        {
            Console.WriteLine($"[ApiClient] Timeout in LoadModelAsync: {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ApiClient] Unexpected error in LoadModelAsync: {ex.Message}");
            return false;
        }
    }

    public static async Task<ModelCapabilities?> GetModelCapabilitiesAsync(string modelName = "current")
    {
        try
        {
            Console.WriteLine($"[ApiClient] Fetching capabilities for model: {modelName}");
            using var resp = await Http.GetAsync($"/v1/models/{modelName}/capabilities");
            resp.EnsureSuccessStatusCode();
            
            var capabilities = await resp.Content.ReadFromJsonAsync<ModelCapabilities>(Json);
            Console.WriteLine($"[ApiClient] Capabilities received: {capabilities?.AvailableParameters.Count ?? 0} parameters");
            return capabilities;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ApiClient] GetModelCapabilities failed: {ex.GetType().Name}: {ex.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Get model capabilities with LoRA modifications applied
    /// </summary>
    public static async Task<ModelCapabilities?> GetModelCapabilitiesWithLoRAsAsync(string modelName = "current", List<AppliedLoRAInfo>? appliedLoRAs = null)
    {
        try
        {
            appliedLoRAs ??= new List<AppliedLoRAInfo>();
            Console.WriteLine($"[ApiClient] Fetching LoRA-aware capabilities for model: {modelName} with {appliedLoRAs.Count} LoRAs");
            
            using var resp = await Http.PostAsJsonAsync($"/v1/models/{modelName}/capabilities/with-loras", appliedLoRAs, Json);
            resp.EnsureSuccessStatusCode();
            
            var capabilities = await resp.Content.ReadFromJsonAsync<ModelCapabilities>(Json);
            Console.WriteLine($"[ApiClient] LoRA-aware capabilities received: {capabilities?.AvailableParameters.Count ?? 0} parameters, {capabilities?.AppliedLoRAs.Count ?? 0} LoRAs");
            return capabilities;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ApiClient] GetModelCapabilitiesWithLoRAs failed: {ex.GetType().Name}: {ex.Message}");
            // Fallback to basic capabilities
            return await GetModelCapabilitiesAsync(modelName);
        }
    }
    
    /// <summary>
    /// Apply a LoRA to the model
    /// </summary>
    public static async Task<bool> ApplyLoRAAsync(AppliedLoRAInfo loraInfo)
    {
        try
        {
            Console.WriteLine($"[ApiClient] Applying LoRA: {loraInfo.Name}");
            using var resp = await Http.PostAsJsonAsync("/v1/loras/apply", loraInfo, Json);
            return resp.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ApiClient] ApplyLoRA failed: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Remove a LoRA from the model
    /// </summary>
    public static async Task<bool> RemoveLoRAAsync(string loraId)
    {
        try
        {
            Console.WriteLine($"[ApiClient] Removing LoRA: {loraId}");
            using var resp = await Http.DeleteAsync($"/v1/loras/{loraId}");
            return resp.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ApiClient] RemoveLoRA failed: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Get currently applied LoRAs
    /// </summary>
    public static async Task<List<AppliedLoRAInfo>?> GetAppliedLoRAsAsync()
    {
        try
        {
            using var resp = await Http.GetAsync("/v1/loras/applied");
            resp.EnsureSuccessStatusCode();
            
            return await resp.Content.ReadFromJsonAsync<List<AppliedLoRAInfo>>(Json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ApiClient] GetAppliedLoRAs failed: {ex.Message}");
            return new List<AppliedLoRAInfo>();
        }
    }
    
    /// <summary>
    /// Clear all applied LoRAs
    /// </summary>
    public static async Task<bool> ClearAllLoRAsAsync()
    {
        try
        {
            Console.WriteLine($"[ApiClient] Clearing all LoRAs");
            using var resp = await Http.DeleteAsync("/v1/loras");
            return resp.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ApiClient] ClearAllLoRAs failed: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Get comprehensive system status for SystemStateViewModel
    /// </summary>
    public static async Task<SystemStatusResponse?> GetSystemStatusAsync()
    {
        try
        {
            // Use the ACTUAL /status endpoint that exists
            using var statusResp = await Http.GetAsync("/status");
            if (!statusResp.IsSuccessStatusCode) return null;
            
            var statusJson = await statusResp.Content.ReadAsStringAsync();
            var statusData = JsonSerializer.Deserialize<JsonElement>(statusJson, Json);
            
            // Extract data from the actual orchestrator response
            var runner = statusData.GetProperty("runner");
            var runnerName = runner.GetProperty("name").GetString();
            var runnerHealthy = runner.GetProperty("healthy").GetBoolean();
            
            // Get additional system info from /v1/system if available
            string? gpuName = null;
            int? vramUsedMB = null;
            int? vramTotalMB = null;
            
            try
            {
                using var systemResp = await Http.GetAsync("/v1/system");
                if (systemResp.IsSuccessStatusCode)
                {
                    var systemJson = await systemResp.Content.ReadAsStringAsync();
                    var systemData = JsonSerializer.Deserialize<JsonElement>(systemJson, Json);
                    
                    if (systemData.TryGetProperty("Gpu", out var gpu))
                    {
                        vramTotalMB = (int?)(gpu.GetProperty("Total").GetInt64() / (1024 * 1024));
                        vramUsedMB = (int?)(gpu.GetProperty("Used").GetInt64() / (1024 * 1024));
                    }
                }
            }
            catch
            {
                // System endpoint failed, continue with basic status
            }
            
            return new SystemStatusResponse
            {
                LoadedModel = runnerHealthy ? runnerName : "No model loaded",
                ActiveRunner = runnerName ?? "No runner",
                ServerPort = 11711,
                GpuName = gpuName,
                VramUsedMB = vramUsedMB,
                VramTotalMB = vramTotalMB,
                QueuedJobs = 0 // TODO: Add job queue tracking
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ApiClient] GetSystemStatus failed: {ex.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Unload currently loaded model
    /// </summary>
    public static async Task<bool> UnloadModelAsync()
    {
        try
        {
            using var resp = await Http.DeleteAsync("/v1/models/current").ConfigureAwait(false);
            return resp.IsSuccessStatusCode;
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"[ApiClient] Network error in UnloadModelAsync: {ex.Message}");
            return false;
        }
        catch (TaskCanceledException ex)
        {
            Console.WriteLine($"[ApiClient] Timeout in UnloadModelAsync: {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ApiClient] Unexpected error in UnloadModelAsync: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Restart the active runner
    /// </summary>
    public static async Task<bool> RestartRunnerAsync()
    {
        try
        {
            using var resp = await Http.PostAsync("/v1/runners/restart", null).ConfigureAwait(false);
            return resp.IsSuccessStatusCode;
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"[ApiClient] Network error in RestartRunnerAsync: {ex.Message}");
            return false;
        }
        catch (TaskCanceledException ex)
        {
            Console.WriteLine($"[ApiClient] Timeout in RestartRunnerAsync: {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ApiClient] Unexpected error in RestartRunnerAsync: {ex.Message}");
            return false;
        }
    }
}

// Response models
public record SystemInfo
{
    public MemoryInfo Ram { get; init; } = new();
    public GpuInfo Gpu { get; init; } = new();
}

public record MemoryInfo
{
    public long Total { get; init; }
    public long Used { get; init; }
    public long Available { get; init; }
    public double UsagePercent { get; init; }
    public string TotalFormatted { get; init; } = "";
    public string UsedFormatted { get; init; } = "";
}

public record GpuInfo
{
    public long Total { get; init; }
    public long Used { get; init; }
    public long Available { get; init; }
    public double UsagePercent { get; init; }
    public string TotalFormatted { get; init; } = "";
    public string UsedFormatted { get; init; } = "";
    public int Temperature { get; init; }
    public int PowerDraw { get; init; }
}

public record ModelInventoryResponse
{
    public BaseModelDto[] BaseModels { get; init; } = Array.Empty<BaseModelDto>();
    public LoraDto[] Loras { get; init; } = Array.Empty<LoraDto>();
    public VaeDto[] Vaes { get; init; } = Array.Empty<VaeDto>();
    public EmbeddingDto[] Embeddings { get; init; } = Array.Empty<EmbeddingDto>();
    public HypernetworkDto[] Hypernetworks { get; init; } = Array.Empty<HypernetworkDto>();
}

public record LoraDto
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string FileName { get; init; } = "";
    public string Size { get; init; } = "";
    public string Type { get; init; } = "";
    public int Rank { get; init; }
    public int Alpha { get; init; }
}

public record VaeDto
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string FileName { get; init; } = "";
    public string Size { get; init; } = "";
    public string Type { get; init; } = "";
    public string Compatibility { get; init; } = "";
}

public record EmbeddingDto
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string FileName { get; init; } = "";
    public string Size { get; init; } = "";
    public string Type { get; init; } = "";
    public string Keyword { get; init; } = "";
    public int Vectors { get; init; }
}

public record HypernetworkDto
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string FileName { get; init; } = "";
    public string Size { get; init; } = "";
    public string Architecture { get; init; } = "";
    public int TrainingSteps { get; init; }
}

public record RunnerInfo
{
    public string? Active { get; init; }
    public RunnerOption[] Available { get; init; } = Array.Empty<RunnerOption>();
}

public record RunnerOption
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string Description { get; init; } = "";
}

/// <summary>
/// Comprehensive system status for the Context Bar
/// </summary>
public record SystemStatusResponse
{
    public string? LoadedModel { get; init; }
    public string? ActiveRunner { get; init; }
    public string? GpuName { get; init; }
    public int? ContextLength { get; init; }
    public double? TokensPerSecond { get; init; }
    public int? ServerPort { get; init; }
    public int? VramUsedMB { get; init; }
    public int? VramTotalMB { get; init; }
    public int? QueuedJobs { get; init; }
}