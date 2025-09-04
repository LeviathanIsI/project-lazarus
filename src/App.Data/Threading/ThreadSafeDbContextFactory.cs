using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Lazarus.App.Data.Threading;

/// <summary>
/// Thread-safe DbContext factory for managing database connections across concurrent operations
/// Implements proper connection pooling and disposal patterns
/// </summary>
public class ThreadSafeDbContextFactory : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ThreadSafeDbContextFactory> _logger;
    private readonly SemaphoreSlim _connectionSemaphore;
    private readonly ConcurrentDictionary<string, int> _activeConnections;
    private readonly int _maxConcurrentConnections;
    private bool _disposed;

    public ThreadSafeDbContextFactory(
        IServiceProvider serviceProvider,
        ILogger<ThreadSafeDbContextFactory> logger,
        int maxConcurrentConnections = 10)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _maxConcurrentConnections = maxConcurrentConnections;
        _connectionSemaphore = new SemaphoreSlim(maxConcurrentConnections, maxConcurrentConnections);
        _activeConnections = new ConcurrentDictionary<string, int>();
    }

    /// <summary>
    /// Creates a new DbContext instance with proper thread safety and connection management
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>A thread-safe DbContext instance</returns>
    public async Task<ThreadSafeDbContextWrapper> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ThreadSafeDbContextFactory));

        var connectionId = Guid.NewGuid().ToString("N");
        
        // Acquire connection from the pool
        await _connectionSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        
        try
        {
            var scope = _serviceProvider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<LazarusDbContext>();
            
            _activeConnections.TryAdd(connectionId, Environment.CurrentManagedThreadId);
            
            _logger.LogDebug("Created DbContext {ConnectionId} on thread {ThreadId}. Active connections: {ActiveCount}", 
                connectionId, Environment.CurrentManagedThreadId, _activeConnections.Count);
            
            return new ThreadSafeDbContextWrapper(context, scope, connectionId, () => ReleaseConnection(connectionId));
        }
        catch
        {
            // Release semaphore if context creation failed
            _connectionSemaphore.Release();
            throw;
        }
    }

    /// <summary>
    /// Executes a database operation with automatic connection management and error handling
    /// </summary>
    /// <typeparam name="T">Return type of the operation</typeparam>
    /// <param name="operation">Database operation to execute</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>Result of the database operation</returns>
    public async Task<T> ExecuteWithContextAsync<T>(
        Func<LazarusDbContext, CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ThreadSafeDbContextFactory));

        await using var contextWrapper = await CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        return await operation(contextWrapper.Context, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes a database operation without return value with automatic connection management
    /// </summary>
    /// <param name="operation">Database operation to execute</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    public async Task ExecuteWithContextAsync(
        Func<LazarusDbContext, CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ThreadSafeDbContextFactory));

        await using var contextWrapper = await CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await operation(contextWrapper.Context, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the current number of active database connections
    /// </summary>
    public int ActiveConnectionCount => _activeConnections.Count;

    /// <summary>
    /// Gets the maximum allowed concurrent connections
    /// </summary>
    public int MaxConcurrentConnections => _maxConcurrentConnections;

    /// <summary>
    /// Releases a connection back to the pool
    /// </summary>
    /// <param name="connectionId">ID of the connection to release</param>
    private void ReleaseConnection(string connectionId)
    {
        if (_disposed)
            return;

        _activeConnections.TryRemove(connectionId, out var threadId);
        _connectionSemaphore.Release();
        
        _logger.LogDebug("Released DbContext {ConnectionId} from thread {ThreadId}. Active connections: {ActiveCount}", 
            connectionId, threadId, _activeConnections.Count);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        
        _logger.LogInformation("Disposing ThreadSafeDbContextFactory with {ActiveCount} active connections", 
            _activeConnections.Count);

        _connectionSemaphore?.Dispose();
        _activeConnections.Clear();
    }
}

/// <summary>
/// Wrapper for DbContext that provides proper disposal and connection tracking
/// </summary>
public class ThreadSafeDbContextWrapper : IAsyncDisposable
{
    private readonly IAsyncDisposable _scope;
    private readonly string _connectionId;
    private readonly Action _onDispose;
    private bool _disposed;

    internal ThreadSafeDbContextWrapper(
        LazarusDbContext context,
        IAsyncDisposable scope,
        string connectionId,
        Action onDispose)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _connectionId = connectionId ?? throw new ArgumentNullException(nameof(connectionId));
        _onDispose = onDispose ?? throw new ArgumentNullException(nameof(onDispose));
    }

    /// <summary>
    /// Gets the wrapped DbContext instance
    /// </summary>
    public LazarusDbContext Context { get; }

    /// <summary>
    /// Gets the connection ID for tracking purposes
    /// </summary>
    public string ConnectionId => _connectionId;

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        try
        {
            await _scope.DisposeAsync().ConfigureAwait(false);
        }
        finally
        {
            _onDispose();
        }
    }
}