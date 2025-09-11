using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.Versioning;

namespace Lazarus.Shared.Utilities.Imaging;

[SupportedOSPlatform("windows")]
public static class WaveformPng
{
    public static byte[] GenerateWaveform(float[] samples, int width, int height)
    {
        using var bitmap = new Bitmap(width, height);
        using var graphics = Graphics.FromImage(bitmap);
        
        // Background
        graphics.Clear(Color.FromArgb(26, 26, 26));
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        
        // Grid lines
        using (var gridPen = new Pen(Color.FromArgb(40, 80, 80, 80), 0.5f))
        {
            // Vertical grid lines every 10%
            for (int i = 1; i < 10; i++)
            {
                float x = width * i / 10f;
                graphics.DrawLine(gridPen, x, 0, x, height);
            }
            
            // Horizontal center line
            float centerY = height / 2f;
            graphics.DrawLine(gridPen, 0, centerY, width, centerY);
        }
        
        // Waveform
        if (samples != null && samples.Length > 0)
        {
            using var waveformPen = new Pen(Color.FromArgb(0, 200, 83), 1.5f);
            using var fillBrush = new SolidBrush(Color.FromArgb(80, 0, 200, 83));
            
            int samplesPerPixel = Math.Max(1, samples.Length / width);
            float centerY = height / 2f;
            
            for (int x = 0; x < width; x++)
            {
                int startIdx = x * samplesPerPixel;
                int endIdx = Math.Min(startIdx + samplesPerPixel, samples.Length);
                
                // Find min/max in this pixel column
                float min = 0, max = 0;
                for (int i = startIdx; i < endIdx; i++)
                {
                    min = Math.Min(min, samples[i]);
                    max = Math.Max(max, samples[i]);
                }
                
                // Scale to pixel coordinates
                float topY = centerY - (max * centerY * 0.9f);
                float bottomY = centerY - (min * centerY * 0.9f);
                
                // Draw vertical bar
                graphics.DrawLine(waveformPen, x, topY, x, bottomY);
                
                // Fill area
                using var path = new GraphicsPath();
                path.AddLine(x, centerY, x, topY);
                path.AddLine(x, bottomY, x, centerY);
                graphics.FillPath(fillBrush, path);
            }
        }
        
        // Convert to PNG
        using var ms = new MemoryStream();
        bitmap.Save(ms, ImageFormat.Png);
        return ms.ToArray();
    }
    
    public static byte[] GenerateSmallWaveform(float[] samples)
        => GenerateWaveform(samples, 320, 40);
    
    public static byte[] GenerateLargeWaveform(float[] samples)
        => GenerateWaveform(samples, 800, 120);
    
    // Signal synthesis helpers for preview mode
    public static float[] GenerateSineWave(int sampleCount, float frequency = 440f, float amplitude = 0.5f)
    {
        var samples = new float[sampleCount];
        float sampleRate = 44100f;
        
        for (int i = 0; i < sampleCount; i++)
        {
            samples[i] = amplitude * (float)Math.Sin(2 * Math.PI * frequency * i / sampleRate);
        }
        
        return samples;
    }
    
    public static float[] GenerateNoise(int sampleCount, float amplitude = 0.1f)
    {
        var samples = new float[sampleCount];
        var random = new Random();
        
        for (int i = 0; i < sampleCount; i++)
        {
            samples[i] = amplitude * (2f * (float)random.NextDouble() - 1f);
        }
        
        return samples;
    }
    
    public static float[] ApplyEnvelope(float[] samples, float attack = 0.1f, float decay = 0.1f, float sustain = 0.7f, float release = 0.2f)
    {
        int totalSamples = samples.Length;
        int attackSamples = (int)(attack * totalSamples);
        int decaySamples = (int)(decay * totalSamples);
        int releaseSamples = (int)(release * totalSamples);
        int sustainSamples = totalSamples - attackSamples - decaySamples - releaseSamples;
        
        var result = new float[totalSamples];
        int idx = 0;
        
        // Attack
        for (int i = 0; i < attackSamples && idx < totalSamples; i++, idx++)
        {
            float envelope = (float)i / attackSamples;
            result[idx] = samples[idx] * envelope;
        }
        
        // Decay
        for (int i = 0; i < decaySamples && idx < totalSamples; i++, idx++)
        {
            float envelope = 1f - ((1f - sustain) * i / decaySamples);
            result[idx] = samples[idx] * envelope;
        }
        
        // Sustain
        for (int i = 0; i < sustainSamples && idx < totalSamples; i++, idx++)
        {
            result[idx] = samples[idx] * sustain;
        }
        
        // Release
        for (int i = 0; i < releaseSamples && idx < totalSamples; i++, idx++)
        {
            float envelope = sustain * (1f - (float)i / releaseSamples);
            result[idx] = samples[idx] * envelope;
        }
        
        return result;
    }
    
    public static float[] MixSignals(params float[][] signals)
    {
        if (signals == null || signals.Length == 0)
            return Array.Empty<float>();
        
        int maxLength = 0;
        foreach (var signal in signals)
            maxLength = Math.Max(maxLength, signal?.Length ?? 0);
        
        var result = new float[maxLength];
        
        for (int i = 0; i < maxLength; i++)
        {
            float sum = 0;
            int count = 0;
            
            foreach (var signal in signals)
            {
                if (signal != null && i < signal.Length)
                {
                    sum += signal[i];
                    count++;
                }
            }
            
            if (count > 0)
                result[i] = sum / count;
        }
        
        return result;
    }
}