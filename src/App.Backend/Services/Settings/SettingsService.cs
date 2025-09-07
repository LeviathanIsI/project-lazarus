using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Lazarus.Shared.Settings;
using Microsoft.Extensions.Logging;

namespace Lazarus.Backend.Services.Settings;

/// <summary>
/// JSON-backed settings persistence service with schema versioning and safe writes.
/// </summary>
public sealed class SettingsService : ISettingsService
{
    private readonly ILogger<SettingsService> _logger;
    private readonly object _gate = new();
    private AppSettings _current;
    // reserved for future debounce implementation
    // private CancellationTokenSource? _saveDebounceCts;

    /// <summary>
    /// Initializes a new instance of <see cref="SettingsService"/>.
    /// </summary>
    public SettingsService(ILogger<SettingsService> logger)
    {
        _logger = logger;
        _current = AppSettings.CreateDefault();
    }

    /// <inheritdoc />
    public AppSettings Current
    {
        get
        {
            lock (_gate) return _current;
        }
    }

    /// <inheritdoc />
    /// <inheritdoc />
    public event EventHandler<AppSettings>? SettingsChanged;

    /// <inheritdoc />
    public async Task<AppSettings> LoadAsync()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPaths.SettingsFile)!);

            if (!File.Exists(SettingsPaths.SettingsFile))
            {
                _logger.LogInformation("Settings file not found; creating defaults at {File}", SettingsPaths.SettingsFile);
                await SaveInternalAsync(_current, CancellationToken.None).ConfigureAwait(false);
                OnSettingsChanged(_current);
                return Current;
            }

            await using var stream = File.Open(SettingsPaths.SettingsFile, FileMode.Open, FileAccess.Read, FileShare.Read);
            var loaded = await JsonSerializer.DeserializeAsync<AppSettings>(stream, JsonOptions(), CancellationToken.None).ConfigureAwait(false)
                ?? AppSettings.CreateDefault();

            UpgradeIfNeeded(loaded);

            lock (_gate) _current = loaded;
            OnSettingsChanged(loaded);
            return loaded;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load settings; using defaults");
            lock (_gate) _current = AppSettings.CreateDefault();
            OnSettingsChanged(_current);
            return Current;
        }
    }

    /// <inheritdoc />
    public Task SaveAsync(AppSettings? settings = null)
    {
        if (settings is not null)
        {
            lock (_gate) _current = settings;
            OnSettingsChanged(_current);
        }
        AppSettings snapshot;
        lock (_gate) snapshot = _current;
        return SaveInternalAsync(snapshot, CancellationToken.None);
    }

    private async Task SaveInternalAsync(AppSettings settings, CancellationToken cancellationToken)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPaths.SettingsFile)!);

            // Write to a tmp file then atomically replace
            await using (var tmpStream = File.Open(SettingsPaths.TempFile, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await JsonSerializer.SerializeAsync(tmpStream, settings, JsonOptions(), CancellationToken.None).ConfigureAwait(false);
                await tmpStream.FlushAsync(CancellationToken.None).ConfigureAwait(false);
            }

#if NET6_0_OR_GREATER
            File.Move(SettingsPaths.TempFile, SettingsPaths.SettingsFile, overwrite: true);
#else
            if (File.Exists(SettingsPaths.SettingsFile)) File.Delete(SettingsPaths.SettingsFile);
            File.Move(SettingsPaths.TempFile, SettingsPaths.SettingsFile);
#endif
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save settings to {File}", SettingsPaths.SettingsFile);
            // Best-effort: try to remove temp file if something failed
            try { if (File.Exists(SettingsPaths.TempFile)) File.Delete(SettingsPaths.TempFile); } catch { }
            throw;
        }
    }

    private static JsonSerializerOptions JsonOptions()
    {
        return new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };
    }

    private void UpgradeIfNeeded(AppSettings settings)
    {
        if (settings.SchemaVersion < SettingsSchema.CurrentVersion)
        {
            // Placeholder for future migrations; keep it simple for MVP.
            settings.SchemaVersion = SettingsSchema.CurrentVersion;
        }
    }

    private void OnSettingsChanged(AppSettings settings)
    {
        try { SettingsChanged?.Invoke(this, settings); } catch { }
    }
}
