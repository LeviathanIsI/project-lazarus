using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace Lazarus.Shared;

/// <summary>
/// Detects model formats from file paths and directories.
/// Provides extensible detection rules for various ML model formats.
/// </summary>
public static class ModelDetector
{
    /// <summary>
    /// Registry of file extension to format mappings for easy extensibility.
    /// Add new entries here to support additional model formats.
    /// </summary>
    private static readonly Dictionary<string, ModelDetectionResult> FileExtensionMappings = new(StringComparer.OrdinalIgnoreCase)
    {
        // GGUF format (llama.cpp)
        { ".gguf", new ModelDetectionResult(ModelFormat.GGUF, RunnerKind.LlamaCpp) },

        // PyTorch formats (vLLM/ExLlamaV2)
        { ".bin", new ModelDetectionResult(ModelFormat.HF, RunnerKind.Vllm) },
        { ".pth", new ModelDetectionResult(ModelFormat.HF, RunnerKind.Vllm) },

        // SafeTensors format (vLLM/ExLlamaV2)
        { ".safetensors", new ModelDetectionResult(ModelFormat.HF, RunnerKind.Vllm) },

        // Future formats - add here for extensibility
        { ".onnx", new ModelDetectionResult(ModelFormat.ONNX, RunnerKind.Vllm) }, // vLLM supports ONNX
        { ".tflite", new ModelDetectionResult(ModelFormat.TFLite, RunnerKind.Unknown) },

        // Add new formats here:
        // { ".your_extension", new ModelDetectionResult(ModelFormat.YourFormat, RunnerKind.YourRunner) }
    };

    /// <summary>
    /// Registry of directory detection functions for extensible directory-based format detection.
    /// Add new detection functions here to support additional directory-based model formats.
    /// </summary>
    private static readonly List<Func<string, ModelDetectionResult?>> DirectoryDetectors = new()
    {
        DetectHuggingFaceDirectory,
        // Add new directory detectors here:
        // YourDirectoryDetectorFunction
    };

    /// <summary>
    /// Detects the model format and preferred runner for a given path.
    /// </summary>
    /// <param name="path">File or directory path to analyze</param>
    /// <returns>Detection result with format and runner, or null if unrecognized</returns>
    public static ModelDetectionResult? DetectFormat(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        try
        {
            if (File.Exists(path))
                return DetectFromFile(path);
            else if (Directory.Exists(path))
                return DetectFromDirectory(path);
        }
        catch (Exception ex)
        {
            // Log warning but don't throw - return null for unrecognized format
            Console.WriteLine($"Warning: Failed to detect model format for {path}: {ex.Message}");
        }

        return null;
    }

    private static ModelDetectionResult? DetectFromFile(string filePath)
    {
        var extension = Path.GetExtension(filePath);

        // Use the extensible mapping registry
        if (FileExtensionMappings.TryGetValue(extension, out var result))
            return result;

        // Fallback to unrecognized
        return null;
    }

    private static ModelDetectionResult? DetectFromDirectory(string dirPath)
    {
        // Try each directory detector in order
        foreach (var detector in DirectoryDetectors)
        {
            var result = detector(dirPath);
            if (result != null)
                return result;
        }

        return null;
    }

    /// <summary>
    /// Detects HuggingFace model directories by looking for characteristic files.
    /// This is an extensible directory detector that can be easily modified or extended.
    /// </summary>
    private static ModelDetectionResult? DetectHuggingFaceDirectory(string dirPath)
    {
        try
        {
            // Primary indicator: config.json file
            if (File.Exists(Path.Combine(dirPath, "config.json")))
                return new ModelDetectionResult(ModelFormat.HF, RunnerKind.Vllm);

            // Alternative: tokenizer.json + model files
            if (File.Exists(Path.Combine(dirPath, "tokenizer.json")))
            {
                // Look for model files
                var modelFiles = Directory.GetFiles(dirPath, "*.*", SearchOption.TopDirectoryOnly)
                    .Select(f => Path.GetExtension(f).ToLowerInvariant())
                    .ToArray();

                // Check for common HF model file extensions
                if (modelFiles.Contains(".safetensors") ||
                    modelFiles.Contains(".bin") ||
                    modelFiles.Contains(".pth") ||
                    modelFiles.Contains(".model") ||
                    File.Exists(Path.Combine(dirPath, "pytorch_model.bin")) ||
                    File.Exists(Path.Combine(dirPath, "tf_model.h5")))
                {
                    return new ModelDetectionResult(ModelFormat.HF, RunnerKind.Vllm);
                }
            }

            // Check for model.safetensors.index.json (sharded models)
            if (File.Exists(Path.Combine(dirPath, "model.safetensors.index.json")))
                return new ModelDetectionResult(ModelFormat.HF, RunnerKind.Vllm);

            // Check for pytorch_model.bin.index.json (legacy sharded models)
            if (File.Exists(Path.Combine(dirPath, "pytorch_model.bin.index.json")))
                return new ModelDetectionResult(ModelFormat.HF, RunnerKind.Vllm);
        }
        catch
        {
            // Directory access issues - treat as not detected
        }

        return null;
    }
}

/// <summary>
/// Result of model format detection.
/// </summary>
public sealed record ModelDetectionResult(ModelFormat Format, RunnerKind PreferredRunner);

// Note: ModelFormat and RunnerKind enums are defined in ModelArtifacts.cs
// These are the canonical definitions used throughout the application
