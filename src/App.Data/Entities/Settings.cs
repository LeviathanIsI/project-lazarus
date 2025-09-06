using System.ComponentModel.DataAnnotations;

namespace Lazarus.Data.Entities;

/// <summary>
/// Represents application settings stored as key-value pairs.
/// </summary>
public class Settings
{
    /// <summary>
    /// Gets or sets the setting key (primary key).
    /// </summary>
    [Key]
    [Required]
    [MaxLength(255)]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the setting value.
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// Gets or sets when this setting was last modified.
    /// </summary>
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
}