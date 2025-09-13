using System;
using System.Threading.Tasks;

namespace Lazarus.Desktop.Services
{
    /// <summary>
    /// Manages the initialization sequence of Lazarus application components
    /// </summary>
    public interface IInitializationManager
    {
        /// <summary>
        /// Indicates whether initialization has completed
        /// </summary>
        bool IsInitialized { get; }
        
        /// <summary>
        /// Indicates whether initialization is currently in progress
        /// </summary>
        bool IsInitializing { get; }
        
        /// <summary>
        /// Current initialization progress (0-100)
        /// </summary>
        int ProgressPercentage { get; }
        
        /// <summary>
        /// Current initialization message
        /// </summary>
        string CurrentMessage { get; }
        
        /// <summary>
        /// Event raised when initialization progress changes
        /// </summary>
        event EventHandler<InitializationProgressEventArgs>? InitializationProgressChanged;
        
        /// <summary>
        /// Event raised when initialization completes successfully
        /// </summary>
        event EventHandler? InitializationCompleted;
        
        /// <summary>
        /// Event raised when initialization fails
        /// </summary>
        event EventHandler<InitializationFailedEventArgs>? InitializationFailed;
        
        /// <summary>
        /// Starts the initialization process
        /// </summary>
        Task InitializeAsync();
        
        /// <summary>
        /// Cancels the initialization process
        /// </summary>
        void Cancel();
    }
    
    /// <summary>
    /// Event arguments for initialization progress updates
    /// </summary>
    public class InitializationProgressEventArgs : EventArgs
    {
        public InitializationProgressEventArgs(int percentage, string message)
        {
            Percentage = percentage;
            Message = message;
        }
        
        public int Percentage { get; }
        public string Message { get; }
    }
    
    /// <summary>
    /// Event arguments for initialization failure
    /// </summary>
    public class InitializationFailedEventArgs : EventArgs
    {
        public InitializationFailedEventArgs(string error)
        {
            Error = error;
        }
        
        public string Error { get; }
    }
}