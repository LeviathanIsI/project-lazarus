using System.ComponentModel.DataAnnotations;

namespace Lazarus.Data.Entities;

/// <summary>
/// Represents an image generation job and its persisted metadata/output.
/// </summary>
public class ImageJob
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(4000)] public string? Prompt { get; set; }
    [MaxLength(4000)] public string? NegativePrompt { get; set; }

    [MaxLength(32)] public string Mode { get; set; } = "Txt2Img"; // Txt2Img | Img2Img | Inpaint

    // Asset selections
    [MaxLength(1024)] public string? ControlNetPath { get; set; }
    [MaxLength(1024)] public string? StylePresetPath { get; set; }
    [MaxLength(1024)] public string? UpscalerPath { get; set; }
    [MaxLength(1024)] public string? VaePath { get; set; }

    // Parameters
    public int? Seed { get; set; }
    public int Steps { get; set; } = 30;
    public double CfgScale { get; set; } = 7.0;
    public int Width { get; set; } = 512;
    public int Height { get; set; } = 512;

    // Img2Img / Inpaint sources
    [MaxLength(1024)] public string? SourceImagePath { get; set; }
    [MaxLength(1024)] public string? MaskImagePath { get; set; }
    public double? Strength { get; set; }

    // Output
    [MaxLength(1024)] public string? OutputPath { get; set; }
}

