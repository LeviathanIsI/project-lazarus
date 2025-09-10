namespace Lazarus.Shared.Images;

public sealed class ImageGenRequest
{
    public string Prompt { get; set; } = string.Empty;
    public string NegativePrompt { get; set; } = string.Empty;

    // Path to diffusion checkpoint (.safetensors | .ckpt | .onnx)
    public string ModelPath { get; set; } = string.Empty;

    // Selected Image runner from UI
    public string? RunnerId { get; set; }

    public int Seed { get; set; }
    public string Sampler { get; set; } = "Euler";
    public int Steps { get; set; } = 30;
    public double Cfg { get; set; } = 7.0;

    // "txt2img" | "img2img" | "inpaint"
    public string Mode { get; set; } = "txt2img";

    // Optional sources for img2img/inpaint
    public string? InitImagePath { get; set; }
    public string? MaskImagePath { get; set; }
    public double? Strength { get; set; }

    // Optional ControlNet input image (single) for basic pipelines
    public string? ControlNetImagePath { get; set; }
}
