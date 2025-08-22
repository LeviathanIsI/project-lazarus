using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using Lazarus.Shared.OpenAI;

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
            using var resp = await Http.GetAsync("/status");
            return resp.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public static async Task<ChatCompletionResponse?> ChatAsync(ChatCompletionRequest request)
    {
        try
        {
            using var resp = await Http.PostAsJsonAsync("/v1/chat/completions", request, Json);
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadFromJsonAsync<ChatCompletionResponse>(Json);
        }
        catch { return null; }
    }

    public static async Task<SystemInfo?> GetSystemInfoAsync()
    {
        try
        {
            using var resp = await Http.GetAsync("/v1/system");
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadFromJsonAsync<SystemInfo>(Json);
        }
        catch { return null; }
    }

    public static async Task<ModelInventoryResponse?> GetAvailableModelsAsync()
    {
        try
        {
            using var resp = await Http.GetAsync("/v1/models/available");
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadFromJsonAsync<ModelInventoryResponse>(Json);
        }
        catch { return null; }
    }

    public static async Task<RunnerInfo?> GetRunnersAsync()
    {
        try
        {
            using var resp = await Http.GetAsync("/v1/runners");
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadFromJsonAsync<RunnerInfo>(Json);
        }
        catch { return null; }
    }

    public static async Task<bool> SwitchRunnerAsync(string type, string baseUrl, string? model = null)
    {
        try
        {
            var request = new { Type = type, BaseUrl = baseUrl, Model = model };
            using var resp = await Http.PostAsJsonAsync("/v1/runners/switch", request, Json);
            return resp.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public static async Task<bool> LoadModelAsync(string modelPath, string? name = null)
    {
        try
        {
            var request = new { ModelPath = modelPath, Name = name };
            using var resp = await Http.PostAsJsonAsync("/v1/models/load", request, Json);
            return resp.IsSuccessStatusCode;
        }
        catch { return false; }
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

public record BaseModelDto
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string FileName { get; init; } = "";
    public string Size { get; init; } = "";
    public string Format { get; init; } = "";
    public string Architecture { get; init; } = "";
    public int ContextLength { get; init; }
    public string Quantization { get; init; } = "";
    public bool IsActive { get; init; }
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