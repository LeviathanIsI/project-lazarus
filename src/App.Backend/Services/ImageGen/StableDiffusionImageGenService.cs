using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Lazarus.Backend.Services.Runners;
using Lazarus.Shared.Images;
using Lazarus.Shared.Runners;

namespace Lazarus.Backend.Services.ImageGen;

public sealed class StableDiffusionImageGenService : IImageGenService
{
    private readonly IRunnerRegistry _registry;
    private readonly HttpClient _http;

    public StableDiffusionImageGenService(IRunnerRegistry registry, HttpClient http)
    {
        _registry = registry;
        _http = http;
    }

    private static bool IsDiffusionModel(string path)
    {
        var ext = Path.GetExtension(path)?.ToLowerInvariant();
        return ext is ".safetensors" or ".ckpt" or ".onnx";
    }

    public async Task<bool> PingAsync(string baseUrl, CancellationToken ct)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl.TrimEnd('/')}/sdapi/v1/sd-models");
            using var res = await _http.SendAsync(req, ct).ConfigureAwait(false);
            return res.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public async IAsyncEnumerable<ImageGenEvent> GenerateAsync(ImageGenRequest req, [EnumeratorCancellation] CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Prompt))
        {
            yield return ImageGenEvent.Error("Prompt is empty.");
            yield break;
        }
        if (string.IsNullOrWhiteSpace(req.ModelPath) || !File.Exists(req.ModelPath) || !IsDiffusionModel(req.ModelPath))
        {
            yield return ImageGenEvent.Error("Select a diffusion model (.safetensors/.ckpt/.onnx).");
            yield break;
        }
        if (string.IsNullOrWhiteSpace(req.RunnerId))
        {
            yield return ImageGenEvent.Error("No Image runner selected.");
            yield break;
        }

        var runner = _registry.GetById(req.RunnerId);
        if (runner is null || runner.Role != RunnerRole.Image || !string.Equals(runner.Kind, "stable-diffusion", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(runner.BaseUrl))
        {
            yield return ImageGenEvent.Error("Invalid Stable Diffusion runner.");
            yield break;
        }

        if (!await PingAsync(runner.BaseUrl, ct).ConfigureAwait(false))
        {
            yield return ImageGenEvent.Error("Stable Diffusion runner is not reachable.");
            yield break;
        }

        yield return ImageGenEvent.Info("Uploading settings");

        var payload = new
        {
            prompt = req.Prompt,
            negative_prompt = req.NegativePrompt,
            seed = req.Seed,
            steps = req.Steps,
            cfg_scale = req.Cfg,
            sampler_name = req.Sampler,
            override_settings = new
            {
                sd_model_checkpoint = Path.GetFileNameWithoutExtension(req.ModelPath)
            }
        };

        var endpoint = (req.Mode ?? "txt2img").ToLowerInvariant() switch
        {
            "img2img" => "/sdapi/v1/img2img",
            "inpaint" => "/sdapi/v1/img2img",
            _ => "/sdapi/v1/txt2img"
        };

        using var resp = await _http.PostAsJsonAsync($"{runner.BaseUrl.TrimEnd('/')}{endpoint}", payload, ct).ConfigureAwait(false);
        if (!resp.IsSuccessStatusCode)
        {
            var err = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            yield return ImageGenEvent.Error($"Runner error: {(int)resp.StatusCode} {resp.ReasonPhrase}  {err}");
            yield break;
        }

        var json = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.TryGetProperty("images", out var arr) && arr.ValueKind == JsonValueKind.Array && arr.GetArrayLength() > 0)
        {
            var b64 = arr[0].GetString() ?? string.Empty;
            byte[]? png = null;
            var malformed = false;
            try { png = Convert.FromBase64String(b64); }
            catch { malformed = true; }
            if (malformed || png is null)
            {
                yield return ImageGenEvent.Error("Runner returned malformed image content.");
            }
            else
            {
                yield return ImageGenEvent.Done(png);
            }
        }
        else
        {
            yield return ImageGenEvent.Error("Runner returned no image.");
        }
    }
}
