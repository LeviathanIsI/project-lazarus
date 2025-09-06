using System.ComponentModel;
using System.Text.Json;
using Lazarus.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lazarus.Data.Repositories;

/// <summary>
/// Repository implementation for settings-specific data operations.
/// </summary>
public class SettingsRepository : Repository<Settings>, ISettingsRepository
{
    private readonly LazarusDbContext _context;
    private readonly ILogger<SettingsRepository>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="logger">Optional logger for repository operations.</param>
    public SettingsRepository(LazarusDbContext context, ILogger<SettingsRepository>? logger = null)
        : base(context, logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<string?> GetValueAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);

        try
        {
            var setting = await _context.Settings
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Key == key, cancellationToken)
                .ConfigureAwait(false);

            return setting?.Value;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to get setting value for key '{Key}'", key);
            throw;
        }
    }

    public async Task<T> GetValueAsync<T>(string key, T defaultValue, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);

        try
        {
            var value = await GetValueAsync(key, cancellationToken).ConfigureAwait(false);

            if (value is null)
            {
                return defaultValue;
            }

            return ConvertValue<T>(value, defaultValue);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to get typed setting value for key '{Key}'", key);
            return defaultValue;
        }
    }

    public async Task SetValueAsync(string key, string? value, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);

        try
        {
            var existingSetting = await _context.Settings
                .FirstOrDefaultAsync(s => s.Key == key, cancellationToken)
                .ConfigureAwait(false);

            if (existingSetting is not null)
            {
                existingSetting.Value = value;
                existingSetting.LastModified = DateTime.UtcNow;
            }
            else
            {
                _context.Settings.Add(new Settings
                {
                    Key = key,
                    Value = value,
                    LastModified = DateTime.UtcNow
                });
            }

            await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to set setting value for key '{Key}'", key);
            throw;
        }
    }

    public async Task SetValueAsync<T>(string key, T value, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);

        var stringValue = ConvertToString(value);
        await SetValueAsync(key, stringValue, cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);

        try
        {
            var setting = await _context.Settings
                .FirstOrDefaultAsync(s => s.Key == key, cancellationToken)
                .ConfigureAwait(false);

            if (setting is not null)
            {
                _context.Settings.Remove(setting);
                await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to remove setting with key '{Key}'", key);
            throw;
        }
    }

    public async Task<IEnumerable<Settings>> GetSettingsByPrefixAsync(string keyPrefix, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(keyPrefix);

        try
        {
            return await _context.Settings
                .AsNoTracking()
                .Where(s => s.Key.StartsWith(keyPrefix))
                .OrderBy(s => s.Key)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to get settings by prefix '{KeyPrefix}'", keyPrefix);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);

        try
        {
            return await _context.Settings
                .AsNoTracking()
                .AnyAsync(s => s.Key == key, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to check if setting exists with key '{Key}'", key);
            throw;
        }
    }

    private static T ConvertValue<T>(string value, T defaultValue)
    {
        if (typeof(T) == typeof(string))
        {
            return (T)(object)value;
        }

        if (typeof(T) == typeof(bool) && bool.TryParse(value, out var boolValue))
        {
            return (T)(object)boolValue;
        }

        if (typeof(T) == typeof(int) && int.TryParse(value, out var intValue))
        {
            return (T)(object)intValue;
        }

        if (typeof(T) == typeof(double) && double.TryParse(value, out var doubleValue))
        {
            return (T)(object)doubleValue;
        }

        if (typeof(T) == typeof(DateTime) && DateTime.TryParse(value, out var dateValue))
        {
            return (T)(object)dateValue;
        }

        // Try JSON deserialization for complex types
        try
        {
            var deserializedValue = JsonSerializer.Deserialize<T>(value);
            return deserializedValue ?? defaultValue;
        }
        catch
        {
            // Fall back to TypeConverter
            try
            {
                var converter = TypeDescriptor.GetConverter(typeof(T));
                if (converter.CanConvertFrom(typeof(string)))
                {
                    var convertedValue = converter.ConvertFromString(value);
                    return convertedValue is not null ? (T)convertedValue : defaultValue;
                }
            }
            catch
            {
                // Return default value if conversion fails
            }
        }

        return defaultValue;
    }

    private static string? ConvertToString<T>(T value)
    {
        if (value is null)
        {
            return null;
        }

        if (value is string stringValue)
        {
            return stringValue;
        }

        if (value is bool or int or double or DateTime)
        {
            return value.ToString();
        }

        // Use JSON serialization for complex types
        try
        {
            return JsonSerializer.Serialize(value);
        }
        catch
        {
            return value.ToString();
        }
    }
}