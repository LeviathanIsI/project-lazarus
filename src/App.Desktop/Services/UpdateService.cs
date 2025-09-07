using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using Lazarus.Desktop.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lazarus.Desktop.Services;

internal sealed class UpdateService : IUpdateService
{
    private readonly ILogger<UpdateService> _logger;
    private readonly IOptionsMonitor<UpdatesOptions> _options;
    private readonly IHttpClientFactory _httpFactory;

    public UpdateService(ILogger<UpdateService> logger, IOptionsMonitor<UpdatesOptions> options, IHttpClientFactory httpFactory)
    {
        _logger = logger;
        _options = options;
        _httpFactory = httpFactory;
    }

    public async Task<UpdateCheckResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        var current = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0,0,0,0);
        var feed = _options.CurrentValue.FeedUrl ?? Environment.GetEnvironmentVariable("LAZARUS_UPDATE_FEED");
        var notes = _options.CurrentValue.ReleaseNotesUrl;

        if (string.IsNullOrWhiteSpace(feed))
        {
            return new UpdateCheckResult(false, current, null, feed, notes, "No update feed configured");
        }

        var latest = await TryGetLatestVersionAsync(feed!, cancellationToken).ConfigureAwait(false);
        if (latest is null)
        {
            return new UpdateCheckResult(false, current, null, feed, notes, "Feed returned no version");
        }

        var available = latest > current;
        return new UpdateCheckResult(available, current, latest, feed, notes, available ? "Update available" : "Up-to-date");
    }

    private async Task<Version?> TryGetLatestVersionAsync(string feedUrl, CancellationToken ct)
    {
        using var http = _httpFactory.CreateClient();
        http.Timeout = TimeSpan.FromSeconds(6);
        http.DefaultRequestHeaders.UserAgent.ParseAdd("Lazarus-Desktop/UpdateService");
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
        var t = input.Trim();
        if (t.StartsWith('v') || t.StartsWith('V')) t = t.Substring(1);
        var idx = t.IndexOfAny(new[] { '+', ' ' });
        if (idx >= 0) t = t.Substring(0, idx);
        return t;
    }
}

