using System.Text.Json;
using Lazarus.Orchestrator.Runners;
using Lazarus.Orchestrator.Services;
using Lazarus.Shared.OpenAI;
using Lazarus.Shared.Utilities; // <-- added
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace Lazarus.Orchestrator;

public static class OrchestratorHost
{
    private static WebApplication? _app;
    private static Task? _runTask;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private static readonly DateTimeOffset _started = DateTimeOffset.UtcNow;

    public static async Task StartAsync(string url = "http://127.0.0.1:11711", CancellationToken ct = default)
    {
        if (_app != null) return;

        // ensure Lazarus folders exist
        DirectoryBootstrap.EnsureDirectories(); // <-- added

        // Bind runner from env (LAZARUS_RUNNER_URL / KIND / NAME / MODEL)
        RunnerRegistry.InitializeFromEnv();

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = Array.Empty<string>() });
        builder.WebHost.UseUrls(url);

        var app = builder.Build();

        // rest of your existing endpoints remain unchanged...


        // Simple health for orchestrator + runner
        app.MapGet("/status", async () =>
        {
            var hasRunner = RunnerRegistry.Active is not null;
            var runnerOk = hasRunner && await RunnerRegistry.Active!.HealthAsync();
            var body = new
            {
                ok = true,
                name = "Lazarus Orchestrator",
                version = "0.1.1-dev",
                started = _started.ToUnixTimeSeconds(),
                runner = new
                {
                    configured = hasRunner,
                    name = hasRunner ? RunnerRegistry.Active!.Name : null,
                    baseUrl = hasRunner ? RunnerRegistry.Active!.BaseAddress.ToString() : null,
                    healthy = hasRunner ? runnerOk : (bool?)null
                }
            };
            return Results.Json(body, Json);
        });

        // System information endpoint
        app.MapGet("/v1/system", () =>
        {
            var ram = SystemInfoService.GetSystemMemory();
            var gpu = SystemInfoService.GetGpuMemory();

            var body = new
            {
                ram = new
                {
                    total = ram.TotalBytes,
                    used = ram.UsedBytes,
                    available = ram.AvailableBytes,
                    usagePercent = ram.UsagePercentage,
                    totalFormatted = ram.TotalFormatted,
                    usedFormatted = ram.UsedFormatted
                },
                gpu = new
                {
                    total = gpu.TotalBytes,
                    used = gpu.UsedBytes,
                    available = gpu.AvailableBytes,
                    usagePercent = gpu.UsagePercentage,
                    totalFormatted = gpu.TotalFormatted,
                    usedFormatted = gpu.UsedFormatted,
                    temperature = gpu.Temperature,
                    powerDraw = gpu.PowerDraw
                }
            };
            return Results.Json(body, Json);
        });

        // Model scanning endpoint
        app.MapGet("/v1/models/scan", () =>
        {
            var inventory = ModelScannerService.ScanAll();
            return Results.Json(inventory, Json);
        });

        // Available models for current runner
        app.MapGet("/v1/models/available", async () =>
        {
            var inventory = ModelScannerService.ScanAll();
            var activeModel = RunnerRegistry.Active?.Name;

            var body = new
            {
                baseModels = inventory.BaseModels.Select(m => new
                {
                    id = m.Name,
                    name = m.Name,
                    fileName = m.FileName,
                    size = m.SizeFormatted,
                    format = m.Format,
                    architecture = m.Architecture,
                    contextLength = m.ContextLength,
                    quantization = m.Quantization,
                    isActive = m.Name.Equals(activeModel, StringComparison.OrdinalIgnoreCase)
                }),
                loras = inventory.LoRAs.Select(l => new
                {
                    id = l.Name,
                    name = l.Name,
                    fileName = l.FileName,
                    size = l.SizeFormatted,
                    type = l.Type,
                    rank = l.Rank,
                    alpha = l.Alpha
                }),
                vaes = inventory.VAEs.Select(v => new
                {
                    id = v.Name,
                    name = v.Name,
                    fileName = v.FileName,
                    size = v.SizeFormatted,
                    type = v.Type,
                    compatibility = v.Compatibility
                }),
                embeddings = inventory.Embeddings.Select(e => new
                {
                    id = e.Name,
                    name = e.Name,
                    fileName = e.FileName,
                    size = e.SizeFormatted,
                    type = e.Type,
                    keyword = e.Keyword,
                    vectors = e.Vectors
                }),
                hypernetworks = inventory.Hypernetworks.Select(h => new
                {
                    id = h.Name,
                    name = h.Name,
                    fileName = h.FileName,
                    size = h.SizeFormatted,
                    architecture = h.Architecture,
                    trainingSteps = h.TrainingSteps
                })
            };
            return Results.Json(body, Json);
        });

        // Runner management
        app.MapPost("/v1/runners/switch", (SwitchRunnerRequest request) =>
        {
            try
            {
                RunnerRegistry.SwitchTo(request.Type, request.BaseUrl, request.Model);
                return Results.Ok(new { success = true, message = $"Switched to {request.Type}" });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { success = false, error = ex.Message });
            }
        });

        // Get available runners
        app.MapGet("/v1/runners", () =>
        {
            var body = new
            {
                active = RunnerRegistry.Active?.Name,
                available = new[]
                {
                    new { id = "llama-server", name = "LLaMA Server", description = "External llama-server process" },
                    new { id = "llama-cpp", name = "LLaMA.cpp", description = "Direct llama.cpp integration" }
                }
            };
            return Results.Json(body, Json);
        });

        // Model loading endpoint
        app.MapPost("/v1/models/load", (LoadModelRequest request) =>
        {
            try
            {
                // This would typically restart the runner with the new model
                // For now, just update the active model reference
                var success = RunnerRegistry.LoadModel(request.ModelPath);
                return Results.Ok(new { success, message = success ? "Model loaded" : "Failed to load model" });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { success = false, error = ex.Message });
            }
        });

        // Minimal models list (OpenAI compatibility)
        app.MapGet("/v1/models", () =>
        {
            var body = new
            {
                data = new[]
                {
                    new { id = RunnerRegistry.Active?.Name ?? "echo", @object = "model" }
                }
            };
            return Results.Json(body, Json);
        });

        // OpenAI-compatible chat proxy
        app.MapPost("/v1/chat/completions", async (ChatCompletionRequest payload, CancellationToken reqCt) =>
        {
            if (RunnerRegistry.Active is not null)
            {
                var resp = await RunnerRegistry.Active.ChatAsync(payload, reqCt);
                return Results.Json(resp, Json);
            }

            // Fallback echo if no runner configured
            var userText = payload?.Messages?.LastOrDefault(m => m.Role == "user")?.Content?.Trim() ?? "";
            var content = userText.Equals("ping", StringComparison.OrdinalIgnoreCase) ? "pong" : $"echo: {userText}";

            var respFallback = new ChatCompletionResponse
            {
                Model = payload?.Model ?? "local-dev",
                Choices =
                [
                    new Choice
                    {
                        Index = 0,
                        Message = new ChatMessage { Role = "assistant", Content = content },
                        FinishReason = "stop"
                    }
                ],
                Usage = new Usage
                {
                    PromptTokens = userText.Length / 4,
                    CompletionTokens = content.Length / 4,
                    TotalTokens = (userText.Length + content.Length) / 4
                }
            };
            return Results.Json(respFallback, Json);
        });

        _app = app;
        _runTask = ((IHost)_app).RunAsync(ct);
        await Task.Delay(100, ct);
    }

    public static async Task StopAsync(CancellationToken ct = default)
    {
        if (_app is null) return;
        await _app.StopAsync(ct);
        _app = null;
        _runTask = null;
    }
}

// Request models for new endpoints
public record SwitchRunnerRequest(string Type, string BaseUrl, string? Model = null);
public record LoadModelRequest(string ModelPath, string? Name = null);