using System.Net.Http.Json;
using System.Text.Json;
using Lazarus.Shared.OpenAI;

namespace Lazarus.Orchestrator.Runners;

public sealed class LlamaServerRunner : IChatRunner
{
    private readonly HttpClient _http;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);
    public string Name { get; }
    public Uri BaseAddress { get; }
    private readonly string _defaultModel;

    public LlamaServerRunner(string name, Uri baseUrl, string? defaultModel = null, HttpMessageHandler? handler = null)
    {
        Name = string.IsNullOrWhiteSpace(name) ? "llama-server" : name;
        BaseAddress = baseUrl;
        _defaultModel = string.IsNullOrWhiteSpace(defaultModel)
            ? "Qwen2.5-32B-Instruct-Q5_K_M.gguf"
            : defaultModel;

        _http = handler is null ? new HttpClient() : new HttpClient(handler);
        _http.BaseAddress = BaseAddress;
        _http.Timeout = TimeSpan.FromMinutes(5);

        Console.WriteLine($"[LlamaServerRunner] Initialized with base URL: {BaseAddress}, default model: {_defaultModel}");
    }

    public async Task<bool> HealthAsync(CancellationToken ct = default)
    {
        try
        {
            var healthUris = new[] { "/health", "/v1/models", "../health" };

            foreach (var healthPath in healthUris)
            {
                try
                {
                    Console.WriteLine($"[LlamaServerRunner] Health check attempt: {BaseAddress}{healthPath}");
                    using var resp = await _http.GetAsync(healthPath, ct);
                    if (resp.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"[LlamaServerRunner] Health check successful via {healthPath}");
                        return true;
                    }
                    Console.WriteLine($"[LlamaServerRunner] Health check failed via {healthPath}: {resp.StatusCode}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[LlamaServerRunner] Health check exception via {healthPath}: {ex.Message}");
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LlamaServerRunner] Health check completely failed: {ex.Message}");
            return false;
        }
    }

    public async Task<ChatCompletionResponse> ChatAsync(ChatCompletionRequest payload, CancellationToken ct = default)
    {
        try
        {
            payload.Model ??= _defaultModel;

            Console.WriteLine($"[LlamaServerRunner] Sending chat request to {BaseAddress}chat/completions");
            Console.WriteLine($"[LlamaServerRunner] Request model: {payload.Model}");
            Console.WriteLine($"[LlamaServerRunner] Request payload: {JsonSerializer.Serialize(payload, _json)}");

            using var resp = await _http.PostAsJsonAsync("chat/completions", payload, _json, ct);

            var statusCode = resp.StatusCode;
            var responseContent = await resp.Content.ReadAsStringAsync(ct);

            Console.WriteLine($"[LlamaServerRunner] Response status: {statusCode}");
            Console.WriteLine($"[LlamaServerRunner] Response body: {responseContent}");

            if (!resp.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"llama-server returned {statusCode}: {responseContent}");
            }

            var body = JsonSerializer.Deserialize<ChatCompletionResponse>(responseContent, _json);
            if (body == null)
            {
                throw new InvalidOperationException("llama-server returned null response after deserialization");
            }

            Console.WriteLine($"[LlamaServerRunner] Successfully parsed response with {body.Choices.Count} choices");
            return body;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LlamaServerRunner] ChatAsync failed: {ex.Message}");
            Console.WriteLine($"[LlamaServerRunner] Stack trace: {ex.StackTrace}");
            throw;
        }
    }
}