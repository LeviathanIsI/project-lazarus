using Lazarus.Data.Entities;
using Lazarus.Data.Enums;

namespace Lazarus.Data.Repositories;

/// <summary>
/// Repository interface for model-specific data operations.
/// </summary>
public interface IModelRepository : IRepository<Model>
{
    /// <summary>
    /// Gets all active models asynchronously.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The active models.</returns>
    Task<IEnumerable<Model>> GetActiveModelsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets models by runner type asynchronously.
    /// </summary>
    /// <param name="runnerType">The runner type to filter by.</param>
    /// <param name="activeOnly">Whether to return only active models.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The models with the specified runner type.</returns>
    Task<IEnumerable<Model>> GetModelsByRunnerTypeAsync(RunnerType runnerType, bool activeOnly = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a model by name asynchronously.
    /// </summary>
    /// <param name="name">The model name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The model if found; otherwise, null.</returns>
    Task<Model?> GetModelByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a model path already exists asynchronously.
    /// </summary>
    /// <param name="path">The model path to check.</param>
    /// <param name="excludeId">Optional model ID to exclude from the check.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the path exists; otherwise, false.</returns>
    Task<bool> PathExistsAsync(string path, Guid? excludeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivates all models asynchronously.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of models deactivated.</returns>
    Task<int> DeactivateAllModelsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Activates a specific model and deactivates all others asynchronously.
    /// </summary>
    /// <param name="modelId">The model ID to activate.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the operation was successful; otherwise, false.</returns>
    Task<bool> SetActiveModelAsync(Guid modelId, CancellationToken cancellationToken = default);
}