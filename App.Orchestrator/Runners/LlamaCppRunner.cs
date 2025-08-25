using System.Net.Http.Json;
using System.Text.Json;
using Lazarus.Shared.OpenAI;

namespace Lazarus.Orchestrator.Runners;

public sealed class LlamaCppRunner : IChatRunner
{
    private readonly HttpClient _http;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public string Name => "llama.cpp";
    public Uri BaseAddress { get; }
    public string? CurrentModel => null; // LlamaCppRunner doesn't track specific models

    public LlamaCppRunner(Uri baseAddress)
    {
        BaseAddress = baseAddress;
        _http = new HttpClient { BaseAddress = baseAddress, Timeout = TimeSpan.FromMinutes(5) };
    }

    public async Task<bool> HealthAsync(CancellationToken ct = default)
    {
        try
        {
            Console.WriteLine($"[LlamaCppRunner] Health check to {BaseAddress}/v1/models");
            using var resp = await _http.GetAsync("/v1/models", ct);
            var isHealthy = resp.IsSuccessStatusCode;
            Console.WriteLine($"[LlamaCppRunner] Health result: {isHealthy} ({resp.StatusCode})");
            return isHealthy;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LlamaCppRunner] Health check failed: {ex.Message}");
            return false;
        }
    }

    public async Task<ChatCompletionResponse> ChatAsync(ChatCompletionRequest req, CancellationToken ct = default)
    {
        try
        {
            Console.WriteLine($"[LlamaCppRunner] Sending request to {BaseAddress}/v1/chat/completions");
            Console.WriteLine($"[LlamaCppRunner] Request payload: {JsonSerializer.Serialize(req, _json)}");

            using var resp = await _http.PostAsJsonAsync("/v1/chat/completions", req, _json, ct);

            var statusCode = resp.StatusCode;
            var responseContent = await resp.Content.ReadAsStringAsync(ct);

            Console.WriteLine($"[LlamaCppRunner] Response status: {statusCode}");
            Console.WriteLine($"[LlamaCppRunner] Response body: {responseContent}");

            if (!resp.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"llama.cpp server returned {statusCode}: {responseContent}");
            }

            var parsed = JsonSerializer.Deserialize<ChatCompletionResponse>(responseContent, _json);
            if (parsed is null)
            {
                throw new InvalidOperationException("llama.cpp returned null response after deserialization");
            }

            Console.WriteLine($"[LlamaCppRunner] Successfully parsed response with {parsed.Choices.Count} choices");
            return parsed;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LlamaCppRunner] ChatAsync failed: {ex.Message}");
            Console.WriteLine($"[LlamaCppRunner] Stack trace: {ex.StackTrace}");
            throw;
        }
    }
}