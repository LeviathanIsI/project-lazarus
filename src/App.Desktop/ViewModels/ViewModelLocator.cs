using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.Windows;

namespace Lazarus.Desktop.ViewModels;

/// <summary>
/// Service locator for ViewModels with dependency injection integration.
/// Provides thread-safe access to ViewModels with proper lifetime management.
/// </summary>
public sealed class ViewModelLocator : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<Type, object> _singletonViewModels;
    private readonly object _lock = new();
    private bool _disposed;

    public ViewModelLocator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _singletonViewModels = new Dictionary<Type, object>();
    }

    /// <summary>
    /// Gets the main view model with singleton lifetime.
    /// </summary>
    public MainViewModel MainViewModel => GetOrCreateSingleton<MainViewModel>();

    /// <summary>
    /// Gets the navigation view model with singleton lifetime.
    /// </summary>
    public NavigationViewModel NavigationViewModel => GetOrCreateSingleton<NavigationViewModel>();

    /// <summary>
    /// Gets the models view model with singleton lifetime.
    /// </summary>
    public ModelsViewModel ModelsViewModel => GetOrCreateSingleton<ModelsViewModel>();

    /// <summary>
    /// Creates a new instance of the specified ViewModel type.
    /// Use this for transient ViewModels that need fresh instances.
    /// </summary>
    /// <typeparam name="T">The ViewModel type to create.</typeparam>
    /// <returns>A new instance of the ViewModel.</returns>
    public T CreateViewModel<T>() where T : ViewModelBase
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ViewModelLocator));

        return _serviceProvider.GetRequiredService<T>();
    }

    /// <summary>
    /// Gets or creates a singleton instance of the specified ViewModel type.
    /// Use this for ViewModels that should maintain state across the application lifetime.
    /// </summary>
    /// <typeparam name="T">The ViewModel type to get or create.</typeparam>
    /// <returns>The singleton instance of the ViewModel.</returns>
    public T GetOrCreateSingleton<T>() where T : ViewModelBase
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ViewModelLocator));

        lock (_lock)
        {
            if (!_singletonViewModels.TryGetValue(typeof(T), out var existingViewModel))
            {
                existingViewModel = _serviceProvider.GetRequiredService<T>();
                _singletonViewModels[typeof(T)] = existingViewModel;
            }

            return (T)existingViewModel;
        }
    }

    /// <summary>
    /// Releases a singleton ViewModel instance, allowing it to be garbage collected.
    /// The ViewModel will be recreated on next access.
    /// </summary>
    /// <typeparam name="T">The ViewModel type to release.</typeparam>
    public void ReleaseSingleton<T>() where T : ViewModelBase
    {
        if (_disposed)
            return;

        lock (_lock)
        {
            if (_singletonViewModels.TryGetValue(typeof(T), out var viewModel))
            {
                _singletonViewModels.Remove(typeof(T));

                // Dispose the ViewModel if it implements IDisposable
                if (viewModel is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }
    }

    /// <summary>
    /// Clears all singleton ViewModel instances.
    /// </summary>
    public void ClearSingletons()
    {
        if (_disposed)
            return;

        lock (_lock)
        {
            foreach (var viewModel in _singletonViewModels.Values)
            {
                if (viewModel is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }

            _singletonViewModels.Clear();
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            ClearSingletons();
            _disposed = true;
        }
    }

    /// <summary>
    /// Static property for XAML binding support.
    /// Gets the current ViewModelLocator instance from the application resources.
    /// </summary>
    public static ViewModelLocator? Instance
    {
        get
        {
            // Ensure we're on the UI thread for Application.Current access
            if (Application.Current?.Dispatcher?.CheckAccess() == false)
            {
                return Application.Current.Dispatcher.Invoke(() => Instance);
            }

            return Application.Current?.Resources["ViewModelLocator"] as ViewModelLocator;
        }
    }
}
