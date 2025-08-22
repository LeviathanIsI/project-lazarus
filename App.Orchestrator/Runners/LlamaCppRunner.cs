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

    public LlamaCppRunner(Uri baseAddress)
    {
        BaseAddress = baseAddress;
        _http = new HttpClient { BaseAddress = baseAddress };
    }

    public async Task<bool> HealthAsync(CancellationToken ct = default)
    {
        try
        {
            // Most llama.cpp builds expose OpenAI‑compat endpoints; /v1/models is a safe ping.
            using var resp = await _http.GetAsync("/v1/models", ct);
            return resp.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public async Task<ChatCompletionResponse> ChatAsync(ChatCompletionRequest req, CancellationToken ct = default)
    {
        using var resp = await _http.PostAsJsonAsync("/v1/chat/completions", req, _json, ct);
        resp.EnsureSuccessStatusCode();
        var parsed = await resp.Content.ReadFromJsonAsync<ChatCompletionResponse>(_json, ct);
        if (parsed is null) throw new InvalidOperationException("Runner returned no body.");
        return parsed;
    }
}
