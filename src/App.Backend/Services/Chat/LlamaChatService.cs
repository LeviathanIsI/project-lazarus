using System.Text;
using System.Text.Json;
using Lazarus.Backend.Runners;
using Lazarus.Shared.Contracts.Chat;
using Lazarus.Shared.Enums;

namespace Lazarus.Backend.Services.Chat;

public interface IChatService
{
    IAsyncEnumerable<string> StreamAsync(ChatRequest request, CancellationToken ct);
}

public sealed class LlamaChatService : IChatService
{
    private readonly IRunnerRegistry _registry;
    private readonly HttpClient _http;

    public LlamaChatService(IRunnerRegistry registry, HttpClient http)
    {
        _registry = registry;
        _http = http;
    }

    public async IAsyncEnumerable<string> StreamAsync(ChatRequest request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        var runner = _registry.GetDefault(RunnerKind.ChatLlm)
                     ?? throw new InvalidOperationException("No default ChatLlm runner configured.");
        var client = _http;

        var url = $"{runner.BaseUrl.TrimEnd('/')}/v1/chat/completions";
        var payload = new
        {
            model = request.Model,
            messages = request.Messages.Select(m => new { role = InferRole(m), content = m }).ToArray(),
            stream = request.Stream
        };
        using var req = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };

        using var res = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
        res.EnsureSuccessStatusCode();

        await using var stream = await res.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync().ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (!line.StartsWith("data:", StringComparison.OrdinalIgnoreCase)) continue;
            var data = line.Substring(5).Trim();
            if (string.Equals(data, "[DONE]", StringComparison.OrdinalIgnoreCase)) yield break;
            string? deltaOut = null;
            try
            {
                using var doc = JsonDocument.Parse(data);
                deltaOut = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("delta")
                    .GetProperty("content")
                    .GetString();
            }
            catch { }
            if (!string.IsNullOrEmpty(deltaOut))
                yield return deltaOut!;
        }
    }

    private static string InferRole(string _) => "user";
}
