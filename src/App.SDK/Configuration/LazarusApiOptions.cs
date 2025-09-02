namespace Lazarus.App.SDK.Configuration;

/// <summary>
/// Configuration options for the Lazarus API client
/// </summary>
public class LazarusApiOptions
{
    /// <summary>
    /// Configuration section name
    /// </summary>
    public const string SectionName = "LazarusApi";

    /// <summary>
    /// Gets or sets the base URL for the Lazarus API
    /// </summary>
    public string BaseUrl { get; set; } = "https://localhost:7001";

    /// <summary>
    /// Gets or sets the API key for authentication
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Gets or sets the timeout for HTTP requests in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the maximum number of retry attempts for failed requests
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Gets or sets a value indicating whether to enable detailed logging
    /// </summary>
    public bool EnableDetailedLogging { get; set; } = false;
}