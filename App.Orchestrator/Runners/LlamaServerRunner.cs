// src/App.Orchestrator/Runners/LlamaServerRunner.cs
using System.Net.Http.Json;
using Lazarus.Shared.OpenAI;

namespace Lazarus.Orchestrator.Runners;

public sealed class LlamaServerRunner : IChatRunner
{
    private readonly HttpClient _http;
    public string Name { get; }
    public Uri BaseAddress { get; }
    private readonly string _defaultModel;

    // baseUrl should be the OpenAI-compatible root, e.g. "http://127.0.0.1:8080/v1"
    public LlamaServerRunner(string name, Uri baseUrl, string? defaultModel = null, HttpMessageHandler? handler = null)
    {
        Name = string.IsNullOrWhiteSpace(name) ? "llama-server" : name;
        BaseAddress = baseUrl;
        _defaultModel = string.IsNullOrWhiteSpace(defaultModel)
            ? "Qwen2.5-32B-Instruct-Q5_K_M.gguf"
            : defaultModel;

        _http = handler is null ? new HttpClient() : new HttpClient(handler);
        _http.BaseAddress = BaseAddress; // NOTE: this should end with /v1
    }

    public async Task<bool> HealthAsync(CancellationToken ct = default)
    {
        try
        {
            // llama-server exposes /health at the server root (no /v1)
            // If BaseAddress ends with /v1, go one level up.
            var healthUri = BaseAddress.AbsolutePath.EndsWith("/v1", StringComparison.OrdinalIgnoreCase)
                ? new Uri(new Uri(BaseAddress, "."), "/health")
                : new Uri(BaseAddress, "/health");

            using var resp = await _http.GetAsync(healthUri, ct);
            return resp.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<ChatCompletionResponse> ChatAsync(ChatCompletionRequest payload, CancellationToken ct = default)
    {
        payload.Model ??= _defaultModel;

        // BaseAddress is .../v1, so use relative "chat/completions" (no leading slash)
        using var resp = await _http.PostAsJsonAsync("chat/completions", payload, cancellationToken: ct);
        resp.EnsureSuccessStatusCode();

        var body = await resp.Content.ReadFromJsonAsync<ChatCompletionResponse>(cancellationToken: ct);
        return body ?? throw new InvalidOperationException("llama-server returned an empty body");
    }
}
