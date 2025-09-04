using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Net.Http;
using System.ComponentModel;

namespace Lazarus.App.Desktop.Services;

/// <summary>
/// Hosted service that embeds the Lazarus orchestrator API within the WPF application
/// </summary>
public class OrchestratorHostService : IHostedService, INotifyPropertyChanged, IDisposable
{
    private readonly ILogger<OrchestratorHostService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly HttpClient _httpClient;
    
    private IHost? _orchestratorHost;
    private bool _isRunning;
    private bool _isHealthy;
    private string _status = "Stopped";
    private CancellationTokenSource? _cancellationTokenSource;
    
    // Configuration constants
    // Bind explicitly to IPv4 loopback to avoid localhost IPv6/IPv4 ambiguity.
    private const string HostUrl = "http://127.0.0.1:11711";
    private const int HealthCheckTimeoutMs = 5000;
    private const int StartupTimeoutMs = 30000;
    private const int HealthCheckIntervalMs = 15000;

    public OrchestratorHostService(ILogger<OrchestratorHostService> logger, IServiceProvider serviceProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        
        // Use a dedicated handler to dodge system proxies and force HTTP/1.1 for stability on loopback.
        var handler = new HttpClientHandler
        {
            Proxy = null,
            UseProxy = false
        };
        _httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromMilliseconds(HealthCheckTimeoutMs)
        };
        _httpClient.DefaultRequestVersion = new Version(1, 1);
        _httpClient.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
        
        _logger.LogInformation("OrchestratorHostService initialized for URL: {HostUrl}", HostUrl);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Gets a value indicating whether the orchestrator API is running
    /// </summary>
    public bool IsRunning
    {
        get => _isRunning;
        private set
        {
            if (_isRunning != value)
            {
                _isRunning = value;
                OnPropertyChanged(nameof(IsRunning));
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether the orchestrator API is healthy and responsive
    /// </summary>
    public bool IsHealthy
    {
        get => _isHealthy;
        private set
        {
            if (_isHealthy != value)
            {
                _isHealthy = value;
                OnPropertyChanged(nameof(IsHealthy));
            }
        }
    }

    /// <summary>
    /// Gets the current status of the orchestrator service
    /// </summary>
    public string Status
    {
        get => _status;
        private set
        {
            if (_status != value)
            {
                _status = value;
                OnPropertyChanged(nameof(Status));
                _logger.LogInformation("Orchestrator service status changed to: {Status}", value);
            }
        }
    }

    /// <summary>
    /// Gets the orchestrator API endpoint
    /// </summary>
    public string ApiEndpoint => HostUrl;

    /// <summary>
    /// Starts the orchestrator hosted service
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (IsRunning)
        {
            _logger.LogWarning("Attempt to start orchestrator when already running");
            return;
        }

        try
        {
            Status = "Starting";
            _logger.LogInformation("Starting Lazarus orchestrator API host");

            // Create the orchestrator host
            _orchestratorHost = CreateOrchestratorHost();
            
            // Start the host
            await _orchestratorHost.StartAsync(cancellationToken);
            
            IsRunning = true;
            Status = "Starting API";
            
            _logger.LogInformation("Orchestrator host started, waiting for API readiness");

            // Wait for API to become ready
            var isReady = await WaitForApiReadyAsync(cancellationToken);
            if (isReady)
            {
                Status = "Running";
                IsHealthy = true;
                StartHealthMonitoring();
                _logger.LogInformation("Orchestrator API is ready and accepting requests at {ApiEndpoint}", ApiEndpoint);
            }
            else
            {
                Status = "Error: API not ready";
                await StopAsync(cancellationToken);
                throw new InvalidOperationException("Orchestrator API failed to become ready within timeout period");
            }
        }
        catch (Exception ex)
        {
            Status = "Error: Startup failed";
            _logger.LogError(ex, "Error starting orchestrator service");
            await StopAsync(cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// Stops the orchestrator hosted service
    /// </summary>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!IsRunning)
        {
            return;
        }

        try
        {
            Status = "Stopping";
            _logger.LogInformation("Stopping Lazarus orchestrator API host");

            // Cancel health monitoring
            _cancellationTokenSource?.Cancel();

            if (_orchestratorHost != null)
            {
                await _orchestratorHost.StopAsync(TimeSpan.FromSeconds(10));
                _orchestratorHost.Dispose();
                _orchestratorHost = null;
            }

            IsRunning = false;
            IsHealthy = false;
            Status = "Stopped";
            
            _logger.LogInformation("Orchestrator API host stopped");
        }
        catch (Exception ex)
        {
            Status = "Error: Stop failed";
            _logger.LogError(ex, "Error stopping orchestrator service");
        }
    }

    /// <summary>
    /// Performs a health check on the orchestrator API
    /// </summary>
    public async Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default)
    {
        if (!IsRunning)
        {
            _logger.LogInformation("Health check skipped - orchestrator not running");
            return false;
        }

        try
        {
            var healthUrl = $"{ApiEndpoint}/api/health";
            _logger.LogDebug("Performing health check to {HealthUrl}", healthUrl);
            
            using var response = await _httpClient.GetAsync(healthUrl, cancellationToken);
            var isHealthy = response.IsSuccessStatusCode;
            
            _logger.LogDebug("Health check response: {StatusCode} {ReasonPhrase}", response.StatusCode, response.ReasonPhrase);
            
            IsHealthy = isHealthy;
            
            if (!isHealthy)
            {
                _logger.LogWarning("Orchestrator health check failed: {StatusCode} {ReasonPhrase}", response.StatusCode, response.ReasonPhrase);
            }
            else
            {
                _logger.LogInformation("Orchestrator health check succeeded");
            }

            return isHealthy;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogInformation("Orchestrator health check HTTP error: {Message}", ex.Message);
            IsHealthy = false;
            return false;
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogInformation("Orchestrator health check timed out");
            IsHealthy = false;
            return false;
        }
        catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Orchestrator health check cancelled");
            IsHealthy = false;
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogInformation(ex, "Orchestrator health check error");
            IsHealthy = false;
            return false;
        }
    }

    /// <summary>
    /// Creates the orchestrator host with proper configuration
    /// </summary>
    private IHost CreateOrchestratorHost()
    {
        var builder = Host.CreateDefaultBuilder()
            .ConfigureWebHostDefaults(webBuilder =>
            {
                // Bind explicitly to IPv4 loopback to match the readiness probe.
                webBuilder.UseKestrel(options =>
                {
                    options.AddServerHeader = false;
                    options.Listen(System.Net.IPAddress.Loopback, 11711);
                });
                // Keep UseUrls for clarity, but Kestrel.Listen above is authoritative.
                webBuilder.UseUrls(HostUrl);
                webBuilder.ConfigureServices((context, services) =>
                {
                    // Add basic web API services
                    services.AddControllers();
                    services.AddEndpointsApiExplorer();
                    services.AddSwaggerGen();
                    
                    // Add CORS for local development
                    services.AddCors(options =>
                    {
                        options.AddDefaultPolicy(builder =>
                        {
                            builder.AllowAnyOrigin()
                                   .AllowAnyMethod()
                                   .AllowAnyHeader();
                        });
                    });

                    // Add health checks (explicit self check — makes behavior crystal clear)
                    services.AddHealthChecks()
                            .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy());
                    
                    // Add logging
                    services.AddLogging(logging =>
                    {
                        logging.ClearProviders();
                        logging.AddConsole();
                        logging.AddDebug();
                        logging.SetMinimumLevel(LogLevel.Information);
                    });

                    // TODO: Add orchestrator-specific services here
                    // This would include runner management, model registry, etc.
                });
                
                webBuilder.Configure((context, app) =>
                {
                    // Configure the HTTP request pipeline
                    if (context.HostingEnvironment.IsDevelopment())
                    {
                        app.UseSwagger();
                        app.UseSwaggerUI();
                    }

                    app.UseCors();
                    app.UseRouting();
                    
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapControllers();
                        endpoints.MapHealthChecks("/api/health");
                        
                        // Add a basic endpoint for testing
                        endpoints.MapGet("/api/status", async context =>
                        {
                            var response = new
                            {
                                Status = "Running",
                                Timestamp = DateTime.UtcNow,
                                Version = "1.0.0",
                                Service = "Lazarus Orchestrator API"
                            };
                            
                            context.Response.ContentType = "application/json";
                            await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
                        });
                    });
                });
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.AddDebug();
                logging.SetMinimumLevel(LogLevel.Information);
            });

        return builder.Build();
    }

    /// <summary>
    /// Waits for the API to become ready
    /// </summary>
    private async Task<bool> WaitForApiReadyAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("WaitForApiReadyAsync: Starting readiness probe loop");
        
        // Use an internal timeout so a prematurely-cancelled outer CTS doesn't abort a winning probe.
        using var overallCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        overallCts.CancelAfter(StartupTimeoutMs);
        var token = overallCts.Token;
        var deadline = DateTime.UtcNow.AddMilliseconds(StartupTimeoutMs);
        
        _logger.LogInformation("WaitForApiReadyAsync: Timeout set to {StartupTimeoutMs}ms, deadline {Deadline}", StartupTimeoutMs, deadline);

        async Task<bool> TryUrlAsync(string url, CancellationToken ct)
        {
            try
            {
                using var resp = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
                _logger.LogInformation("Readiness probe {Url} -> {Code}", url, (int)resp.StatusCode);
                if ((int)resp.StatusCode >= 200 && (int)resp.StatusCode < 300)
                    return true;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                _logger.LogDebug("Readiness probe cancelled for {Url}", url);
            }
            catch (Exception ex)
            {
                _logger.LogDebug("Readiness probe exception for {Url}: {Message}", url, ex.Message);
            }
            return false;
        }

        while (DateTime.UtcNow < deadline && !token.IsCancellationRequested)
        {
            _logger.LogInformation("WaitForApiReadyAsync: Starting probe attempt, time remaining: {Remaining}ms", (deadline - DateTime.UtcNow).TotalMilliseconds);
            
            var health = $"{ApiEndpoint}/api/health";
            var status = $"{ApiEndpoint}/api/status";

            _logger.LogInformation("WaitForApiReadyAsync: Trying health endpoint: {HealthUrl}", health);
            var healthResult = await TryUrlAsync(health, token).ConfigureAwait(false);
            _logger.LogInformation("WaitForApiReadyAsync: Health result: {HealthResult}", healthResult);

            if (healthResult)
            {
                _logger.LogInformation("Orchestrator readiness confirmed.");
                return true;
            }

            _logger.LogInformation("WaitForApiReadyAsync: Trying status endpoint: {StatusUrl}", status);
            var statusResult = await TryUrlAsync(status, token).ConfigureAwait(false);
            _logger.LogInformation("WaitForApiReadyAsync: Status result: {StatusResult}", statusResult);

            if (statusResult)
            {
                _logger.LogInformation("Orchestrator readiness confirmed.");
                return true;
            }

            _logger.LogInformation("WaitForApiReadyAsync: Both endpoints failed, waiting 1 second before retry");
            await Task.Delay(1000, token).ConfigureAwait(false);
        }

        return false;
    }

    /// <summary>
    /// Starts continuous health monitoring
    /// </summary>
    private void StartHealthMonitoring()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource = new CancellationTokenSource();
        
        _ = Task.Run(async () =>
        {
            var token = _cancellationTokenSource.Token;
            
            while (!token.IsCancellationRequested && IsRunning)
            {
                try
                {
                    await Task.Delay(HealthCheckIntervalMs, token);
                    
                    if (!token.IsCancellationRequested && IsRunning)
                    {
                        var isHealthy = await HealthCheckAsync(token);
                        
                        if (!isHealthy && IsRunning)
                        {
                            Status = "Unhealthy";
                            _logger.LogWarning("Orchestrator health check failed, API may be unresponsive");
                        }
                        else if (isHealthy && Status == "Unhealthy")
                        {
                            Status = "Running";
                            _logger.LogInformation("Orchestrator API health restored");
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancelling
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in orchestrator health monitoring loop");
                    await Task.Delay(5000, token); // Wait before retrying
                }
            }
        }, _cancellationTokenSource.Token);
    }

    /// <summary>
    /// Raises the PropertyChanged event
    /// </summary>
    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Disposes of resources
    /// </summary>
    public void Dispose()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        
        if (IsRunning)
        {
            _ = Task.Run(async () => await StopAsync());
        }
        
        _orchestratorHost?.Dispose();
        _httpClient?.Dispose();
    }
}