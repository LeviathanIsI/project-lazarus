using System.Collections.Concurrent;
using System.Net;
using System.Text.Json.Serialization;
using Lazarus.Shared;

var builder = WebApplication.CreateBuilder(args);

// Force binding strictly to loopback:11711
builder.WebHost.ConfigureKestrel(options =>
{
    options.Listen(IPAddress.Loopback, 11711);
});

// Minimal extras for dev visibility
builder.Services.AddEndpointsApiExplorer();

// Simple in-memory runner registry
var runners = new ConcurrentDictionary<string, RunnerInfo>();

// Ensure first-run directory layout is present
DirectoryBootstrap.EnsureAll();

var app = builder.Build();

// Health endpoint
app.MapGet("/health", () => Results.Json(new
{
    status = "ok",
    service = "orchestrator",
    timestamp = DateTimeOffset.UtcNow
}));

// Models list (placeholder: returns empty for now)
app.MapGet("/api/models", () => Results.Json(Array.Empty<ModelInfo>()));

// Start runner (simulated)
app.MapPost("/api/runners", (StartRunnerRequest req) =>
{
    var id = Guid.NewGuid().ToString("n");
    var info = new RunnerInfo
    {
        Id = id,
        ModelId = req.ModelId,
        RunnerType = req.Configuration?.RunnerType ?? "unknown",
        Port = req.Configuration?.Port ?? 0,
        StartedAt = DateTimeOffset.UtcNow
    };

    runners[id] = info;
    return Results.Json(info);
});

// Stop runner (simulated)
app.MapDelete("/api/runners/{id}", (string id) =>
{
    if (runners.TryRemove(id, out _))
        return Results.NoContent();
    return Results.NotFound();
});

// Runner status (simulated: mark all as healthy)
app.MapGet("/api/runners/status", () =>
{
    var statuses = runners.Values.Select(r => new RunnerStatus
    {
        Id = r.Id,
        ModelId = r.ModelId,
        RunnerType = r.RunnerType,
        Port = r.Port,
        IsHealthy = true,
        LastHealthCheck = DateTimeOffset.UtcNow,
        ErrorMessage = null
    });
    return Results.Json(statuses);
});

app.Run();

// DTOs mirroring Desktop client expectations (property names in camelCase)
public sealed class ModelInfo
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string Path { get; set; }
    public long SizeBytes { get; set; }
    public string Architecture { get; set; } = "unknown";
    public string[] SupportedRunners { get; set; } = Array.Empty<string>();
}

public sealed class RunnerConfiguration
{
    public required string RunnerType { get; set; }
    public int Port { get; set; }
    public Dictionary<string, object>? Parameters { get; set; }
}

public sealed class StartRunnerRequest
{
    public required string ModelId { get; set; }
    public RunnerConfiguration? Configuration { get; set; }
}

public sealed class RunnerInfo
{
    public required string Id { get; set; }
    public required string ModelId { get; set; }
    public required string RunnerType { get; set; }
    public int Port { get; set; }
    public DateTimeOffset StartedAt { get; set; }
}

public sealed class RunnerStatus
{
    public required string Id { get; set; }
    public required string ModelId { get; set; }
    public required string RunnerType { get; set; }
    public int Port { get; set; }
    public bool IsHealthy { get; set; }
    public DateTimeOffset LastHealthCheck { get; set; }
    public string? ErrorMessage { get; set; }
}

