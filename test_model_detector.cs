using System;
using System.IO;
using Lazarus.Shared;

class TestModelDetector
{
    static void Main()
    {
        Console.WriteLine("Testing ModelDetector...");

        // Test file extensions
        TestDetection("model.gguf", ModelFormat.GGUF, RunnerKind.LlamaCpp);
        TestDetection("model.bin", ModelFormat.HF, RunnerKind.Vllm);
        TestDetection("model.pth", ModelFormat.HF, RunnerKind.Vllm);
        TestDetection("model.safetensors", ModelFormat.HF, RunnerKind.Vllm);
        TestDetection("model.onnx", ModelFormat.ONNX, RunnerKind.Unknown);
        TestDetection("model.tflite", ModelFormat.TFLite, RunnerKind.Unknown);
        TestDetection("model.unknown", null, null);

        // Test directory detection (create temp directories)
        var tempDir = Path.Combine(Path.GetTempPath(), "TestModelDir");
        if (!Directory.Exists(tempDir))
        {
            Directory.CreateDirectory(tempDir);
            File.WriteAllText(Path.Combine(tempDir, "config.json"), "{}");
            TestDetection(tempDir, ModelFormat.HF, RunnerKind.Vllm);
            Directory.Delete(tempDir, true);
        }

        Console.WriteLine("ModelDetector tests completed.");
    }

    static void TestDetection(string path, ModelFormat? expectedFormat, RunnerKind? expectedRunner)
    {
        var result = ModelDetector.DetectFormat(path);
        var format = result?.Format ?? ModelFormat.Unknown;
        var runner = result?.PreferredRunner ?? RunnerKind.Unknown;

        Console.WriteLine($"Path: {path}");
        Console.WriteLine($"  Expected: Format={expectedFormat}, Runner={expectedRunner}");
        Console.WriteLine($"  Detected: Format={format}, Runner={runner}");

        if (format == expectedFormat && runner == expectedRunner)
            Console.WriteLine("  ✓ PASS");
        else
            Console.WriteLine("  ✗ FAIL");
        Console.WriteLine();
    }
}

