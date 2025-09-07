using Lazarus.Desktop.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace Lazarus.Desktop.Services;

public sealed class OrchestratorRunnerClient : IOrchestratorRunnerClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OrchestratorRunnerClient> _logger;
    private readonly IOptionsMonitor<OrchestratorOptions> _options;
    private readonly JsonSerializerOptions _jsonOptions;
    private bool _disposed;

    public OrchestratorRunnerClient(HttpClient httpClient, ILogger<OrchestratorRunnerClient> logger, IOptionsMonitor<OrchestratorOptions> options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        ConfigureHttpClient();
    }

    public async Task<bool> LoadModelAsync(string modelPath, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        if (string.IsNullOrWhiteSpace(modelPath))
            throw new ArgumentException("Model path is required", nameof(modelPath));

        try
        {
            using var response = await _httpClient.PostAsJsonAsync("/runner/load", new { modelPath }, _jsonOptions, cancellationToken)
                .ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                _logger.LogWarning("LoadModelAsync failed: {Status} {Body}", (int)response.StatusCode, body);
                return false;
            }

            var payload = await response.Content.ReadFromJsonAsync<LoadRunnerResponse>(_jsonOptions, cancellationToken)
                .ConfigureAwait(false);
            var ok = payload?.Status?.Equals("ok", StringComparison.OrdinalIgnoreCase) == true;
            return ok;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(ex, "Error loading model {ModelPath}", modelPath);
            return false;
        }
    }

    public async Task<bool> UnloadAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        try
        {
            using var response = await _httpClient.PostAsync("/runner/unload", content: null, cancellationToken)
                .ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                return false;
            var payload = await response.Content.ReadFromJsonAsync<SimpleStatus>(_jsonOptions, cancellationToken)
                .ConfigureAwait(false);
            return payload?.Status?.Equals("ok", StringComparison.OrdinalIgnoreCase) == true;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(ex, "Error unloading runner");
            return false;
        }
    }

    public async Task<RunnerProcessStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        try
        {
            var status = await _httpClient.GetFromJsonAsync<RunnerProcessStatus>("/runner/status", _jsonOptions, cancellationToken)
                .ConfigureAwait(false);
            return status ?? new RunnerProcessStatus(false, null, null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get runner status");
            return new RunnerProcessStatus(false, null, null);
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _httpClient?.Dispose();
            _disposed = true;
        }
    }

    private void ConfigureHttpClient()
    {
        var options = _options.CurrentValue;
        _httpClient.BaseAddress = new Uri(options.BaseUrl);
        _httpClient.Timeout = options.RequestTimeout;
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Lazarus-Desktop/RunnerClient");
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
    }

    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(OrchestratorRunnerClient));
    }

    private sealed record SimpleStatus(string Status);
    private sealed record LoadRunnerResponse(string Status, bool HotSwapped, string? RunnerId, int? Port, string? RunnerType, string? ModelId, string? ModelPath, int? Pid);
}

