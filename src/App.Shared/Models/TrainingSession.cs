using System.ComponentModel.DataAnnotations;

namespace Lazarus.App.Shared.Models;

/// <summary>
/// Represents a training session within the Lazarus platform
/// </summary>
public class TrainingSession : BaseEntity
{
    /// <summary>
    /// Gets or sets the name of the training session
    /// </summary>
    [Required]
    [MaxLength(256)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the training session
    /// </summary>
    [MaxLength(1024)]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the current status of the training session
    /// </summary>
    public TrainingStatus Status { get; set; } = TrainingStatus.Pending;

    /// <summary>
    /// Gets or sets the timestamp when training started
    /// </summary>
    public DateTimeOffset? StartedAt { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when training completed
    /// </summary>
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>
    /// Gets or sets the progress percentage (0-100)
    /// </summary>
    [Range(0, 100)]
    public double Progress { get; set; }

    /// <summary>
    /// Gets or sets the configuration parameters for this training session
    /// </summary>
    public string? ConfigurationJson { get; set; }
}

/// <summary>
/// Enumeration of possible training session statuses
/// </summary>
public enum TrainingStatus
{
    /// <summary>
    /// Training session is pending and has not started
    /// </summary>
    Pending,

    /// <summary>
    /// Training session is currently running
    /// </summary>
    Running,

    /// <summary>
    /// Training session completed successfully
    /// </summary>
    Completed,

    /// <summary>
    /// Training session failed with errors
    /// </summary>
    Failed,

    /// <summary>
    /// Training session was cancelled by user
    /// </summary>
    Cancelled
}