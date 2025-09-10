using Lazarus.Shared.Enums;

namespace Lazarus.Backend.Runners;

public sealed class RunnerDescriptor
{
    public required string Id { get; init; }
    public required RunnerKind Kind { get; init; }
    public required string BaseUrl { get; init; }
    public string? Provider { get; init; }
    public string? DisplayName { get; init; }
}

