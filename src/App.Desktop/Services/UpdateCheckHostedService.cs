using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Lazarus.Desktop.Configuration;
using Lazarus.Shared.Settings;

namespace Lazarus.Desktop.Services;

/// <summary>
/// Performs an optional update check at startup if enabled by settings.
/// Does not download or install updates; logs availability only.
/// </summary>
internal sealed class UpdateCheckHostedService : IHostedService
{
    private readonly ILogger<UpdateCheckHostedService> _logger;
    private readonly ISettingsService _settings;
    private readonly IOptionsMonitor<UpdatesOptions> _options;
    private readonly IHttpClientFactory _httpFactory;

    public UpdateCheckHostedService(
        ILogger<UpdateCheckHostedService> logger,
        ISettingsService settings,
        IOptionsMonitor<UpdatesOptions> options,
        IHttpClientFactory httpFactory)
    {
        _logger = logger;
        _settings = settings;
        _options = options;
        _httpFactory = httpFactory;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            try { await _settings.LoadAsync().ConfigureAwait(false); } catch { }

            if (!_settings.Current.CheckForUpdatesOnStart)
            {
                _logger.LogDebug("CheckForUpdatesOnStart disabled; skipping update check");
                return;
            }

            var feed = _options.CurrentValue.FeedUrl
                       ?? Environment.GetEnvironmentVariable("LAZARUS_UPDATE_FEED");
            if (string.IsNullOrWhiteSpace(feed))
            {
                _logger.LogInformation("No update feed configured; set Updates:FeedUrl or LAZARUS_UPDATE_FEED to enable.");
                return;
            }

            var currentVersion = GetCurrentVersion();
            var latest = await TryGetLatestVersionAsync(feed, cancellationToken).ConfigureAwait(false);
            if (latest is null)
            {
                _logger.LogInformation("Update check did not return a version from {Feed}", feed);
                return;
            }

            if (latest > currentVersion)
            {
                var notes = _options.CurrentValue.ReleaseNotesUrl ?? string.Empty;
                _logger.LogInformation("New Lazarus version available: {Latest} (current {Current}). {Notes}", latest, currentVersion, notes);
            }
            else
            {
                _logger.LogInformation("Lazarus is up-to-date (version {Version})", currentVersion);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Update check failed");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static Version GetCurrentVersion()
    {
        var v = Assembly.GetExecutingAssembly().GetName().Version;
        return v ?? new Version(0, 0, 0, 0);
    }

    private async Task<Version?> TryGetLatestVersionAsync(string feedUrl, CancellationToken ct)
    {
        using var http = _httpFactory.CreateClient("updates");
        http.Timeout = TimeSpan.FromSeconds(5);
        http.DefaultRequestHeaders.UserAgent.ParseAdd("Lazarus-Desktop/UpdateChecker");
        http.DefaultRequestHeaders.Accept.ParseAdd("application/json, text/plain;q=0.5, */*;q=0.1");

        try
        {
            using var resp = await http.GetAsync(feedUrl, ct).ConfigureAwait(false);
            resp.EnsureSuccessStatusCode();
            var contentType = resp.Content.Headers.ContentType?.MediaType ?? string.Empty;
            if (contentType.Contains("json", StringComparison.OrdinalIgnoreCase))
            {
                using var s = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
                using var doc = await JsonDocument.ParseAsync(s, cancellationToken: ct).ConfigureAwait(false);
                if (doc.RootElement.TryGetProperty("version", out var vProp))
                {
                    var vStr = vProp.GetString();
                    if (Version.TryParse(NormalizeVersion(vStr), out var v)) return v;
                }
            }
            else
            {
                var body = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                if (Version.TryParse(NormalizeVersion(body.Trim()), out var v)) return v;
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogDebug(ex, "Failed to fetch update feed {Feed}", feedUrl);
        }
        return null;
    }

    private static string NormalizeVersion(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "0.0.0";
        // Strip leading 'v' or spaces
        var t = input.Trim();
        if (t.StartsWith('v') || t.StartsWith('V')) t = t.Substring(1);
        // Remove any build metadata (+...) and pre-release (-...)
        var idx = t.IndexOfAny(new[] { '+', ' ' });
        if (idx >= 0) t = t.Substring(0, idx);
        return t;
    }
}

