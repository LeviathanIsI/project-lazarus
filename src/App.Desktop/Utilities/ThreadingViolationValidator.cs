using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;

namespace Lazarus.App.Desktop.Utilities;

/// <summary>
/// Validates and prevents threading violations in WPF UI layer
/// </summary>
public static class ThreadingViolationValidator
{
    private static readonly ILogger Logger = 
        Microsoft.Extensions.Logging.LoggerFactory.Create(builder => builder.AddDebug())
            .CreateLogger(nameof(ThreadingViolationValidator));

    private static readonly ConcurrentDictionary<Type, bool> ValidatedTypes = new();
    
    /// <summary>
    /// Validates that a ViewModel follows thread-safe patterns
    /// </summary>
    /// <param name="viewModel">The ViewModel to validate</param>
    /// <returns>True if thread-safe; otherwise false</returns>
    public static bool ValidateViewModel(INotifyPropertyChanged viewModel)
    {
        var type = viewModel.GetType();
        
        if (ValidatedTypes.TryGetValue(type, out var cachedResult))
        {
            return cachedResult;
        }

        var violations = new List<string>();
        
        // Check for ObservableCollection properties that aren't thread-safe
        var observableCollectionProperties = type.GetProperties()
            .Where(p => p.PropertyType.IsGenericType &&
                       p.PropertyType.GetGenericTypeDefinition() == typeof(ObservableCollection<>))
            .ToList();

        foreach (var prop in observableCollectionProperties)
        {
            violations.Add($"Property {prop.Name} uses ObservableCollection<> instead of ThreadSafeObservableCollection<>");
        }

        // Check for Timer-related fields that could cause threading issues
        var timerFields = type.GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .Where(f => f.FieldType == typeof(System.Timers.Timer) || 
                       f.FieldType == typeof(System.Threading.Timer))
            .ToList();

        foreach (var field in timerFields)
        {
            violations.Add($"Field {field.Name} uses System.Timer instead of DispatcherTimer for UI updates");
        }

        var isValid = violations.Count == 0;
        
        if (!isValid)
        {
            Logger.LogWarning("Threading violations found in {ViewModelType}:\n{Violations}", 
                type.Name, string.Join("\n", violations));
        }
        else
        {
            Logger.LogDebug("ViewModel {ViewModelType} passed threading validation", type.Name);
        }

        ValidatedTypes[type] = isValid;
        return isValid;
    }

    /// <summary>
    /// Ensures an operation runs on the UI thread
    /// </summary>
    /// <param name="action">The action to execute</param>
    public static void EnsureUIThread(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher == null)
        {
            Logger.LogWarning("No application dispatcher available, executing action directly");
            action();
            return;
        }

        if (dispatcher.CheckAccess())
        {
            action();
        }
        else
        {
            dispatcher.Invoke(action);
        }
    }

    /// <summary>
    /// Ensures an operation runs on the UI thread asynchronously
    /// </summary>
    /// <param name="action">The action to execute</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public static Task EnsureUIThreadAsync(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher == null)
        {
            Logger.LogWarning("No application dispatcher available, executing action directly");
            action();
            return Task.CompletedTask;
        }

        if (dispatcher.CheckAccess())
        {
            action();
            return Task.CompletedTask;
        }
        else
        {
            return dispatcher.InvokeAsync(action).Task;
        }
    }

    /// <summary>
    /// Validates that a collection operation is safe for UI binding
    /// </summary>
    /// <typeparam name="T">The type of collection items</typeparam>
    /// <param name="collection">The collection to validate</param>
    /// <returns>True if safe for UI binding; otherwise false</returns>
    public static bool ValidateCollectionForUIBinding<T>(ICollection<T> collection)
    {
        if (collection is Collections.ThreadSafeObservableCollection<T>)
        {
            Logger.LogDebug("Collection of type {CollectionType} is thread-safe", collection.GetType().Name);
            return true;
        }

        if (collection is ObservableCollection<T>)
        {
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher?.CheckAccess() == true)
            {
                Logger.LogDebug("ObservableCollection<{Type}> accessed on UI thread - safe", typeof(T).Name);
                return true;
            }
            else
            {
                Logger.LogWarning("ObservableCollection<{Type}> accessed from background thread - VIOLATION", typeof(T).Name);
                return false;
            }
        }

        Logger.LogDebug("Collection of type {CollectionType} is not observable - safe for background access", 
            collection.GetType().Name);
        return true;
    }

    /// <summary>
    /// Gets a summary of all validated types and their thread safety status
    /// </summary>
    /// <returns>A dictionary of type names and their validation results</returns>
    public static Dictionary<string, bool> GetValidationSummary()
    {
        return ValidatedTypes.ToDictionary(
            kvp => kvp.Key.Name,
            kvp => kvp.Value
        );
    }

    /// <summary>
    /// Clears the validation cache
    /// </summary>
    public static void ClearValidationCache()
    {
        ValidatedTypes.Clear();
        Logger.LogDebug("Threading validation cache cleared");
    }
}

/// <summary>
/// Extension methods for thread-safe operations
/// </summary>
public static class ThreadSafeExtensions
{
    /// <summary>
    /// Safely updates a property on the UI thread
    /// </summary>
    /// <typeparam name="T">The type of the property</typeparam>
    /// <param name="obj">The object containing the property</param>
    /// <param name="propertyUpdater">The action to update the property</param>
    public static void SafeUpdateProperty<T>(this T obj, Action<T> propertyUpdater) where T : class
    {
        ThreadingViolationValidator.EnsureUIThread(() => propertyUpdater(obj));
    }

    /// <summary>
    /// Safely updates a property on the UI thread asynchronously
    /// </summary>
    /// <typeparam name="T">The type of the property</typeparam>
    /// <param name="obj">The object containing the property</param>
    /// <param name="propertyUpdater">The action to update the property</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public static Task SafeUpdatePropertyAsync<T>(this T obj, Action<T> propertyUpdater) where T : class
    {
        return ThreadingViolationValidator.EnsureUIThreadAsync(() => propertyUpdater(obj));
    }
}