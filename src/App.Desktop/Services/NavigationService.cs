using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Lazarus.Desktop.Services;

/// <summary>
/// Implementation of the navigation service for managing view transitions.
/// </summary>
public sealed class NavigationService : INavigationService, IDisposable
{
    private readonly Stack<NavigationEntry> _backStack = new();
    private readonly Stack<NavigationEntry> _forwardStack = new();
    private NavigationEntry? _currentEntry;
    private bool _disposed;

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler<NavigationEventArgs>? Navigated;

    public string? CurrentView => _currentEntry?.ViewName;

    public bool CanGoBack => _backStack.Count > 0;

    public bool CanGoForward => _forwardStack.Count > 0;

    public void NavigateTo(string viewName, object? parameter = null)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(NavigationService));

        if (string.IsNullOrWhiteSpace(viewName))
            throw new ArgumentException("View name cannot be null or empty.", nameof(viewName));

        // If we have a current view, push it to the back stack
        if (_currentEntry != null)
        {
            _backStack.Push(_currentEntry);
        }

        // Clear forward stack when navigating to a new view
        _forwardStack.Clear();

        // Set new current view
        _currentEntry = new NavigationEntry(viewName, parameter);

        // Notify of navigation
        OnNavigated(viewName, parameter);
        OnPropertyChanged(nameof(CurrentView));
        OnPropertyChanged(nameof(CanGoBack));
        OnPropertyChanged(nameof(CanGoForward));
    }

    public void GoBack()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(NavigationService));

        if (!CanGoBack)
            throw new InvalidOperationException("Cannot navigate back - no previous view in history.");

        // Push current view to forward stack
        if (_currentEntry != null)
        {
            _forwardStack.Push(_currentEntry);
        }

        // Pop from back stack
        _currentEntry = _backStack.Pop();

        // Notify of navigation
        OnNavigated(_currentEntry.ViewName, _currentEntry.Parameter);
        OnPropertyChanged(nameof(CurrentView));
        OnPropertyChanged(nameof(CanGoBack));
        OnPropertyChanged(nameof(CanGoForward));
    }

    public void GoForward()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(NavigationService));

        if (!CanGoForward)
            throw new InvalidOperationException("Cannot navigate forward - no next view in history.");

        // Push current view to back stack
        if (_currentEntry != null)
        {
            _backStack.Push(_currentEntry);
        }

        // Pop from forward stack
        _currentEntry = _forwardStack.Pop();

        // Notify of navigation
        OnNavigated(_currentEntry.ViewName, _currentEntry.Parameter);
        OnPropertyChanged(nameof(CurrentView));
        OnPropertyChanged(nameof(CanGoBack));
        OnPropertyChanged(nameof(CanGoForward));
    }

    public void ClearHistory()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(NavigationService));

        _backStack.Clear();
        _forwardStack.Clear();

        OnPropertyChanged(nameof(CanGoBack));
        OnPropertyChanged(nameof(CanGoForward));
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _backStack.Clear();
            _forwardStack.Clear();
            _currentEntry = null;
            _disposed = true;
        }
    }

    private void OnNavigated(string viewName, object? parameter)
    {
        Navigated?.Invoke(this, new NavigationEventArgs(viewName, parameter));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private sealed record NavigationEntry(string ViewName, object? Parameter);
}