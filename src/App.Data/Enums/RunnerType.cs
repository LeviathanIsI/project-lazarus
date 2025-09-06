namespace Lazarus.Data.Enums;

/// <summary>
/// Represents the type of inference engine runner.
/// </summary>
public enum RunnerType
{
    /// <summary>
    /// llama.cpp CPU/GPU inference with llama-server.exe.
    /// </summary>
    LlamaCpp = 0,

    /// <summary>
    /// Python-based GPU inference server.
    /// </summary>
    VLlm = 1,

    /// <summary>
    /// Optimized CUDA inference engine.
    /// </summary>
    ExLlamaV2 = 2,

    /// <summary>
    /// Containerized model serving.
    /// </summary>
    Ollama = 3
}