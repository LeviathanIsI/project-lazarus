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
    private CancellationTokenSource? _saveDebounceCts;

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
    public string SettingsFilePath => SettingsPaths.SettingsFile;

    /// <inheritdoc />
    public event EventHandler<AppSettings>? SettingsChanged;

    /// <inheritdoc />
    public async Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPaths.SettingsFile)!);

            if (!File.Exists(SettingsPaths.SettingsFile))
            {
                _logger.LogInformation("Settings file not found; creating defaults at {File}", SettingsPaths.SettingsFile);
                await SaveInternalAsync(_current, cancellationToken).ConfigureAwait(false);
                OnSettingsChanged(_current);
                return Current;
            }

            await using var stream = File.Open(SettingsPaths.SettingsFile, FileMode.Open, FileAccess.Read, FileShare.Read);
            var loaded = await JsonSerializer.DeserializeAsync<AppSettings>(stream, JsonOptions(), cancellationToken).ConfigureAwait(false)
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
    public Task SaveAsync(CancellationToken cancellationToken = default)
    {
        AppSettings snapshot;
        lock (_gate) snapshot = _current;
        return SaveInternalAsync(snapshot, cancellationToken);
    }

    /// <inheritdoc />
    public void SaveSoon()
    {
        // Debounce saves (~2s default for MVP)
        var delay = TimeSpan.FromSeconds(2);

        var pending = Interlocked.Exchange(ref _saveDebounceCts, new CancellationTokenSource());
        pending?.Cancel();
        var cts = _saveDebounceCts!;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(delay, cts.Token).ConfigureAwait(false);
                await SaveAsync(cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Debounced settings save failed");
            }
        });
    }

    /// <inheritdoc />
    public void Update(Action<AppSettings> apply)
    {
        if (apply is null) throw new ArgumentNullException(nameof(apply));
        lock (_gate)
        {
            apply(_current);
        }
        OnSettingsChanged(Current);
        SaveSoon();
    }

    /// <inheritdoc />
    public async Task ResetToDefaultsAsync(CancellationToken cancellationToken = default)
    {
        var defaults = AppSettings.CreateDefault();
        lock (_gate) _current = defaults;
        OnSettingsChanged(defaults);
        await SaveInternalAsync(defaults, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task ImportAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentNullException(nameof(filePath));
        await using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var imported = await JsonSerializer.DeserializeAsync<AppSettings>(stream, JsonOptions(), cancellationToken).ConfigureAwait(false)
            ?? AppSettings.CreateDefault();
        UpgradeIfNeeded(imported);
        lock (_gate) _current = imported;
        OnSettingsChanged(imported);
        await SaveInternalAsync(imported, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task ExportAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentNullException(nameof(filePath));
        AppSettings snapshot;
        lock (_gate) snapshot = _current;
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        await using var stream = File.Open(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        await JsonSerializer.SerializeAsync(stream, snapshot, JsonOptions(), cancellationToken).ConfigureAwait(false);
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task SaveInternalAsync(AppSettings settings, CancellationToken cancellationToken)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPaths.SettingsFile)!);

            // Write to a tmp file then atomically replace
            await using (var tmpStream = File.Open(SettingsPaths.TempFile, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await JsonSerializer.SerializeAsync(tmpStream, settings, JsonOptions(), cancellationToken).ConfigureAwait(false);
                await tmpStream.FlushAsync(cancellationToken).ConfigureAwait(false);
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
