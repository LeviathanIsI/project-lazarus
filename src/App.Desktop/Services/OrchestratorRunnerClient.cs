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
    private string? _lastError;
    public event EventHandler<RunnerProcessStatus>? RunnerStatusChanged;

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

    public async Task<bool> LoadModelAsync(string modelPath, IEnumerable<string>? loras = null, double? loraScale = null, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        if (string.IsNullOrWhiteSpace(modelPath))
            throw new ArgumentException("Model path is required", nameof(modelPath));

        try
        {
            var payload = new LoadRequest(modelPath, loras?.ToList(), loraScale);

            // Debug logging
            if (loras != null && loras.Any())
            {
                _logger.LogInformation("Sending LoadModelAsync with LoRAs: {Loras}", string.Join(", ", loras));
            }
            else
            {
                _logger.LogInformation("Sending LoadModelAsync without LoRAs");
            }

            using var response = await _httpClient.PostAsJsonAsync("/runner/load", payload, _jsonOptions, cancellationToken)
                .ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                _lastError = TryExtractError(body) ?? body;
                _logger.LogWarning("LoadModelAsync failed: {Status} {Body}", (int)response.StatusCode, body);
                return false;
            }

            var respPayload = await response.Content.ReadFromJsonAsync<LoadRunnerResponse>(_jsonOptions, cancellationToken)
                .ConfigureAwait(false);
            var ok = respPayload?.Status?.Equals("ok", StringComparison.OrdinalIgnoreCase) == true;
            if (ok)
            {
                var status = await GetStatusAsync(cancellationToken).ConfigureAwait(false);
                RunnerStatusChanged?.Invoke(this, status);
            }
            return ok;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _lastError = ex.Message;
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
            {
                var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                _lastError = TryExtractError(body) ?? body;
                return false;
            }
            var payload = await response.Content.ReadFromJsonAsync<SimpleStatus>(_jsonOptions, cancellationToken)
                .ConfigureAwait(false);
            var ok = payload?.Status?.Equals("ok", StringComparison.OrdinalIgnoreCase) == true;
            if (ok)
            {
                var status = await GetStatusAsync(cancellationToken).ConfigureAwait(false);
                RunnerStatusChanged?.Invoke(this, status);
            }
            return ok;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _lastError = ex.Message;
            _logger.LogError(ex, "Error unloading runner");
            return false;
        }
    }

    public async Task<RunnerProcessStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        // Retry logic for startup lag - runner might not be ready yet
        for (var attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                var status = await _httpClient.GetFromJsonAsync<RunnerProcessStatus>("/runner/status", _jsonOptions, cancellationToken)
                    .ConfigureAwait(false);
                var s = status ?? new RunnerProcessStatus(false, null, null);
                RunnerStatusChanged?.Invoke(this, s);
                return s;
            }
            catch (HttpRequestException ex) when (attempt < 2)
            {
                _logger.LogDebug(ex, "Runner status check failed (attempt {Attempt}/3), retrying...", attempt + 1);
                await Task.Delay(500, cancellationToken).ConfigureAwait(false);
            }
            catch (TaskCanceledException ex) when (attempt < 2)
            {
                _logger.LogDebug(ex, "Runner status check timed out (attempt {Attempt}/3), retrying...", attempt + 1);
                await Task.Delay(500, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _lastError = ex.Message;
                _logger.LogWarning(ex, "Failed to get runner status after {Attempts} attempts", attempt + 1);
                return new RunnerProcessStatus(false, null, null);
            }
        }

        // Final attempt failed
        _lastError = "Runner status check failed after retries";
        _logger.LogWarning("Runner status check failed after all retries");
        return new RunnerProcessStatus(false, null, null);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            // HttpClient is managed by IHttpClientFactory, don't dispose it
            // Only set the disposed flag to prevent further use
            _disposed = true;
        }
    }

    public string? LastError => _lastError;

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
    private sealed record LoadRunnerResponse(string Status, bool HotSwapped, string? RunnerId, int? Port, string? RunnerType, string? ModelId, string? ModelPath, int? Pid, int? LorasApplied, string? LaunchArgs);
    private sealed record LoadRequest(string ModelPath, List<string>? Loras, double? LoraScale);

    private static string? TryExtractError(string body)
    {
        try
        {
            var doc = System.Text.Json.JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("error", out var err))
            {
                return err.GetString();
            }
        }
        catch { }
        return null;
    }
}
