using System;
using System.Collections.Generic;
using System.Linq;
using Lazarus.Shared.Runners;

namespace Lazarus.Backend.Services.Runners;

public interface IRunnerRegistry
{
    IEnumerable<RunnerDescriptor> GetAll();
    IEnumerable<RunnerDescriptor> GetByRole(RunnerRole role);
    RunnerDescriptor? GetById(string id);
}

public sealed class RunnerRegistry : IRunnerRegistry
{
    private readonly List<RunnerDescriptor> _runners;

    public RunnerRegistry()
    {
        _runners = LoadFromSettingsOrDisk();
    }

    public IEnumerable<RunnerDescriptor> GetAll() => _runners;

    public IEnumerable<RunnerDescriptor> GetByRole(RunnerRole role) =>
        _runners.Where(r => r.Role == role);

    public RunnerDescriptor? GetById(string id) =>
        _runners.FirstOrDefault(r => string.Equals(r.Id, id, StringComparison.OrdinalIgnoreCase));

    private static List<RunnerDescriptor> LoadFromSettingsOrDisk()
    {
        // TODO: load from persisted settings or discovery. Keep placeholder here to compile.
        return new();
    }
}

