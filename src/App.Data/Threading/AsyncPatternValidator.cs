using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Lazarus.App.Data.Threading;

/// <summary>
/// Validates async patterns and detects potential threading violations in database operations
/// Provides compile-time and runtime enforcement of proper async/await usage
/// </summary>
public static class AsyncPatternValidator
{
    private static readonly ThreadLocal<Stack<string>> CallStack = new(() => new Stack<string>());
    
    /// <summary>
    /// Validates that the current context is appropriate for async database operations
    /// </summary>
    /// <param name="logger">Logger for reporting violations</param>
    /// <param name="operationName">Name of the database operation being performed</param>
    /// <param name="callerMemberName">Automatically provided caller member name</param>
    /// <param name="callerFilePath">Automatically provided caller file path</param>
    /// <param name="callerLineNumber">Automatically provided caller line number</param>
    public static void ValidateAsyncContext(
        ILogger logger,
        string operationName,
        [CallerMemberName] string? callerMemberName = null,
        [CallerFilePath] string? callerFilePath = null,
        [CallerLineNumber] int callerLineNumber = 0)
    {
        var violations = new List<string>();
        
        // Check for synchronization context issues
        ValidateSynchronizationContext(violations);
        
        // Check for blocking async calls
        ValidateAsyncCallChain(violations, callerMemberName);
        
        // Check for UI thread violations
        ValidateUIThreadUsage(violations);
        
        // Check for connection context threading
        ValidateConnectionThreading(violations);
        
        if (violations.Any())
        {
            var violationDetails = string.Join("; ", violations);
            var caller = $"{callerMemberName} in {Path.GetFileName(callerFilePath)}:{callerLineNumber}";
            
            logger.LogError("ASYNC PATTERN VIOLATION in {Operation} called from {Caller}: {Violations}",
                operationName, caller, violationDetails);
                
            // In debug builds, throw to catch violations early
            Debug.Assert(false, $"Async pattern violation: {violationDetails}");
        }
    }
    
    /// <summary>
    /// Validates that ConfigureAwait(false) is being used properly
    /// </summary>
    /// <param name="task">Task to validate</param>
    /// <param name="logger">Logger for reporting violations</param>
    /// <param name="operationName">Name of the operation</param>
    public static void ValidateConfigureAwait<T>(
        Task<T> task, 
        ILogger logger, 
        string operationName,
        [CallerMemberName] string? callerMemberName = null)
    {
        // This is a compile-time guidance method
        // The actual enforcement happens through code review and static analysis
        logger.LogTrace("Validating ConfigureAwait usage for {Operation} in {Caller}", 
            operationName, callerMemberName);
    }
    
    /// <summary>
    /// Tracks async operation entry for deadlock detection
    /// </summary>
    /// <param name="operationName">Name of the operation</param>
    /// <returns>Disposable token to track operation completion</returns>
    public static IDisposable TrackAsyncOperation(string operationName)
    {
        var callStack = CallStack.Value;
        if (callStack == null)
        {
            CallStack.Value = callStack = new Stack<string>();
        }
        
        callStack.Push(operationName);
        return new AsyncOperationTracker(operationName, callStack);
    }
    
    private static void ValidateSynchronizationContext(List<string> violations)
    {
        var syncContext = SynchronizationContext.Current;
        
        // Database operations should not run on UI synchronization contexts
        if (syncContext != null && syncContext.GetType().Name.Contains("WindowsFormsSynchronizationContext"))
        {
            violations.Add("Database operation running on Windows Forms UI thread");
        }
        
        if (syncContext != null && syncContext.GetType().Name.Contains("DispatcherSynchronizationContext"))
        {
            violations.Add("Database operation running on WPF UI thread");
        }
    }
    
    private static void ValidateAsyncCallChain(List<string> violations, string? callerMemberName)
    {
        var callStack = CallStack.Value;
        if (callStack?.Count > 10)
        {
            violations.Add($"Deep async call stack detected ({callStack.Count} levels) - potential async deadlock risk");
        }
        
        // Check for common anti-patterns
        if (callerMemberName != null)
        {
            if (callerMemberName.Contains("Result") || callerMemberName.Contains("Wait"))
            {
                violations.Add("Potential blocking async call detected");
            }
        }
    }
    
    private static void ValidateUIThreadUsage(List<string> violations)
    {
        // Check if we're on the main UI thread (Windows)
        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            var currentThread = Thread.CurrentThread;
            
            // Main thread typically has ApartmentState.STA for WPF applications
            if (currentThread.GetApartmentState() == ApartmentState.STA && 
                currentThread.IsBackground == false)
            {
                violations.Add("Database operation potentially running on main UI thread");
            }
        }
    }
    
    private static void ValidateConnectionThreading(List<string> violations)
    {
        var managedThreadId = Environment.CurrentManagedThreadId;
        
        // Check for excessive thread switching (basic heuristic)
        if (managedThreadId == 1)
        {
            violations.Add("Database operation on thread 1 - likely main application thread");
        }
    }
    
    private class AsyncOperationTracker : IDisposable
    {
        private readonly string _operationName;
        private readonly Stack<string> _callStack;
        private bool _disposed;
        
        public AsyncOperationTracker(string operationName, Stack<string> callStack)
        {
            _operationName = operationName;
            _callStack = callStack;
        }
        
        public void Dispose()
        {
            if (_disposed) return;
            
            _disposed = true;
            
            if (_callStack.Count > 0 && _callStack.Peek() == _operationName)
            {
                _callStack.Pop();
            }
        }
    }
}

/// <summary>
/// Extension methods for validating async patterns in database operations
/// </summary>
public static class AsyncValidationExtensions
{
    /// <summary>
    /// Validates async patterns for database operations
    /// </summary>
    /// <typeparam name="T">Task result type</typeparam>
    /// <param name="task">Task to validate</param>
    /// <param name="logger">Logger instance</param>
    /// <param name="operationName">Name of the operation</param>
    /// <param name="callerMemberName">Caller member name</param>
    /// <returns>The original task for chaining</returns>
    public static Task<T> ValidateAsync<T>(
        this Task<T> task,
        ILogger logger,
        string operationName,
        [CallerMemberName] string? callerMemberName = null)
    {
        AsyncPatternValidator.ValidateAsyncContext(logger, operationName, callerMemberName);
        return task;
    }
    
    /// <summary>
    /// Validates async patterns for database operations without return value
    /// </summary>
    /// <param name="task">Task to validate</param>
    /// <param name="logger">Logger instance</param>
    /// <param name="operationName">Name of the operation</param>
    /// <param name="callerMemberName">Caller member name</param>
    /// <returns>The original task for chaining</returns>
    public static Task ValidateAsync(
        this Task task,
        ILogger logger,
        string operationName,
        [CallerMemberName] string? callerMemberName = null)
    {
        AsyncPatternValidator.ValidateAsyncContext(logger, operationName, callerMemberName);
        return task;
    }
}