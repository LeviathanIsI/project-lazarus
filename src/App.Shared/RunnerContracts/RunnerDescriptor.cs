using System.Text.Json.Serialization;

namespace Lazarus.Shared.Runners;

public sealed class RunnerDescriptor
{
    public string Id { get; set; } = string.Empty;         // unique
    public string Name { get; set; } = string.Empty;       // UI display
    public string Kind { get; set; } = string.Empty;       // "llama.cpp" | "stable-diffusion" | "comfy"
    public string ExecPath { get; set; } = string.Empty;   // process path if spawnable
    public string BaseUrl { get; set; } = string.Empty;    // http://127.0.0.1:7860 or equivalent

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public RunnerRole Role { get; set; } = RunnerRole.Chat; // default for legacy records
}

