using Lazarus.Data.Entities;

namespace Lazarus.Data.Repositories;

/// <summary>
/// Repository interface for settings-specific data operations.
/// </summary>
public interface ISettingsRepository : IRepository<Settings>
{
    /// <summary>
    /// Gets a setting value by key asynchronously.
    /// </summary>
    /// <param name="key">The setting key.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The setting value if found; otherwise, null.</returns>
    Task<string?> GetValueAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a setting value by key with a default value asynchronously.
    /// </summary>
    /// <typeparam name="T">The type to convert the value to.</typeparam>
    /// <param name="key">The setting key.</param>
    /// <param name="defaultValue">The default value to return if the key is not found.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The setting value converted to type T, or the default value.</returns>
    Task<T> GetValueAsync<T>(string key, T defaultValue, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a setting value asynchronously.
    /// </summary>
    /// <param name="key">The setting key.</param>
    /// <param name="value">The setting value.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SetValueAsync(string key, string? value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a setting value asynchronously with type conversion.
    /// </summary>
    /// <typeparam name="T">The type of the value to convert.</typeparam>
    /// <param name="key">The setting key.</param>
    /// <param name="value">The setting value.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SetValueAsync<T>(string key, T value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a setting asynchronously.
    /// </summary>
    /// <param name="key">The setting key to remove.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the setting was removed; otherwise, false.</returns>
    Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all settings with keys matching a prefix asynchronously.
    /// </summary>
    /// <param name="keyPrefix">The key prefix to match.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The settings with matching key prefixes.</returns>
    Task<IEnumerable<Settings>> GetSettingsByPrefixAsync(string keyPrefix, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a setting exists asynchronously.
    /// </summary>
    /// <param name="key">The setting key to check.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the setting exists; otherwise, false.</returns>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
}