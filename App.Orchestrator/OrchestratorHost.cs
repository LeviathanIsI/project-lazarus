using System.Text.Json;
using Lazarus.Orchestrator.Runners;
using Lazarus.Orchestrator.Services;
using Lazarus.Shared.OpenAI;
using Lazarus.Shared.Utilities;
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

        DirectoryBootstrap.EnsureDirectories();
        await RunnerRegistry.InitializeAsync();

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = Array.Empty<string>() });
        builder.WebHost.UseUrls(url);

        var app = builder.Build();

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
                Ram = new
                {
                    Total = ram.TotalBytes,
                    Used = ram.UsedBytes,
                    Available = ram.AvailableBytes,
                    UsagePercent = ram.UsagePercentage,
                    TotalFormatted = ram.TotalFormatted,
                    UsedFormatted = ram.UsedFormatted
                },
                Gpu = new
                {
                    Total = gpu.TotalBytes,
                    Used = gpu.UsedBytes,
                    Available = gpu.AvailableBytes,
                    UsagePercent = gpu.UsagePercentage,
                    TotalFormatted = gpu.TotalFormatted,
                    UsedFormatted = gpu.UsedFormatted,
                    Temperature = gpu.Temperature,
                    PowerDraw = gpu.PowerDraw
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
        app.MapGet("/v1/models/available", () =>
        {
            var inventory = ModelScannerService.ScanAll();
            var activeModel = RunnerRegistry.Active?.Name;

            var body = new
            {
                BaseModels = inventory.BaseModels.Select(m => new
                {
                    Id = m.Name,
                    Name = m.Name,
                    FileName = m.FileName,
                    Size = m.SizeFormatted,
                    Format = m.Format,
                    Architecture = m.Architecture,
                    ContextLength = m.ContextLength,
                    Quantization = m.Quantization,
                    IsActive = m.Name.Equals(activeModel, StringComparison.OrdinalIgnoreCase)
                }),
                Loras = inventory.LoRAs.Select(l => new
                {
                    Id = l.Name,
                    Name = l.Name,
                    FileName = l.FileName,
                    Size = l.SizeFormatted,
                    Type = l.Type,
                    Rank = l.Rank,
                    Alpha = l.Alpha
                }),
                Vaes = inventory.VAEs.Select(v => new
                {
                    Id = v.Name,
                    Name = v.Name,
                    FileName = v.FileName,
                    Size = v.SizeFormatted,
                    Type = v.Type,
                    Compatibility = v.Compatibility
                }),
                Embeddings = inventory.Embeddings.Select(e => new
                {
                    Id = e.Name,
                    Name = e.Name,
                    FileName = e.FileName,
                    Size = e.SizeFormatted,
                    Type = e.Type,
                    Keyword = e.Keyword,
                    Vectors = e.Vectors
                }),
                Hypernetworks = inventory.Hypernetworks.Select(h => new
                {
                    Id = h.Name,
                    Name = h.Name,
                    FileName = h.FileName,
                    Size = h.SizeFormatted,
                    Architecture = h.Architecture,
                    TrainingSteps = h.TrainingSteps
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
            var status = RunnerRegistry.GetStatus();
            var body = new
            {
                Active = status.RunnerType,
                Available = new[]
                {
                    new { Id = "llama-server", Name = "LLaMA Server", Description = "External llama-server process" },
                    new { Id = "llama-cpp", Name = "LLaMA.cpp", Description = "Direct llama.cpp integration" }
                },
                Configured = status.ConfiguredRunners,
                AvailableRunners = status.AvailableRunners
            };
            return Results.Json(body, Json);
        });

        // Model loading endpoint
        app.MapPost("/v1/models/load", (LoadModelRequest request) =>
        {
            try
            {
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
                try
                {
                    var resp = await RunnerRegistry.Active.ChatAsync(payload, reqCt);
                    return Results.Json(resp, Json);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[OrchestratorHost] Chat completion failed: {ex.Message}");
                    return Results.Problem($"Runner failed: {ex.Message}", statusCode: 500);
                }
            }

            // No fallback echo - force proper runner configuration
            return Results.Problem("No active runner configured", statusCode: 503);
        });

        _app = app;
        _runTask = ((IHost)_app).RunAsync(ct);
        await Task.Delay(100, ct);
    }

    public static async Task StopAsync(CancellationToken ct = default)
    {
        if (_app is null) return;
        RunnerRegistry.Shutdown();
        await _app.StopAsync(ct);
        _app = null;
        _runTask = null;
    }
}

// Request models for new endpoints
public record SwitchRunnerRequest(string Type, string BaseUrl, string? Model = null);
public record LoadModelRequest(string ModelPath, string? Name = null);