using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Threading;
using System.Windows;

namespace Lazarus.App.Desktop.ViewModels;

/// <summary>
/// Base view model class providing common MVVM functionality with proper resource disposal and UI thread safety
/// </summary>
public abstract class BaseViewModel : ObservableObject, IDisposable
{
    private bool _isBusy;
    private string _statusMessage = string.Empty;
    
    /// <summary>
    /// Gets the UI dispatcher for thread-safe property updates
    /// </summary>
    protected Dispatcher UIDispatcher { get; } = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;

    /// <summary>
    /// Gets or sets a value indicating whether the view model is busy
    /// </summary>
    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    /// <summary>
    /// Gets or sets the current status message
    /// </summary>
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    /// <summary>
    /// Sets the busy state and status message with thread-safe execution
    /// </summary>
    /// <param name="isBusy">Whether the view model is busy</param>
    /// <param name="message">The status message</param>
    protected void SetBusyState(bool isBusy, string message = "")
    {
        if (UIDispatcher.CheckAccess())
        {
            IsBusy = isBusy;
            StatusMessage = message;
        }
        else
        {
            UIDispatcher.Invoke(() =>
            {
                IsBusy = isBusy;
                StatusMessage = message;
            });
        }
    }

    /// <summary>
    /// Executes an action on the UI thread with proper error handling
    /// </summary>
    /// <param name="action">The action to execute</param>
    protected void ExecuteOnUIThread(Action action)
    {
        ThrowIfDisposed();
        
        if (UIDispatcher.CheckAccess())
        {
            action();
        }
        else
        {
            UIDispatcher.Invoke(action);
        }
    }

    /// <summary>
    /// Executes an action on the UI thread asynchronously with proper error handling
    /// </summary>
    /// <param name="action">The action to execute</param>
    /// <returns>A task representing the asynchronous operation</returns>
    protected Task ExecuteOnUIThreadAsync(Action action)
    {
        ThrowIfDisposed();
        
        if (UIDispatcher.CheckAccess())
        {
            action();
            return Task.CompletedTask;
        }
        else
        {
            return UIDispatcher.InvokeAsync(action).Task;
        }
    }

    #region IDisposable Implementation

    private bool _disposed = false;

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases unmanaged and - optionally - managed resources
    /// </summary>
    /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            // Derived classes should override this method to dispose of their specific resources
            DisposeResources();
            _disposed = true;
        }
    }

    /// <summary>
    /// Override this method in derived classes to dispose of specific resources
    /// </summary>
    protected virtual void DisposeResources()
    {
        // Base implementation - nothing to dispose
    }

    /// <summary>
    /// Throws an exception if the object has been disposed
    /// </summary>
    protected void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(GetType().Name);
        }
    }

    #endregion
}