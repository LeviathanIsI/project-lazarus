using System.Net.Http.Json;
using System.Text.Json;
using Lazarus.App.SDK.Configuration;
using Lazarus.App.Shared.Contracts;
using Lazarus.App.Shared.DTOs;
using Lazarus.App.Shared.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lazarus.App.SDK;

/// <summary>
/// HTTP client wrapper for the Lazarus API
/// </summary>
public class LazarusApiClient : ITrainingService, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<LazarusApiClient> _logger;
    private readonly LazarusApiOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="LazarusApiClient"/> class
    /// </summary>
    /// <param name="httpClient">The HTTP client</param>
    /// <param name="options">The API client options</param>
    /// <param name="logger">The logger</param>
    public LazarusApiClient(
        HttpClient httpClient,
        IOptions<LazarusApiOptions> options,
        ILogger<LazarusApiClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        ConfigureHttpClient();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<TrainingSession>> GetAllSessionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Retrieving all training sessions");

            var response = await _httpClient.GetAsync("api/training/sessions", cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<IEnumerable<TrainingSession>>>(
                    _jsonOptions, cancellationToken);
                
                return apiResponse?.Data ?? Enumerable.Empty<TrainingSession>();
            }

            _logger.LogWarning("Failed to retrieve training sessions. Status: {StatusCode}", response.StatusCode);
            return Enumerable.Empty<TrainingSession>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving training sessions");
            return Enumerable.Empty<TrainingSession>();
        }
    }

    /// <inheritdoc />
    public async Task<TrainingSession?> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Retrieving training session {SessionId}", sessionId);

            var response = await _httpClient.GetAsync($"api/training/sessions/{sessionId}", cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<TrainingSession>>(
                    _jsonOptions, cancellationToken);
                
                return apiResponse?.Data;
            }

            _logger.LogWarning("Failed to retrieve training session {SessionId}. Status: {StatusCode}", 
                sessionId, response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving training session {SessionId}", sessionId);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<TrainingSession> CreateSessionAsync(TrainingSession session, CancellationToken cancellationToken = default)
    {
        if (session == null)
            throw new ArgumentNullException(nameof(session));

        try
        {
            _logger.LogInformation("Creating training session {SessionName}", session.Name);

            var response = await _httpClient.PostAsJsonAsync("api/training/sessions", session, _jsonOptions, cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<TrainingSession>>(
                    _jsonOptions, cancellationToken);
                
                if (apiResponse?.Data != null)
                {
                    _logger.LogInformation("Training session {SessionId} created successfully", apiResponse.Data.Id);
                    return apiResponse.Data;
                }
            }

            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Failed to create training session. Status: {StatusCode}, Content: {Content}", 
                response.StatusCode, errorContent);
            
            throw new HttpRequestException($"Failed to create training session. Status: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating training session {SessionName}", session.Name);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<TrainingSession> UpdateSessionAsync(TrainingSession session, CancellationToken cancellationToken = default)
    {
        if (session == null)
            throw new ArgumentNullException(nameof(session));

        try
        {
            _logger.LogInformation("Updating training session {SessionId}", session.Id);

            var response = await _httpClient.PutAsJsonAsync($"api/training/sessions/{session.Id}", session, _jsonOptions, cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<TrainingSession>>(
                    _jsonOptions, cancellationToken);
                
                if (apiResponse?.Data != null)
                {
                    _logger.LogInformation("Training session {SessionId} updated successfully", session.Id);
                    return apiResponse.Data;
                }
            }

            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Failed to update training session {SessionId}. Status: {StatusCode}, Content: {Content}", 
                session.Id, response.StatusCode, errorContent);
            
            throw new HttpRequestException($"Failed to update training session. Status: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating training session {SessionId}", session.Id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> StartSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        return await ExecuteSessionActionAsync(sessionId, "start", cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> StopSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        return await ExecuteSessionActionAsync(sessionId, "stop", cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Deleting training session {SessionId}", sessionId);

            var response = await _httpClient.DeleteAsync($"api/training/sessions/{sessionId}", cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Training session {SessionId} deleted successfully", sessionId);
                return true;
            }

            _logger.LogWarning("Failed to delete training session {SessionId}. Status: {StatusCode}", 
                sessionId, response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting training session {SessionId}", sessionId);
            return false;
        }
    }

    /// <summary>
    /// Executes a session action (start/stop) asynchronously
    /// </summary>
    /// <param name="sessionId">The session identifier</param>
    /// <param name="action">The action to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the action was successful, false otherwise</returns>
    private async Task<bool> ExecuteSessionActionAsync(Guid sessionId, string action, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Executing {Action} action on training session {SessionId}", action, sessionId);

            var response = await _httpClient.PostAsync($"api/training/sessions/{sessionId}/{action}", null, cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully executed {Action} action on training session {SessionId}", action, sessionId);
                return true;
            }

            _logger.LogWarning("Failed to execute {Action} action on training session {SessionId}. Status: {StatusCode}", 
                action, sessionId, response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing {Action} action on training session {SessionId}", action, sessionId);
            return false;
        }
    }

    /// <summary>
    /// Configures the HTTP client with default settings
    /// </summary>
    private void ConfigureHttpClient()
    {
        _httpClient.BaseAddress = new Uri(_options.BaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);

        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Lazarus-SDK/1.0");

        if (!string.IsNullOrEmpty(_options.ApiKey))
        {
            _httpClient.DefaultRequestHeaders.Add("X-API-Key", _options.ApiKey);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (!_disposed)
        {
            _httpClient?.Dispose();
            _disposed = true;
        }
    }
}