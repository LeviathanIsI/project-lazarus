namespace Lazarus.Desktop.Configuration;

/// <summary>
/// Configuration options for database connections and behavior.
/// </summary>
public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    /// <summary>
    /// Custom connection string. If null, uses default application data location.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Command timeout in seconds for database operations.
    /// </summary>
    public int CommandTimeout { get; set; } = 30;

    /// <summary>
    /// Enable sensitive data logging for EF Core (development only).
    /// </summary>
    public bool EnableSensitiveDataLogging { get; set; } = false;
}