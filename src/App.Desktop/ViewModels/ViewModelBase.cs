using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Lazarus.Desktop.ViewModels
{
    /// <summary>
    /// Base class for ViewModels with property change notification and disposal support.
    /// Provides thread-safe property change notifications and proper cleanup patterns.
    /// </summary>
    public abstract class ViewModelBase : INotifyPropertyChanged, IDisposable
    {
        private bool _disposed;

        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Gets a value indicating whether this ViewModel has been disposed.
        /// </summary>
        protected bool IsDisposed => _disposed;

        /// <summary>
        /// Raises the PropertyChanged event for the specified property.
        /// Automatically marshals to the UI thread if called from a background thread.
        /// </summary>
        /// <param name="propertyName">The name of the property that changed.</param>
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            if (_disposed || string.IsNullOrEmpty(propertyName))
                return;

            var handler = PropertyChanged;
            if (handler == null)
                return;

            var args = new PropertyChangedEventArgs(propertyName);

            // Ensure property change notifications are raised on the UI thread
            if (System.Windows.Application.Current?.Dispatcher?.CheckAccess() == false)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => handler(this, args));
            }
            else
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Sets the property value and raises PropertyChanged if the value has changed.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="field">The backing field for the property.</param>
        /// <param name="value">The new value to set.</param>
        /// <param name="propertyName">The name of the property (automatically provided by CallerMemberName).</param>
        /// <returns>True if the property value was changed; otherwise, false.</returns>
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (_disposed)
                return false;

            if (Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        /// <summary>
        /// Sets the property value and raises PropertyChanged if the value has changed.
        /// Also executes the specified action after setting the property.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="field">The backing field for the property.</param>
        /// <param name="value">The new value to set.</param>
        /// <param name="onChanged">Action to execute after the property is changed.</param>
        /// <param name="propertyName">The name of the property (automatically provided by CallerMemberName).</param>
        /// <returns>True if the property value was changed; otherwise, false.</returns>
        protected bool SetProperty<T>(ref T field, T value, Action onChanged, [CallerMemberName] string? propertyName = null)
        {
            if (SetProperty(ref field, value, propertyName))
            {
                onChanged?.Invoke();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Throws an ObjectDisposedException if the ViewModel has been disposed.
        /// </summary>
        protected void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the ViewModel and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">True to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources
                    OnDisposing();
                }

                _disposed = true;
            }
        }

        /// <summary>
        /// Called when the ViewModel is being disposed. 
        /// Override this method to perform cleanup of managed resources.
        /// </summary>
        protected virtual void OnDisposing()
        {
            // Base implementation does nothing - override in derived classes
        }
    }
}