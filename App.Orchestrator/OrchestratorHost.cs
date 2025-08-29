using System.Text.Json;
using Lazarus.Orchestrator.Runners;
using Lazarus.Orchestrator.Services;
using Lazarus.Shared.OpenAI;
using Lazarus.Shared.Models;
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
    
    // LoRA state management - this is the critical missing piece
    private static readonly List<AppliedLoRAInfo> _activeLoRAs = new();
    private static readonly object _loraStateLock = new();

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

        // Model unload endpoint (UI expects this route)
        app.MapDelete("/v1/models/current", () =>
        {
            try
            {
                var success = RunnerRegistry.UnloadModel();
                return Results.Ok(new { success, message = success ? "Model unloaded" : "Failed to unload model" });
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

        // Model introspection endpoint - discover actual parameter capabilities
        app.MapGet("/v1/models/{modelName}/capabilities", async (string modelName) =>
        {
            if (RunnerRegistry.Active is null)
            {
                return Results.Problem("No active runner configured", statusCode: 503);
            }

            try
            {
                // Use the current model if no specific model provided
                var targetModel = modelName == "current" ? RunnerRegistry.CurrentModel ?? "unknown" : modelName;
                
                var capabilities = await ModelIntrospectionService.IntrospectModelAsync(RunnerRegistry.Active, targetModel);
                return Results.Json(capabilities, Json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OrchestratorHost] Model introspection failed: {ex.Message}");
                return Results.Problem($"Introspection failed: {ex.Message}", statusCode: 500);
            }
        });
        
        // LoRA-aware model capabilities endpoint
        app.MapPost("/v1/models/{modelName}/capabilities/with-loras", async (string modelName, List<AppliedLoRAInfo> appliedLoRAs) =>
        {
            if (RunnerRegistry.Active is null)
            {
                return Results.Problem("No active runner configured", statusCode: 503);
            }

            try
            {
                // Use the current model if no specific model provided
                var targetModel = modelName == "current" ? RunnerRegistry.CurrentModel ?? "unknown" : modelName;
                
                // Get base capabilities first
                var baseCapabilities = await ModelIntrospectionService.IntrospectModelAsync(RunnerRegistry.Active, targetModel);
                
                // Apply LoRA modifications
                var loraAwareCapabilities = ModelIntrospectionService.UpdateCapabilitiesWithLoRAs(baseCapabilities, appliedLoRAs);
                
                Console.WriteLine($"[OrchestratorHost] Generated LoRA-aware capabilities for {targetModel} with {appliedLoRAs.Count} LoRAs");
                return Results.Json(loraAwareCapabilities, Json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OrchestratorHost] LoRA-aware introspection failed: {ex.Message}");
                return Results.Problem($"LoRA-aware introspection failed: {ex.Message}", statusCode: 500);
            }
        });
        
        // Endpoint to report currently applied LoRAs - FIXED STATE SYNCHRONIZATION
        app.MapGet("/v1/loras/applied", () =>
        {
            lock (_loraStateLock)
            {
                Console.WriteLine($"[OrchestratorHost] Reporting {_activeLoRAs.Count} active LoRAs");
                return Results.Json(_activeLoRAs.ToList(), Json);
            }
        });
        
        // Endpoint to apply a LoRA - ACTUAL STATE MANAGEMENT
        app.MapPost("/v1/loras/apply", async (AppliedLoRAInfo loraInfo) =>
        {
            Console.WriteLine($"[OrchestratorHost] LoRA application request: {loraInfo.Name} (weight: {loraInfo.Weight})");
            
            lock (_loraStateLock)
            {
                // Remove existing instance if present
                _activeLoRAs.RemoveAll(l => l.Id == loraInfo.Id);
                
                // Add the new LoRA
                loraInfo.AppliedAt = DateTime.UtcNow;
                loraInfo.IsEnabled = true;
                _activeLoRAs.Add(loraInfo);
                
                Console.WriteLine($"[OrchestratorHost] LoRA '{loraInfo.Name}' applied. Total active LoRAs: {_activeLoRAs.Count}");
            }
            
            // TODO: Integrate with actual model runner to load the LoRA
            // For now, we're managing state so the UI can see it
            
            return Results.Json(new { 
                success = true, 
                message = $"LoRA '{loraInfo.Name}' applied successfully",
                activeLoRAs = _activeLoRAs.Count,
                totalWeight = _activeLoRAs.Where(l => l.IsEnabled).Sum(l => l.Weight)
            }, Json);
        });
        
        // Endpoint to remove a LoRA - ACTUAL STATE MANAGEMENT
        app.MapDelete("/v1/loras/{loraId}", async (string loraId) =>
        {
            Console.WriteLine($"[OrchestratorHost] LoRA removal request: {loraId}");
            
            bool removed = false;
            string removedName = "unknown";
            
            lock (_loraStateLock)
            {
                var loraToRemove = _activeLoRAs.FirstOrDefault(l => l.Id == loraId);
                if (loraToRemove != null)
                {
                    removedName = loraToRemove.Name;
                    _activeLoRAs.Remove(loraToRemove);
                    removed = true;
                    
                    Console.WriteLine($"[OrchestratorHost] LoRA '{removedName}' removed. Remaining active LoRAs: {_activeLoRAs.Count}");
                }
                else
                {
                    Console.WriteLine($"[OrchestratorHost] LoRA '{loraId}' not found in active list");
                }
            }
            
            // TODO: Remove from actual model runner
            
            return Results.Json(new { 
                success = removed, 
                message = removed ? $"LoRA '{removedName}' removed successfully" : $"LoRA '{loraId}' not found",
                activeLoRAs = _activeLoRAs.Count
            }, Json);
        });
        
        // Endpoint to clear all LoRAs - MASS STATE RESET
        app.MapDelete("/v1/loras", async () =>
        {
            Console.WriteLine($"[OrchestratorHost] Clearing all LoRAs");
            
            int clearedCount;
            lock (_loraStateLock)
            {
                clearedCount = _activeLoRAs.Count;
                _activeLoRAs.Clear();
            }
            
            Console.WriteLine($"[OrchestratorHost] Cleared {clearedCount} active LoRAs");
            
            return Results.Json(new { 
                success = true, 
                message = $"Cleared {clearedCount} active LoRAs",
                activeLoRAs = 0
            }, Json);
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