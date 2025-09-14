using System;
using System.Threading;
using System.Threading.Tasks;

namespace Lazarus.Desktop.Services
{
    /// <summary>
    /// Bootstrap progress information
    /// </summary>
    public record BootstrapProgress(string Step, int Percent);

    /// <summary>
    /// Application bootstrapper interface
    /// </summary>
    public interface IAppBootstrapper
    {
        /// <summary>
        /// Initializes the application with progress reporting
        /// </summary>
        /// <param name="progress">Progress reporter (optional)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task that completes when initialization is done</returns>
        Task InitializeAsync(IProgress<BootstrapProgress>? progress = null, CancellationToken cancellationToken = default);
    }
}
