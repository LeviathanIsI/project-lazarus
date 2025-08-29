using Lazarus.Shared.OpenAI;

namespace Lazarus.Orchestrator.Runners;

public interface IChatRunner
{
    string Name { get; }
    Uri BaseAddress { get; }
    string? CurrentModel { get; }
    Task<bool> HealthAsync(CancellationToken ct = default);
    Task<ChatCompletionResponse> ChatAsync(ChatCompletionRequest req, CancellationToken ct = default);
    Task<bool> UnloadAsync(CancellationToken ct = default);
}
