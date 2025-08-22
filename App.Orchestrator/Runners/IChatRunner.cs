using Lazarus.Shared.OpenAI;

namespace Lazarus.Orchestrator.Runners;

public interface IChatRunner
{
    string Name { get; }
    Uri BaseAddress { get; }
    Task<bool> HealthAsync(CancellationToken ct = default);
    Task<ChatCompletionResponse> ChatAsync(ChatCompletionRequest req, CancellationToken ct = default);
}
