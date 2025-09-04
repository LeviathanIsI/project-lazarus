using Lazarus.App.Shared.Models;

namespace Lazarus.App.Shared.Contracts;

/// <summary>
/// Service contract for managing runner instances
/// </summary>
public interface IRunnerService
{
    /// <summary>
    /// Gets all registered runners and their current status asynchronously
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of runner statuses</returns>
    Task<IEnumerable<RunnerStatus>> GetAllRunnersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific runner by ID asynchronously
    /// </summary>
    /// <param name="runnerId">The runner identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The runner status if found, null otherwise</returns>
    Task<RunnerStatus?> GetRunnerAsync(string runnerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts a specific runner asynchronously
    /// </summary>
    /// <param name="runnerId">The runner identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if started successfully, false otherwise</returns>
    Task<bool> StartRunnerAsync(string runnerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops a specific runner asynchronously
    /// </summary>
    /// <param name="runnerId">The runner identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if stopped successfully, false otherwise</returns>
    Task<bool> StopRunnerAsync(string runnerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a new runner instance
    /// </summary>
    /// <param name="runnerId">The runner identifier</param>
    /// <param name="name">The runner display name</param>
    /// <param name="modelName">The model this runner handles</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if registered successfully, false otherwise</returns>
    Task<bool> RegisterRunnerAsync(string runnerId, string name, string modelName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unregisters a runner instance
    /// </summary>
    /// <param name="runnerId">The runner identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if unregistered successfully, false otherwise</returns>
    Task<bool> UnregisterRunnerAsync(string runnerId, CancellationToken cancellationToken = default);
}