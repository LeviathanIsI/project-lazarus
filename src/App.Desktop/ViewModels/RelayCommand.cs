using System.Windows.Input;

namespace Lazarus.Desktop.ViewModels
{
    /// <summary>
    /// A generic command implementation that supports both synchronous and asynchronous operations.
    /// Provides thread-safe command execution with proper error handling.
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Predicate<object?>? _canExecute;
        private bool _isExecuting;

        /// <summary>
        /// Initializes a new instance of the RelayCommand class.
        /// </summary>
        /// <param name="execute">The action to execute.</param>
        /// <param name="canExecute">The predicate to determine if execution is allowed.</param>
        public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        /// <summary>
        /// Initializes a new instance of the RelayCommand class with a parameterless action.
        /// </summary>
        /// <param name="execute">The action to execute.</param>
        /// <param name="canExecute">The function to determine if execution is allowed.</param>
        public RelayCommand(Action execute, Func<bool>? canExecute = null)
            : this(_ => execute(), canExecute != null ? _ => canExecute() : null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the RelayCommand class with an async action.
        /// </summary>
        /// <param name="executeAsync">The async action to execute.</param>
        /// <param name="canExecute">The function to determine if execution is allowed.</param>
        public RelayCommand(Func<Task> executeAsync, Func<bool>? canExecute = null)
        {
            _execute = _ => ExecuteAsync(executeAsync);
            _canExecute = canExecute != null ? _ => canExecute() && !_isExecuting : _ => !_isExecuting;
        }

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parameter)
        {
            return !_isExecuting && (_canExecute?.Invoke(parameter) ?? true);
        }

        public void Execute(object? parameter)
        {
            if (!CanExecute(parameter))
                return;

            try
            {
                _execute(parameter);
            }
            catch (Exception ex)
            {
                // Log the exception if logging is available
                System.Diagnostics.Debug.WriteLine($"RelayCommand execution failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Manually raises the CanExecuteChanged event to refresh command state.
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }

        private async void ExecuteAsync(Func<Task> asyncAction)
        {
            if (_isExecuting)
                return;

            try
            {
                _isExecuting = true;
                RaiseCanExecuteChanged();

                await asyncAction().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // Log the exception if logging is available
                System.Diagnostics.Debug.WriteLine($"RelayCommand async execution failed: {ex.Message}");
                throw;
            }
            finally
            {
                _isExecuting = false;
                RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// A generic version of RelayCommand that provides type-safe parameter handling.
    /// </summary>
    /// <typeparam name="T">The type of the command parameter.</typeparam>
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T?> _execute;
        private readonly Predicate<T?>? _canExecute;
        private bool _isExecuting;

        /// <summary>
        /// Initializes a new instance of the RelayCommand class.
        /// </summary>
        /// <param name="execute">The typed action to execute.</param>
        /// <param name="canExecute">The typed predicate to determine if execution is allowed.</param>
        public RelayCommand(Action<T?> execute, Predicate<T?>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        /// <summary>
        /// Initializes a new instance of the RelayCommand class with an async action.
        /// </summary>
        /// <param name="executeAsync">The async action to execute.</param>
        /// <param name="canExecute">The predicate to determine if execution is allowed.</param>
        public RelayCommand(Func<T?, Task> executeAsync, Predicate<T?>? canExecute = null)
        {
            _execute = param => ExecuteAsync(executeAsync, param);
            _canExecute = param => !_isExecuting && (canExecute?.Invoke(param) ?? true);
        }

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parameter)
        {
            if (_isExecuting)
                return false;

            // Handle parameter conversion
            if (parameter == null && default(T) == null)
                return _canExecute?.Invoke(default) ?? true;

            if (parameter is T typedParameter)
                return _canExecute?.Invoke(typedParameter) ?? true;

            return _canExecute?.Invoke(default) ?? true;
        }

        public void Execute(object? parameter)
        {
            if (!CanExecute(parameter))
                return;

            try
            {
                // Handle parameter conversion
                T? typedParameter = default;
                if (parameter is T param)
                    typedParameter = param;
                else if (parameter != null && typeof(T).IsAssignableFrom(parameter.GetType()))
                    typedParameter = (T)parameter;

                _execute(typedParameter);
            }
            catch (Exception ex)
            {
                // Log the exception if logging is available
                System.Diagnostics.Debug.WriteLine($"RelayCommand<{typeof(T).Name}> execution failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Manually raises the CanExecuteChanged event to refresh command state.
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }

        private async void ExecuteAsync(Func<T?, Task> asyncAction, T? parameter)
        {
            if (_isExecuting)
                return;

            try
            {
                _isExecuting = true;
                RaiseCanExecuteChanged();

                await asyncAction(parameter).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // Log the exception if logging is available
                System.Diagnostics.Debug.WriteLine($"RelayCommand<{typeof(T).Name}> async execution failed: {ex.Message}");
                throw;
            }
            finally
            {
                _isExecuting = false;
                RaiseCanExecuteChanged();
            }
        }
    }
}