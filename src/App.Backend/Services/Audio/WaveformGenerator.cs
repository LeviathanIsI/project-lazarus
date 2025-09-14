using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;

namespace Lazarus.Backend.Services.Audio;

/// <summary>
/// Simple waveform PNG generator without WPF dependencies
/// </summary>
internal static class WaveformGenerator
{
    [SupportedOSPlatform("windows")]
    public static byte[] GenerateSmall(float[] samples)
    {
        return Generate(samples, 320, 40);
    }

    [SupportedOSPlatform("windows")]
    public static byte[] GenerateLarge(float[] samples)
    {
        return Generate(samples, 640, 120);
    }

    [SupportedOSPlatform("windows")]
    private static byte[] Generate(float[] samples, int width, int height)
    {
        if (samples == null || samples.Length == 0)
            return Array.Empty<byte>();

        using var bitmap = new Bitmap(width, height);
        using var graphics = Graphics.FromImage(bitmap);

        // Background
        graphics.Clear(Color.FromArgb(30, 30, 35));

        // Calculate bins
        int binSize = Math.Max(1, samples.Length / width);
        float[] bins = new float[width];

        for (int i = 0; i < width; i++)
        {
            int start = i * binSize;
            int end = Math.Min(start + binSize, samples.Length);

            float maxAbs = 0;
            for (int j = start; j < end; j++)
            {
                float abs = Math.Abs(samples[j]);
                if (abs > maxAbs) maxAbs = abs;
            }
            bins[i] = maxAbs;
        }

        // Normalize
        float max = bins.Max();
        if (max > 0)
        {
            for (int i = 0; i < bins.Length; i++)
                bins[i] = bins[i] / max;
        }

        // Draw waveform
        using var pen = new Pen(Color.FromArgb(100, 200, 255), 1);
        float centerY = height / 2f;

        for (int x = 0; x < width; x++)
        {
            float amplitude = bins[x] * (height / 2f - 2);
            graphics.DrawLine(pen, x, centerY - amplitude, x, centerY + amplitude);
        }

        // Save to PNG
        using var stream = new MemoryStream();
        bitmap.Save(stream, ImageFormat.Png);
        return stream.ToArray();
    }
}