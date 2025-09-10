using Lazarus.Shared.Enums;

namespace Lazarus.Backend.Runners;

public sealed class InMemoryRunnerRegistry : IRunnerRegistry
{
    private readonly Dictionary<string, RunnerDescriptor> _map;

    public InMemoryRunnerRegistry(IEnumerable<RunnerDescriptor> runners)
    {
        _map = runners.ToDictionary(r => r.Id, StringComparer.OrdinalIgnoreCase);
    }

    public RunnerDescriptor? GetById(string id) =>
        _map.TryGetValue(id, out var r) ? r : null;

    public RunnerDescriptor? GetDefault(RunnerKind kind) =>
        _map.Values.FirstOrDefault(r => r.Kind == kind);
}

