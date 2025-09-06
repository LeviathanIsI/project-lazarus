namespace Lazarus.Desktop.Configuration;

/// <summary>
/// Configuration options for binary validation during application startup.
/// </summary>
public sealed class BinaryValidationOptions
{
    public const string SectionName = "BinaryValidation";

    /// <summary>
    /// Root path to the binaries directory.
    /// </summary>
    public string BinariesPath { get; set; } = @"D:\project-lazarus\binaries";

    /// <summary>
    /// Path to the runners directory relative to BinariesPath.
    /// </summary>
    public string RunnersPath { get; set; } = "runners";

    /// <summary>
    /// Expected runner executable name for llama.cpp.
    /// </summary>
    public string LlamaServerExecutable { get; set; } = "llama-server.exe";

    /// <summary>
    /// Expected llama runner directory name.
    /// </summary>
    public string LlamaRunnerDirectory { get; set; } = "llama-b6394-bin-win-cuda-12.4-x64";

    /// <summary>
    /// Timeout for binary validation checks during startup.
    /// </summary>
    public TimeSpan ValidationTimeout { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Whether to perform CUDA driver availability check.
    /// </summary>
    public bool CheckCudaDriver { get; set; } = true;
}