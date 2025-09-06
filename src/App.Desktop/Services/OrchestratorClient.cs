using Lazarus.Desktop.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace Lazarus.Desktop.Services;

/// <summary>
/// HTTP client implementation for communicating with the Lazarus orchestrator API.
/// </summary>
public sealed class OrchestratorClient : IOrchestratorClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OrchestratorClient> _logger;
    private readonly IOptionsMonitor<OrchestratorOptions> _options;
    private readonly JsonSerializerOptions _jsonOptions;
    private bool _isHealthy;
    private bool _disposed;

    public OrchestratorClient(
        HttpClient httpClient,
        ILogger<OrchestratorClient> logger,
        IOptionsMonitor<OrchestratorOptions> options)
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

    public event EventHandler<HealthStatusChangedEventArgs>? HealthStatusChanged;

    public bool IsHealthy => _isHealthy;

    public async Task<bool> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(OrchestratorClient));

        try
        {
            using var response = await _httpClient.GetAsync("/health", cancellationToken).ConfigureAwait(false);
            var isHealthy = response.IsSuccessStatusCode;

            if (_isHealthy != isHealthy)
            {
                _isHealthy = isHealthy;
                var errorMessage = isHealthy ? null : $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}";
                OnHealthStatusChanged(isHealthy, errorMessage);
            }

            if (isHealthy)
            {
                _logger.LogDebug("Orchestrator health check successful");
            }
            else
            {
                _logger.LogWarning("Orchestrator health check failed: {StatusCode} {ReasonPhrase}",
                    response.StatusCode, response.ReasonPhrase);
            }

            return isHealthy;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            if (_isHealthy)
            {
                _isHealthy = false;
                OnHealthStatusChanged(false, ex.Message);
            }

            _logger.LogError(ex, "Orchestrator health check failed with exception");
            return false;
        }
    }

    public async Task<IEnumerable<ModelInfo>> GetAvailableModelsAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(OrchestratorClient));

        try
        {
            var models = await _httpClient.GetFromJsonAsync<ModelInfo[]>("/api/models", _jsonOptions, cancellationToken)
                .ConfigureAwait(false);

            return models ?? Array.Empty<ModelInfo>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve available models");
            return Array.Empty<ModelInfo>();
        }
    }

    public async Task<RunnerInfo> StartRunnerAsync(string modelId, RunnerConfiguration configuration, CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(OrchestratorClient));

        if (string.IsNullOrWhiteSpace(modelId))
            throw new ArgumentException("Model ID cannot be null or empty.", nameof(modelId));

        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        try
        {
            var request = new { ModelId = modelId, Configuration = configuration };
            using var response = await _httpClient.PostAsJsonAsync("/api/runners", request, _jsonOptions, cancellationToken)
                .ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            var runnerInfo = await response.Content.ReadFromJsonAsync<RunnerInfo>(_jsonOptions, cancellationToken)
                .ConfigureAwait(false);

            if (runnerInfo == null)
                throw new InvalidOperationException("Failed to deserialize runner information from response.");

            _logger.LogInformation("Successfully started runner {RunnerId} for model {ModelId}",
                runnerInfo.Id, modelId);

            return runnerInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start runner for model {ModelId}", modelId);
            throw;
        }
    }

    public async Task StopRunnerAsync(string runnerId, CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(OrchestratorClient));

        if (string.IsNullOrWhiteSpace(runnerId))
            throw new ArgumentException("Runner ID cannot be null or empty.", nameof(runnerId));

        try
        {
            using var response = await _httpClient.DeleteAsync($"/api/runners/{runnerId}", cancellationToken)
                .ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            _logger.LogInformation("Successfully stopped runner {RunnerId}", runnerId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop runner {RunnerId}", runnerId);
            throw;
        }
    }

    public async Task<IEnumerable<RunnerStatus>> GetRunnerStatusAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(OrchestratorClient));

        try
        {
            var statuses = await _httpClient.GetFromJsonAsync<RunnerStatus[]>("/api/runners/status", _jsonOptions, cancellationToken)
                .ConfigureAwait(false);

            return statuses ?? Array.Empty<RunnerStatus>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve runner status information");
            return Array.Empty<RunnerStatus>();
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _httpClient?.Dispose();
            _disposed = true;
            _logger.LogDebug("OrchestratorClient disposed");
        }
    }

    private void ConfigureHttpClient()
    {
        var options = _options.CurrentValue;
        _httpClient.BaseAddress = new Uri(options.BaseUrl);
        _httpClient.Timeout = options.RequestTimeout;

        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Lazarus-Desktop/1.0");
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
    }

    private void OnHealthStatusChanged(bool isHealthy, string? errorMessage = null)
    {
        HealthStatusChanged?.Invoke(this, new HealthStatusChangedEventArgs(isHealthy, errorMessage));
    }
}