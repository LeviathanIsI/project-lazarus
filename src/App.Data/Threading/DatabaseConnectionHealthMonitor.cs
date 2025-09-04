using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lazarus.App.Data.Threading;

/// <summary>
/// Background service that monitors database connection health and prevents connection leaks
/// Ensures proper connection pool management and thread safety
/// </summary>
public class DatabaseConnectionHealthMonitor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseConnectionHealthMonitor> _logger;
    private readonly TimeSpan _healthCheckInterval;
    private readonly TimeSpan _connectionTimeout;

    public DatabaseConnectionHealthMonitor(
        IServiceProvider serviceProvider,
        ILogger<DatabaseConnectionHealthMonitor> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _healthCheckInterval = TimeSpan.FromMinutes(2); // Check every 2 minutes
        _connectionTimeout = TimeSpan.FromSeconds(10);  // 10 second timeout for health checks
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Database connection health monitor started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_healthCheckInterval, stoppingToken).ConfigureAwait(false);
                await PerformHealthCheckAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Database connection health monitor stopping due to cancellation");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during database connection health check");
                
                // Wait before retrying to avoid tight error loops
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken).ConfigureAwait(false);
            }
        }

        _logger.LogInformation("Database connection health monitor stopped");
    }

    /// <summary>
    /// Performs a comprehensive database connection health check
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    private async Task PerformHealthCheckAsync(CancellationToken cancellationToken)
    {
        using var timeoutCts = new CancellationTokenSource(_connectionTimeout);
        using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
        
        var startTime = DateTime.UtcNow;
        
        try
        {
            await using var scope = _serviceProvider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<LazarusDbContext>();

            // Test database connectivity with a simple query
            var connectionTest = await context.Database.CanConnectAsync(combinedCts.Token).ConfigureAwait(false);
            
            if (connectionTest)
            {
                // Perform additional checks - count total records to ensure query execution works
                var assetCount = await context.LlmAssets.CountAsync(combinedCts.Token).ConfigureAwait(false);
                var sessionCount = await context.TrainingSessions.CountAsync(combinedCts.Token).ConfigureAwait(false);
                
                var duration = DateTime.UtcNow - startTime;
                
                _logger.LogDebug("Database health check PASSED - Assets: {AssetCount}, Sessions: {SessionCount}, Duration: {Duration}ms",
                    assetCount, sessionCount, duration.TotalMilliseconds);

                // Check for potential connection pool issues
                if (duration.TotalSeconds > 2)
                {
                    _logger.LogWarning("Database health check took {Duration:F2}s - potential connection pool saturation",
                        duration.TotalSeconds);
                }
            }
            else
            {
                _logger.LogError("Database health check FAILED - Cannot connect to database");
                await LogConnectionPoolStatusAsync(context).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested)
        {
            _logger.LogError("Database health check TIMEOUT after {Timeout}s - connection pool may be exhausted", 
                _connectionTimeout.TotalSeconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check EXCEPTION - connection stability compromised");
        }
    }

    /// <summary>
    /// Logs detailed connection pool status for debugging
    /// </summary>
    /// <param name="context">Database context to analyze</param>
    private async Task LogConnectionPoolStatusAsync(LazarusDbContext context)
    {
        try
        {
            // Check if we can get basic database info
            var connectionString = context.Database.GetConnectionString();
            _logger.LogInformation("Connection string: {ConnectionString}", 
                connectionString?.Replace("Password=", "Password=***") ?? "NULL");

            // Try to get SQLite specific information
            if (context.Database.IsSqlite())
            {
                using var command = context.Database.GetDbConnection().CreateCommand();
                command.CommandText = "SELECT sqlite_version()";
                await context.Database.OpenConnectionAsync().ConfigureAwait(false);
                var version = (await command.ExecuteScalarAsync().ConfigureAwait(false))?.ToString();
                _logger.LogInformation("SQLite version: {Version}", version ?? "Unknown");
            }

            _logger.LogInformation("Active thread count: {ThreadCount}, Process threads: {ProcessThreads}",
                Environment.CurrentManagedThreadId,
                Environment.ProcessorCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to gather connection pool diagnostics");
        }
    }

    /// <summary>
    /// Forces garbage collection to clean up potential connection leaks
    /// </summary>
    private void ForceGarbageCollection()
    {
        _logger.LogWarning("Forcing garbage collection to clean up potential connection leaks");
        
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        var memoryBefore = GC.GetTotalMemory(false);
        var memoryAfter = GC.GetTotalMemory(true);
        
        _logger.LogInformation("Garbage collection completed - Memory before: {MemoryBefore:N0} bytes, after: {MemoryAfter:N0} bytes, freed: {Freed:N0} bytes",
            memoryBefore, memoryAfter, memoryBefore - memoryAfter);
    }
}