using Lazarus.Shared.Enums;

namespace Lazarus.Backend.Runners;

public interface IRunnerRegistry
{
    RunnerDescriptor? GetById(string id);
    RunnerDescriptor? GetDefault(RunnerKind kind);
}

