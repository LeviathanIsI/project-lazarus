namespace Lazarus.Desktop.Configuration;

/// <summary>
/// Configuration for application update checks.
/// </summary>
public sealed class UpdatesOptions
{
    public const string SectionName = "Updates";

    /// <summary>
    /// Optional URL to a feed that returns the latest version info.
    /// Supports either plain text (version string) or JSON with a 'version' field.
    /// </summary>
    public string? FeedUrl { get; set; }

    /// <summary>
    /// Optional URL to release notes.
    /// </summary>
    public string? ReleaseNotesUrl { get; set; }
}

