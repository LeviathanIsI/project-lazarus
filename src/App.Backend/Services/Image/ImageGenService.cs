using System.Text;
using System.Text.Json;
using Lazarus.Backend.Runners;
using Lazarus.Shared.Contracts.Image;
using Lazarus.Shared.Enums;

namespace Lazarus.Backend.Services.Image;

public interface IImageGenService
{
    Task<byte[]> GeneratePngAsync(ImageGenRequest req, CancellationToken ct);
}

public sealed class ImageGenService : IImageGenService
{
    private readonly IRunnerRegistry _registry;
    private readonly HttpClient _http;

    public ImageGenService(IRunnerRegistry registry, HttpClient http)
    {
        _registry = registry;
        _http = http;
    }

    public async Task<byte[]> GeneratePngAsync(ImageGenRequest req, CancellationToken ct)
    {
        var descriptor = _registry.GetById(req.RunnerId)
                         ?? throw new InvalidOperationException($"Image runner '{req.RunnerId}' not found.");

        if (descriptor.Kind != RunnerKind.ImageGen)
            throw new InvalidOperationException($"Runner '{req.RunnerId}' is not an ImageGen runner.");

        return descriptor.Provider?.ToLowerInvariant() switch
        {
            "sd-webui" or "automatic" => await CallAutomaticAsync(descriptor.BaseUrl, req, ct).ConfigureAwait(false),
            "comfyui" => await CallComfyAsync(descriptor.BaseUrl, req, ct).ConfigureAwait(false),
            _ => throw new NotSupportedException($"Unknown image provider '{descriptor.Provider}'.")
        };
    }

    private async Task<byte[]> CallAutomaticAsync(string baseUrl, ImageGenRequest r, CancellationToken ct)
    {
        var client = _http;
        var url = $"{baseUrl.TrimEnd('/')}/sdapi/v1/txt2img";
        var payload = new
        {
            prompt = r.Prompt,
            negative_prompt = r.NegativePrompt ?? string.Empty,
            width = r.Width,
            height = r.Height,
            steps = r.Steps,
            cfg_scale = r.CfgScale
        };
        using var req = new HttpRequestMessage(HttpMethod.Post, url)
        { Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json") };
        using var res = await client.SendAsync(req, ct).ConfigureAwait(false);
        res.EnsureSuccessStatusCode();

        var json = await res.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        using var doc = JsonDocument.Parse(json);
        var b64 = doc.RootElement.GetProperty("images")[0].GetString();
        return Convert.FromBase64String(b64!);
    }

    private async Task<byte[]> CallComfyAsync(string baseUrl, ImageGenRequest r, CancellationToken ct)
    {
        var client = _http;
        var url = $"{baseUrl.TrimEnd('/')}/prompt";
        var graph = new { prompt = r.Prompt };
        using var req = new HttpRequestMessage(HttpMethod.Post, url)
        { Content = new StringContent(JsonSerializer.Serialize(graph), Encoding.UTF8, "application/json") };
        using var res = await client.SendAsync(req, ct).ConfigureAwait(false);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);
    }
}
